using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class HexGrid : MonoBehaviour
{
    public HexCell Cell;
    public Text CellLabelPrefab;
    public Texture2D NoiseSource;
    public HexGridChunk ChunkPrefab;
    public int CellCountX = 10, CellCountZ = 5;
    public int Seed;

	// this would break Unity - components created with 'new' will not be able to start coroutines or anything Unity related
	// readonly Pathfinder _pathfinder = new Pathfinder();

	/* === Explaination ===
	    Coroutines are run by the coroutine scheduler and are bound to the MonoBehaviour that has been used to start the coroutine.
	    StartCoroutine is an instance member of MonoBehaviour.

		All Components MUST NOT be created with "new". Components always need to be created with AddComponent.
		Components can only "live" on GameObjects.

		Wrongly initialized Components (i.e. created with "new") will turn into "fake null objects".
		"Fake null objects" are true C# managed classes but they are lacking the native C++ equivalent in the engine's core.
		They could still be used as "normal" managed classes, but nothing related to Untiy will work.
		Furthermore the "UnityEngine.Object" baseclass overloads the == operator and "fakes" that the object is null
		when it's lacking the native part. That happens when you create a Component class with new or when you Destroy such an object.
	*/

	Pathfinder _pathfinder;

    HexCell[] _cells;
    HexGridChunk[] _chunks;
    int _chunkCountX, _chunkCountZ;

	void Awake()
    {
        HexMetrics.NoiseSource = NoiseSource;
        FeatureManager.InitializeHashGrid(Seed);
        CreateMap(CellCountX, CellCountZ);

		var pos = CellLabelPrefab.transform.position;
		var newPos = new Vector3(pos.x, pos.y + 1, pos.z);
		CellLabelPrefab.transform.position = newPos;

		_pathfinder = GetComponent<Pathfinder>();
	}

	void OnEnable()
	{
		if (!HexMetrics.NoiseSource)
		{
			HexMetrics.NoiseSource = NoiseSource;
			FeatureManager.InitializeHashGrid(Seed);
		}
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

	public void FindPath(HexCell from, HexCell to)
	{
		_pathfinder.FindPath(_cells, from, to);
	}

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z >= CellCountZ) return null;

        int x = coordinates.X + z / 2;
        if (x < 0 || x >= CellCountX) return null;

        return _cells[x + z * CellCountX];
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
		StopAllCoroutines();

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
		cell.Id = i;
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