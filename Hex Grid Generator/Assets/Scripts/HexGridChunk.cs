using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] _cells;
    HexMesh _hexMesh;
    Canvas _gridCanvas;

    void Awake()
    {
        _gridCanvas = GetComponentInChildren<Canvas>();
        _hexMesh = GetComponentInChildren<HexMesh>();
        _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
    }

    void Start() => _hexMesh.Triangulate(_cells);

    public void AddCell(int index, HexCell cell)
    {
        _cells[index] = cell;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(_gridCanvas.transform, false);
    }
}