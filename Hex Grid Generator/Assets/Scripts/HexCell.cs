using System;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    #region properties
    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    public Color Color
    {
        get
        {
            return _color;
        }
        set
        {
            if (_color == value) return;

            _color = value;
            Refresh();
        }
    }

    public int Elevation
    {
        get
        {
            return _elevation;
        }
        set
        {
            if (_elevation == value) return;

            _elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.ElevationStep;

            var noise = (HexMetrics.SampleNoise(position).y * 2f - 1f) 
                * (HexMetrics.ElevationPerturbFlag ? 1f : HexMetrics.ElevationPerturbStrength);
            position.y += noise;

            transform.localPosition = position;

            Vector3 uiPosition = UiRect.localPosition;
            uiPosition.z = -position.y;
            UiRect.localPosition = uiPosition;

            // preventing uphill rivers
            if (HasOutgoingRiver && Elevation < GetNeighbor(OutgoingRiver).Elevation)
                RemoveOutgoingRiver();
            if (HasIncomingRiver && Elevation > GetNeighbor(IncomingRiver).Elevation)
                RemoveIncomingRiver();
            
            Refresh();
        }
    }
    #endregion

    public HexCoordinates Coordinates;
    public RectTransform UiRect;
    public HexGridChunk Chunk;
    public bool HasIncomingRiver, HasOutgoingRiver;
    public HexDirection IncomingRiver, OutgoingRiver;

    [SerializeField] HexCell[] _neighbors;

    Color _color;
    int _elevation;

    public float StreamBedY => (Elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;

    public HexCell GetNeighbor(HexDirection? direction)
    {
        if (direction == null)
            throw new ArgumentNullException();

        return _neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction) 
        => HexMetrics.GetEdgeType(Elevation, _neighbors[(int)direction].Elevation);

    public HexEdgeType GetEdgeType(HexCell otherCell) => HexMetrics.GetEdgeType(Elevation, otherCell.Elevation);

    public bool HasRiverThroughEdge(HexDirection direction) 
        => HasIncomingRiver && IncomingRiver == direction
        || HasOutgoingRiver && OutgoingRiver == direction;

    public bool HasRiver => HasIncomingRiver || HasOutgoingRiver;

    public bool HasRiverBeginOrEnd => HasIncomingRiver ^ HasOutgoingRiver; // xor operator, true only if one of the argument is true
        //=> HasIncomingRiver && !HasOutgoingRiver
        //|| !HasIncomingRiver && HasOutgoingRiver;
    
    public void RemoveOutgoingRiver()
    {
        if (!HasOutgoingRiver) return;
        
        // nieghbor's river has to be taken care of too
        HexCell neighbor = GetNeighbor(OutgoingRiver);

        HasOutgoingRiver = false;
        RefreshSelfOnly();

        neighbor.HasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!HasIncomingRiver) return;

        HexCell neighbor = GetNeighbor(IncomingRiver);

        HasIncomingRiver = false;
        RefreshSelfOnly();
        
        neighbor.HasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }
    
    public void SetIncomingRiver(HexDirection direction)
    {
        // incoming river is already set
        if (HasIncomingRiver) return;

        var neighbor = GetNeighbor(direction);

        // neighbor does not exists or its outgoing river is already set
        if (neighbor != null && neighbor.HasOutgoingRiver) return;

        // river cannot flow uphill
        if (Elevation > neighbor.Elevation) return;

        HasIncomingRiver = true;
        IncomingRiver = direction;
        RefreshSelfOnly();

        neighbor.HasOutgoingRiver = true;
        neighbor.OutgoingRiver = direction.Opposite();
        neighbor.RefreshSelfOnly();
    }

    void RefreshSelfOnly() => Chunk.Refresh();

    void Refresh()
    {
        if (Chunk)
        {
            Chunk.Refresh();
            for (int i = 0; i < _neighbors.Length; i++)
            {
                HexCell neighbor = _neighbors[i];
                if (neighbor != null && neighbor.Chunk != Chunk)
                    neighbor.Chunk.Refresh();
            }
        }
    }

    public float RiverSurfaceY
    {
        get
        {
            return (Elevation + HexMetrics.RiverSurfaceElevationOffset) * HexMetrics.ElevationStep;
        }
    }
}