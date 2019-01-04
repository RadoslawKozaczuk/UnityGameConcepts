using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GutterTile : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public DraggedTile DraggedTilePrefab;

    Image _image;
    SymbolType _symbolType;
    bool _isOverDropsite;
    DraggedTile _currentDraggedTile;
    Vector2 _targetPositionForTile; // for smoothing the movement effect

    public SymbolType Type
    {
        get { return _symbolType; }
        set
        {
            _symbolType = value;
            _image.sprite = value.Normal;
        }
    }

    public void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void Update()
    {
        if (_currentDraggedTile == null)
            return;
        
        _currentDraggedTile.transform.localPosition = _isOverDropsite
            // smoothing the snap
            ? Vector2.Lerp(_currentDraggedTile.transform.localPosition, _targetPositionForTile, Time.deltaTime * 10)
            // snap directly to wherever the mouse is positioned
            : _targetPositionForTile;
    }

    public void OnDestroy()
    {
        if (_currentDraggedTile == null)
            return;

        Destroy(_currentDraggedTile.gameObject);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _currentDraggedTile = (DraggedTile)Instantiate(DraggedTilePrefab);

        // we want to position the tile in absolute fashion not in relation to someone's pivot
        var canvasRect = GetComponentInParent<Canvas>()
            .GetComponent<RectTransform>();

        _currentDraggedTile.transform.SetParent(canvasRect);
        _currentDraggedTile.Type = Type;

        // translate a screen point to world posiiton in the context of parent of the tile that we are moving
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, Camera.main, out localPoint);
        _targetPositionForTile = localPoint;
        _currentDraggedTile.transform.localPosition = localPoint;

        // disable the image that we are currently dragging
        _image.enabled = false;
        _isOverDropsite = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _image.enabled = true;
        _currentDraggedTile.Drop();
        _currentDraggedTile = null;
    }

    // this is invoked every drame
    public void OnDrag(PointerEventData eventData)
    {
        if (_isOverDropsite)
            return;

        // we are translating between the screen position and the position 
        // that is valid inside the parent of a thing that we are moving
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _currentDraggedTile.transform.parent.GetComponent<RectTransform>(),
            eventData.position,
            Camera.main,
            out localPoint);

        _targetPositionForTile = localPoint;
    }

    public void EnterBoardTile(BoardTile tile)
    {
        _isOverDropsite = true;

        var screenPointOfTile = RectTransformUtility.WorldToScreenPoint(Camera.main, tile.transform.position);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _currentDraggedTile.transform.parent.GetComponent<RectTransform>(),
            screenPointOfTile,
            Camera.main,
            out localPoint);

        _targetPositionForTile = localPoint;
        _currentDraggedTile.IsOverDropsite = true;
    }

    public void ExitBoardTile(BoardTile tile)
    {
        _isOverDropsite = false;
        _currentDraggedTile.IsOverDropsite = false;
    }
}
