using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public Color[] Colors;
    public HexGrid HexGrid;
    private Color _activeColor;
    int _activeElevation;

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
            EditCell(HexGrid.GetCell(hit.point));
    }

    void EditCell(HexCell cell)
    {
        cell.Color = _activeColor;
        cell.Elevation = _activeElevation;
    }

    public void SelectColor(int index) => _activeColor = Colors[index];
    public void SetElevation(float elevation) => _activeElevation = (int)elevation;
}