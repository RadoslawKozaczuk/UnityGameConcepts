using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
	[SerializeField] Unit _unitPrefab;
	[SerializeField] Material _terrainMaterial;
	[SerializeField] HexGrid _hexGrid;

    HexCell _previousCell, _searchFromCell, _searchToCell;
	HexDirection _dragDirection;
    EditModes _riverMode, _roadMode;

	// TODO these default values should be read from the interface not hardcoded
	TerrainTypes _activeTerrainType = TerrainTypes.Grass;
	int _activeElevation = 1, _brushSize, _activeWaterLevel;
	bool _applyElevation = true, _applyWaterLevel = true, _isDrag, _editMode;

	void Awake() => _terrainMaterial.DisableKeyword("GRID_ON");

	void Update()
    {
        // The EventSystem knows only about the UI objects
        // so we can ask him if the cursor is above something at the moment of click
        // and if not it means we can normally process input.
        // This is done so to avoid undesirable double interacting with the UI and the grid at the same time.
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (Input.GetMouseButton(0))
			{
				HandleInput();
				return;
			}
			if (Input.GetKeyDown(KeyCode.U))
			{
				if (Input.GetKey(KeyCode.LeftShift))
					DestroyUnit();
				else
					CreateUnit();

				return;
			}
		}

		_previousCell = null;
	}

    public void SetRiverMode(int mode) => _riverMode = (EditModes)mode;

	public void SetTerrainType(int type) => _activeTerrainType = (TerrainTypes)type;

	public void SetElevation(float elevation) => _activeElevation = (int)elevation;

	public void SetApplyElevation(bool toggle) => _applyElevation = toggle;

	public void SetBrushSize(float size) => _brushSize = (int)size;

	public void ToggleTerrainPerturbation() => HexMetrics.ElevationPerturbFlag = !HexMetrics.ElevationPerturbFlag;

	public void RecreateMap() { }

	public void SetRoadMode(int mode) => _roadMode = (EditModes)mode;

	public void SetApplyWaterLevel(bool toggle) => _applyWaterLevel = toggle;

	public void SetWaterLevel(float level) => _activeWaterLevel = (int)level;

	public void ShowGrid(bool visible)
	{
		if (visible)
			_terrainMaterial.EnableKeyword("GRID_ON");
		else
			_terrainMaterial.DisableKeyword("GRID_ON");
	}

	public void SetEditMode(bool toggle) => _editMode = toggle;

	void HandleInput()
    {
		HexCell currentCell = GetCellUnderCursor();
		if(currentCell == null)
		{
			_previousCell = null;
			return;
		}

		if (_previousCell && _previousCell != currentCell)
			ValidateDrag(currentCell);
		else
			_isDrag = false;

		if (_editMode)
			EditCells(currentCell);
		else if (Input.GetKey(KeyCode.LeftShift) && _searchToCell != currentCell)
		{
			if (_searchFromCell)
				_searchFromCell.DisableHighlight();

			_searchFromCell = currentCell;
			_searchFromCell.EnableHighlight(Color.blue);

			if (_searchToCell)
				_hexGrid.FindPath(_searchFromCell, _searchToCell);
		}
		else if(_searchFromCell && _searchFromCell != currentCell)
		{
			_searchToCell = currentCell;
			currentCell.EnableHighlight(Color.red);
			_hexGrid.FindPath(_searchFromCell, _searchToCell);
		}

		_previousCell = currentCell;
    }

	HexCell GetCellUnderCursor()
	{
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);

		return Physics.Raycast(inputRay, out RaycastHit hit)
			? _hexGrid.GetCell(hit.point)
			: null;
	}

	void CreateUnit()
	{
		HexCell cell = GetCellUnderCursor();
		if (cell && !cell.Unit)
		{
			Unit unit = Instantiate(_unitPrefab);
			unit.transform.SetParent(_hexGrid.transform, false);
			unit.Location = cell;
			unit.Orientation = Random.Range(0f, 360f);
		}
	}

	void DestroyUnit()
	{
		HexCell cell = GetCellUnderCursor();
		if (cell && cell.Unit)
			cell.Unit.Die();
	}

	void ValidateDrag(HexCell currentCell)
    {
        for (_dragDirection = HexDirection.NorthEast; _dragDirection <= HexDirection.NorthWest; _dragDirection++)
        {
            if (_previousCell.GetNeighbor(_dragDirection) == currentCell)
            {
                _isDrag = true;
                return;
            }
        }
        _isDrag = false;
    }

    void EditCells(HexCell center)
    {
        int centerX = center.Coordinates.X;
        int centerZ = center.Coordinates.Z;

        for (int r = 0, z = centerZ - _brushSize; z <= centerZ; z++, r++)
            for (int x = centerX - r; x <= centerX + _brushSize; x++)
                EditCell(_hexGrid.GetCell(new HexCoordinates(x, z)));

        for (int r = 0, z = centerZ + _brushSize; z > centerZ; z--, r++)
            for (int x = centerX - _brushSize; x <= centerX + r; x++)
                EditCell(_hexGrid.GetCell(new HexCoordinates(x, z)));
    }

	void EditCell(HexCell cell)
	{
		if (cell)
		{
			if (_activeTerrainType != TerrainTypes.None)
				cell.TerrainType = _activeTerrainType;

			if (_applyElevation)
				cell.Elevation = _activeElevation;

			if (_riverMode == EditModes.Remove)
				cell.RemoveRiver();

			if (_roadMode == EditModes.Remove)
				cell.RemoveRoads();

			if (_applyWaterLevel)
				cell.WaterLevel = _activeWaterLevel;

			if (_isDrag)
			{
				var oppositeDir = _dragDirection.Opposite();
				HexCell otherCell = cell.GetNeighbor(oppositeDir);
				if (otherCell)
				{
					if (_riverMode == EditModes.Add)
						cell.SetIncomingRiver(oppositeDir, otherCell);
					if (_roadMode == EditModes.Add)
						otherCell.AddRoad(_dragDirection);
				}
			}
		}
	}
}