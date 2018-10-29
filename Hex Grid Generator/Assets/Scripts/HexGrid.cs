using UnityEngine;

public class HexGrid : MonoBehaviour
{
    public HexCell Cell;
    public int Width = 6;
    public int Height = 6;
    
    HexCell[] _cells;

    void Awake()
    {
        _cells = new HexCell[Height * Width];

        for (int z = 0, i = 0; z < Height; z++)
            for (int x = 0; x < Width; x++)
                CreateCell(x, z, i++);
    }

    void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = x * 10f;
        position.y = 0f;
        position.z = z * 10f;

        HexCell cell = _cells[i] = Instantiate(Cell);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
    }
}