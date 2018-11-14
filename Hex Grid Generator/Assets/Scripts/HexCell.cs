using UnityEngine;

public class HexCell : MonoBehaviour
{
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

            Refresh();
        }
    }
    int _elevation;

    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    public RectTransform UiRect;
    public HexGridChunk Chunk;

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
    Color _color;

    public HexCoordinates Coordinates;

    [SerializeField]
    HexCell[] _neighbors;
    
    public HexCell GetNeighbor(HexDirection direction) => _neighbors[(int)direction];

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction) 
        => HexMetrics.GetEdgeType(Elevation, _neighbors[(int)direction].Elevation);

    public HexEdgeType GetEdgeType(HexCell otherCell) 
        => HexMetrics.GetEdgeType(Elevation, otherCell.Elevation);

    void Refresh()
    {
        if (Chunk)
        {
            Chunk.Refresh();
            for (int i = 0; i < _neighbors.Length; i++)
            {
                HexCell neighbor = _neighbors[i];
                if (neighbor != null && neighbor.Chunk != Chunk)
                {
                    neighbor.Chunk.Refresh();
                }
            }
        }
    }
}