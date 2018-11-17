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
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;

            transform.localPosition = position;

            Vector3 uiPosition = UiRect.localPosition;
            uiPosition.z = -position.y;
            UiRect.localPosition = uiPosition;

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
    public HexDirection IncomingRiver, OutgoingRiver;
    public bool HasIncomingRiver, HasOutgoingRiver;

    [SerializeField] HexCell[] _neighbors;

    Color _color;
    int _elevation;
    
    public HexCell GetNeighbor(HexDirection direction) => _neighbors[(int)direction];

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

    public bool HasRiverBeginOrEnd => HasIncomingRiver != HasOutgoingRiver;

    public void RemoveOutgoingRiver()
    {
        if (!HasOutgoingRiver) return;

        HasOutgoingRiver = false;
        RefreshSelfOnly();

        // nieghbor's river has to be taken care of too
        HexCell neighbor = GetNeighbor(OutgoingRiver);
        neighbor.HasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!HasIncomingRiver)
            return;

        HasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(IncomingRiver);
        neighbor.HasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void SetOutgoingRiver(HexDirection direction)
    {
        if (HasOutgoingRiver && OutgoingRiver == direction)
            return;

        HexCell neighbor = GetNeighbor(direction);
        if (!neighbor || Elevation < neighbor.Elevation)
            return;

        RemoveOutgoingRiver();
        if (HasIncomingRiver && IncomingRiver == direction)
            RemoveIncomingRiver();

        HasOutgoingRiver = true;
        OutgoingRiver = direction;
        RefreshSelfOnly();

        neighbor.RemoveIncomingRiver();
        neighbor.HasIncomingRiver = true;
        neighbor.IncomingRiver = direction.Opposite();
        neighbor.RefreshSelfOnly();
    }

    public float StreamBedY => (Elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;

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
}