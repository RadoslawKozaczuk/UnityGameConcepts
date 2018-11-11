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
            _elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.ElevationStep;
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = _elevation * -HexMetrics.ElevationStep;
            uiRect.localPosition = uiPosition;
        }
    }
    int _elevation;
    
    public Color Color;
    public HexCoordinates Coordinates;

    [SerializeField]
    HexCell[] _neighbors;

    public RectTransform uiRect;

    public HexCell GetNeighbor(HexDirection direction) => _neighbors[(int)direction];

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }
}