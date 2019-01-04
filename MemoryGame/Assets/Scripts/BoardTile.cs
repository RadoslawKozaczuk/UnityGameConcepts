using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BoardTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler {

    SymbolType _symbolType;
    Animator _animator;

    public Image SpriteTarget;

    public GameBoard GameBoard { get; set; }

    public bool IsFlipped
    {
        get { return _animator.GetBool("IsFlipped"); }
        set { _animator.SetBool("IsFlipped", value); }
    }

    public bool IsHovering
    {
        get { return _animator.GetBool("IsHovering"); }
        set { _animator.SetBool("IsHovering", value); }
    }

    public bool IsPlaced
    {
        get { return _animator.GetBool("IsPlaced"); }
        set
        {
            _animator.SetBool("IsPlaced", value);
            SpriteTarget.sprite = value ? _symbolType.Dropped : _symbolType.Normal;
        }
    }

    public SymbolType Type
    {
        get { return _symbolType; }
        set
        {
            _symbolType = value;
            IsPlaced = IsPlaced;
        }
    }

    public void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void ResetGame()
    {
        IsPlaced = IsHovering = IsFlipped = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsPlaced || eventData.pointerDrag == null)
            return;

        var gutterTile = eventData.pointerDrag.GetComponent<GutterTile>();
        if (gutterTile == null)
            return;

        IsHovering = true;
        gutterTile.EnterBoardTile(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsPlaced || eventData.pointerDrag == null)
            return;

        var gutterTile = eventData.pointerDrag.GetComponent<GutterTile>();
        if (gutterTile == null)
            return;

        IsHovering = false;
        gutterTile.ExitBoardTile(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        IsHovering = false;

        if (IsPlaced || eventData.pointerDrag == null)
            return;

        var gutterTile = eventData.pointerDrag.GetComponent<GutterTile>();
        if (gutterTile == null)
            return;
    }
}
