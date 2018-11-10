using UnityEngine;

public class HexCell : MonoBehaviour
{
    [SerializeField]
    HexCell[] _neighbors;

    public Color Color;
    public HexCoordinates Coordinates;

    public HexCell GetNeighbor(HexDirection direction) => _neighbors[(int)direction];

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        _neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }
}