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

        ShowUI(false);
    }
    
    void LateUpdate()
    {
        _hexMesh.Triangulate(_cells);
        enabled = false;
    }

    public void Refresh() => enabled = true;

    public void AddCell(int index, HexCell cell)
    {
        _cells[index] = cell;
        cell.Chunk = this;
        cell.transform.SetParent(transform, false);
        cell.UiRect.SetParent(_gridCanvas.transform, false);
    }

    public void ShowUI(bool visible)
    {
        _gridCanvas.gameObject.SetActive(visible);
    }
}