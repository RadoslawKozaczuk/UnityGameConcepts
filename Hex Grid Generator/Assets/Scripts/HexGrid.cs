using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class HexGrid : MonoBehaviour
{
    public Color[] Colors;
    public HexCell Cell;
    public Text CellLabelPrefab;
    public Texture2D NoiseSource;
    public HexGridChunk ChunkPrefab;
    public int CellCountX = 10, CellCountZ = 5;
    public int Seed;

    HexCell[] _cells;
    HexGridChunk[] _chunks;
    int _chunkCountX, _chunkCountZ;

    void Awake()
    {
        HexMetrics.NoiseSource = NoiseSource;
        FeatureManager.InitializeHashGrid(Seed);
        HexMetrics.Colors = Colors;
        CreateMap(CellCountX, CellCountZ);
    }

    public void CreateMap(int x, int z)
    {
        if (x <= 0 || x % HexMetrics.ChunkSizeX != 0 || z <= 0 || z % HexMetrics.ChunkSizeZ != 0)
        {
            Debug.LogError("Unsupported map size.");
            return;
        }

        if (_chunks != null)
            for (int i = 0; i < _chunks.Length; i++)
                Destroy(_chunks[i].gameObject);

        CellCountX = x;
        CellCountZ = z;

        _chunkCountX = CellCountX / HexMetrics.ChunkSizeX;
        _chunkCountZ = CellCountZ / HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    void OnEnable()
    {
        if (!HexMetrics.NoiseSource)
        {
            HexMetrics.NoiseSource = NoiseSource;
            FeatureManager.InitializeHashGrid(Seed);
            HexMetrics.Colors = Colors;
        }
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z >= CellCountZ) return null;

        int x = coordinates.X + z / 2;
        if (x < 0 || x >= CellCountX) return null;
        
        return _cells[x + z * CellCountX];
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < _chunks.Length; i++)
            _chunks[i].ShowUI(visible);
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(CellCountX);
        writer.Write(CellCountZ);

        for (int i = 0; i < _cells.Length; i++)
            _cells[i].Save(writer);
    }

    public void Load(BinaryReader reader)
    {
        CreateMap(reader.ReadInt32(), reader.ReadInt32());

        for (int i = 0; i < _cells.Length; i++)
            _cells[i].Load(reader);

        for (int i = 0; i < _chunks.Length; i++)
            _chunks[i].Refresh();
    }

    // Get cell returns cell from a given position
    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * CellCountX + coordinates.Z / 2;
        return _cells[index];
    }

    void CreateChunks()
    {
        _chunks = new HexGridChunk[_chunkCountX * _chunkCountZ];

        for (int z = 0, i = 0; z < _chunkCountZ; z++)
            for (int x = 0; x < _chunkCountX; x++)
            {
                HexGridChunk chunk = _chunks[i++] = Instantiate(ChunkPrefab);
                chunk.transform.SetParent(transform);
            }
    }

    void CreateCells()
    {
        _cells = new HexCell[CellCountX * CellCountZ];

        for (int z = 0, i = 0; z < CellCountZ; z++)
            for (int x = 0; x < CellCountX; x++)
                CreateCell(x, z, i++);
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
        cell.transform.localPosition = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        // As we go through the cells row by row, left to right, we know which cells have already been created. 
        // Those are the cells that we can connect to.
        if (x > 0)
            cell.SetNeighbor(HexDirection.West, _cells[i - 1]);
        // We have two more bidirectional connections to make.
        // As these are between different grid rows, we can only connect with the previous row.
        // This means that we have to skip the first row entirely.
        if (z > 0)
        {
            // As the rows zigzag, they have to be treated differently.
            // Let's first deal with the even rows. As all cells in such rows have a SE neighbor, we can connect to those.
            if ((z & 1) == 0) // bitwise and used as a mask to get an even number (even number always has 0 as last number)
            {
                cell.SetNeighbor(HexDirection.SouthEast, _cells[i - CellCountX]);
                // We can connect to the SW neighbors as well. Except for the first cell in each row, as it doesn't have one.
                if (x > 0)
                    cell.SetNeighbor(HexDirection.SouthWest, _cells[i - CellCountX - 1]);
            }
            // The odds rows follow the same logic, but mirrored. Once that's done, all neighbors in our grid are connected.
            else
            {
                cell.SetNeighbor(HexDirection.SouthWest, _cells[i - CellCountX]);
                if (x < CellCountX - 1)
                    cell.SetNeighbor(HexDirection.SouthEast, _cells[i - CellCountX + 1]);
            }
        }
        // Not every cell is connected to exactly six neighbors. 
        // The cells that form the border of our grid end up with at least two and at most five neighbors.

        Text label = Instantiate(CellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.Coordinates.ToStringOnSeparateLines();

        cell.UiRect = label.rectTransform;
        cell.Elevation = 0;

        // default values
        cell.Elevation = 1;

        AddCellToChunk(x, z, cell);
    }

    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.ChunkSizeX;
        int chunkZ = z / HexMetrics.ChunkSizeZ;
        HexGridChunk chunk = _chunks[chunkX + chunkZ * _chunkCountX];

        int localX = x - chunkX * HexMetrics.ChunkSizeX;
        int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);
    }
}