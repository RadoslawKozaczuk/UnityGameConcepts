using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    [SerializeField] Color[] _colors;
    [SerializeField] HexGrid _hexGrid;
    Color _activeColor;
    int _activeElevation, _brushSize;
    bool _applyColor, _applyElevation = true;

    void Awake() => SelectColor(0);

    void Update()
    {
        if (Input.GetMouseButton(0) 
            // The EventSystem knows only about the UI objects 
            // so we can ask him if the cursor is above something at the moment of click
            // and if not it means we can normally process input.
            // This is done so to avoid undesirable double interacting with the UI and the grid at the same time.
            && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
            EditCells(_hexGrid.GetCell(hit.point));
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
        if (_applyColor)
            cell.Color = _activeColor;

        if (_applyElevation)
            cell.Elevation = _activeElevation;
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

    public void SetBrushSize(float size)
    {
        _brushSize = (int)size;
    }
}