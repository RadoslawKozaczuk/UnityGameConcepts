using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public HexCell Cell;
    public Text CellLabelPrefab;
    public Color DefaultColor = Color.white;
    public int Width = 6;
    public int Height = 6;
    
    HexMesh _hexMesh;
    HexCell[] _cells;
    Canvas _gridCanvas;

    void Awake()
    {
        _hexMesh = GetComponentInChildren<HexMesh>();
        _gridCanvas = GetComponentInChildren<Canvas>();
        _cells = new HexCell[Height * Width];

        for (int z = 0, i = 0; z < Height; z++)
            for (int x = 0; x < Width; x++)
                CreateCell(x, z, i++);
    }

    void Start() => _hexMesh.Triangulate(_cells);
    
    void CreateCell(int x, int z, int i)
    {
        Vector3 position;

        // Each row is offset along the X axis by the inner radius so we have to add half of z to x.
        // Every second row, all cells should move back one additional step. 
        // Subtracting the integer division of Z by 2 before multiplying will do the trick.
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.OuterRadius * 1.5f);

        HexCell cell = _cells[i] = Instantiate(Cell);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Color = DefaultColor;

        // As we go through the cells row by row, left to right, we know which cells have already been created. 
        // Those are the cells that we can connect to.
        if (x > 0)
            cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
        // We have two more bidirectional connections to make.
        // As these are between different grid rows, we can only connect with the previous row.
        // This means that we have to skip the first row entirely.
        if (z > 0)
        {
            // As the rows zigzag, they have to be treated differently.
            // Let's first deal with the even rows. As all cells in such rows have a SE neighbor, we can connect to those.
            if ((z & 1) == 0) // bitwise and used as a mask to get an even number (even number always has 0 as last number)
            {
                cell.SetNeighbor(HexDirection.SE, _cells[i - Width]);
                // We can connect to the SW neighbors as well. Except for the first cell in each row, as it doesn't have one.
                if (x > 0)
                    cell.SetNeighbor(HexDirection.SW, _cells[i - Width - 1]);
            }
            // The odds rows follow the same logic, but mirrored. Once that's done, all neighbors in our grid are connected.
            else
            {
                cell.SetNeighbor(HexDirection.SW, _cells[i - Width]);
                if (x < Width - 1)
                    cell.SetNeighbor(HexDirection.SE, _cells[i - Width + 1]);
            }
        }
        // Not every cell is connected to exactly six neighbors. 
        // The cells that form the border of our grid end up with at least two and at most five neighbors.
        
        Text label = Instantiate(CellLabelPrefab);
        label.rectTransform.SetParent(_gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.Coordinates.ToStringOnSeparateLines();
    }

    public void ColorCell(Vector3 position, Color color)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        
        int index = coordinates.X + coordinates.Z * Width + coordinates.Z / 2;
        HexCell cell = _cells[index];
        cell.Color = color;
        _hexMesh.Triangulate(_cells);
    }
}