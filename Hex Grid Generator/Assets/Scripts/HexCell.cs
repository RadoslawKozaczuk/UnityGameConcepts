using System;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    #region properties
    public Vector3 Position => transform.localPosition;

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value) return;

            _color = value;
            Refresh();
        }
    }

    public int Elevation
    {
        get => _elevation;
        set
        {
            if (_elevation == value) return;

            // changing elevation may affect roads
            // if the slope become to stiff road should be removed
            for (int i = 0; i < _roads.Length; i++)
                if (_roads[i] && GetElevationDifference((HexDirection)i) > 1)
                    SetRoad(i, false);

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

            ValidateRivers();

            Refresh();
        }
    }

    public bool HasRoads
    {
        get
        {
            for (int i = 0; i < _roads.Length; i++)
                if (_roads[i]) return true;
            return false;
        }
    }

    public HexDirection RiverBeginOrEndDirection => HasIncomingRiver ? IncomingRiver : OutgoingRiver;

    public int WaterLevel
    {
        get => _waterLevel;
        set
        {
            if (_waterLevel == value)
                return;

            _waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }
    int _waterLevel;

    public bool IsUnderwater => _waterLevel > 0;
    #endregion

    public HexCoordinates Coordinates;
    public RectTransform UiRect;
    public HexGridChunk Chunk;
    public bool HasIncomingRiver, HasOutgoingRiver;
    public HexDirection IncomingRiver, OutgoingRiver;

    [SerializeField] HexCell[] _neighbors;
    [SerializeField] bool[] _roads;

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

    public bool HasRoadThroughEdge(HexDirection direction) => _roads[(int)direction];

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

    public void SetIncomingRiver(HexDirection direction, HexCell neighbor)
    {
        // incoming river is already set
        if (HasIncomingRiver) return;

        // neighbor does not exists or its outgoing river is already set
        if (neighbor.HasOutgoingRiver) return;
        
        if (!IsValidRiverDestination(neighbor)) return;

        HasIncomingRiver = true;
        IncomingRiver = direction;

        neighbor.HasOutgoingRiver = true;
        neighbor.OutgoingRiver = direction.Opposite();

        // rivers wash out roads 
        SetRoad((int)direction, false);
    }

    public void SetOutgoingRiver(HexDirection direction, HexCell neighbor)
    {
        // incoming river is already set
        if (HasOutgoingRiver) return;

        // neighbor does not exists or its incoming river is already set
        if (neighbor.HasIncomingRiver) return;
        
        if (!IsValidRiverDestination(neighbor)) return;

        HasOutgoingRiver = true;
        IncomingRiver = direction;

        neighbor.HasIncomingRiver = true;
        neighbor.IncomingRiver = direction.Opposite();

        // rivers wash out roads and refresh the cell
        SetRoad((int)direction, false);
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

    public float RiverSurfaceY => (Elevation + HexMetrics.WaterSurfaceElevationOffset) * HexMetrics.ElevationStep;

    public float WaterSurfaceY => (_waterLevel + HexMetrics.WaterSurfaceElevationOffset) * HexMetrics.ElevationStep;

    public void AddRoad(HexDirection direction)
    {
        // rivers and roads cannot go in the same direction as well as the elevation difference cannot be greater than 1
        if (!_roads[(int)direction] && !HasRiverThroughEdge(direction) && GetElevationDifference(direction) <= 1)
            SetRoad((int)direction, true);
    }

    /// <summary>
    /// Returns elevation difference as an absolute number between this cell and the neighbor cell
    /// </summary>
    public int GetElevationDifference(HexDirection direction)
    {
        int difference = _elevation - GetNeighbor(direction).Elevation;
        return difference >= 0 ? difference : -difference;
    }

    public void RemoveRoads()
    {
        for (int i = 0; i < _neighbors.Length; i++)
            if (_roads[i])
                SetRoad(i, false);
    }

    bool IsValidRiverDestination(HexCell neighbor) => neighbor && (Elevation >= neighbor.Elevation || WaterLevel == neighbor.Elevation);

    void SetRoad(int index, bool state)
    {
        _roads[index] = state;
        _neighbors[index]._roads[(int)((HexDirection)index).Opposite()] = state;
        _neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    void ValidateRivers()
    {
        if (HasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(OutgoingRiver)))
        {
            RemoveOutgoingRiver();
        }
        if (HasIncomingRiver && !GetNeighbor(IncomingRiver).IsValidRiverDestination(this))
        {
            RemoveIncomingRiver();
        }
    }
}