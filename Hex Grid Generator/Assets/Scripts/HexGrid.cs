using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public HexCell Cell;
    public Text CellLabelPrefab;
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

    void Start()
    {
        _hexMesh.Triangulate(_cells);
    }

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

        Text label = Instantiate(CellLabelPrefab);
        label.rectTransform.SetParent(_gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.Coordinates.ToStringOnSeparateLines();
    }
}