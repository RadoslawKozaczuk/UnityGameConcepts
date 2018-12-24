using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    [SerializeField] Color[] _colors;
    [SerializeField] HexGrid _hexGrid;
    HexCell _previousCell;
    Color _activeColor;
    int _activeElevation, _brushSize;
    bool _applyColor = false, _applyElevation = true, _isDrag;
    HexDirection _dragDirection;
    EditModes _riverMode, _roadMode;

    void Awake() => SelectColor(0);

    void Update()
    {
        // The EventSystem knows only about the UI objects 
        // so we can ask him if the cursor is above something at the moment of click
        // and if not it means we can normally process input.
        // This is done so to avoid undesirable double interacting with the UI and the grid at the same time.
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
            HandleInput();
        else
            _previousCell = null;
    }

    public void SetRiverMode(int mode)
    {
        _riverMode = (EditModes)mode;
        Debug.Log("river mode: " + (int)_riverMode);
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(inputRay, out RaycastHit hit))
        {
            HexCell currentCell = _hexGrid.GetCell(hit.point);
            if (_previousCell && _previousCell != currentCell)
                ValidateDrag(currentCell);
            else
                _isDrag = false;
            EditCells(currentCell);
            _previousCell = currentCell;
        }
        else
            _previousCell = null;
    }

    void ValidateDrag(HexCell currentCell)
    {
        for (_dragDirection = HexDirection.NorthEast; _dragDirection <= HexDirection.NorthWest; _dragDirection++)
        {
            if (_previousCell.GetNeighbor(_dragDirection) == currentCell)
            {
                _isDrag = true;
                Debug.Log("Drag direction: " + _dragDirection.ToString());
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
            if (_applyColor)
                cell.Color = _activeColor;

            if (_applyElevation)
                cell.Elevation = _activeElevation;

            if (_riverMode == EditModes.Remove)
                cell.RemoveRiver();

            if (_roadMode == EditModes.Remove)
                cell.RemoveRoads();

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

    public void SelectColor(int index)
    {
        _applyColor = index >= 0;
        if (_applyColor)
            _activeColor = _colors[index];
    }

    public void SetElevation(float elevation)
    {
        if (_applyColor)
            _activeElevation = (int)elevation;
    }

    public void SetApplyElevation(bool toggle) => _applyElevation = toggle;

    public void SetBrushSize(float size) => _brushSize = (int)size;

    public void ShowUI(bool visible) => _hexGrid.ShowUI(visible);

    public void ToggleTerrainPerturbation() => HexMetrics.ElevationPerturbFlag = !HexMetrics.ElevationPerturbFlag;

    public void RecreateMap()
    {

    }

    public void SetRoadMode(int mode) => _roadMode = (EditModes)mode;
}