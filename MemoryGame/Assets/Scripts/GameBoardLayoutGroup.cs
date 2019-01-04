using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteInEditMode]
public class GameBoardLayoutGroup
    // inherites from MonoBehavior
    // funcionally there is no difference when we inherite from UIBehavior or MonoBehavior
    // but it better from the object oriented programming perspective
    : UIBehaviour,
    ILayoutGroup
{
    public Vector2 CellSize;
    public int CellsPerRow;

    // caching the RectTranform
    // we need caching here because Awake or Start method may not alaways be invoked
    // when we are in the Editor
    // so if we want to play around with things in the Editor
    // we need a method that works regardless whether we are in the Play mode or not
    public RectTransform Rect
    {
        get { return _rect ?? (_rect = GetComponent<RectTransform>()); }
    }
    private RectTransform _rect;

    public void SetLayoutHorizontal()
    {
        // ignore the invalid values
        if (CellsPerRow <= 0)
            return;

        // how many columns we need to create
        var columnCount = Mathf.Ceil(Rect.childCount / (float)CellsPerRow) - 1;

        var currentRow = 0;
        var currentColumn = 0;

        // we calculate the left edge
        // all the children has their pivot in the center
        // so we have to offset them to the right by the half of the cell size
        var startX = -CellsPerRow * CellSize.x / 2f + (CellSize.x / 2);
        var startY = -columnCount * CellSize.y / 2f;

        for(var i = 0; i < Rect.childCount; i++)
        {
            // try to perform cast, if it fails the value will be null
            var child = Rect.GetChild(i) as RectTransform;
            if (child == null) continue;

            child.sizeDelta = CellSize;

            // anchored position is going to be the distance between
            // the anchors (center of the parent in our case) 
            // and the position of the object (center of the object in our case)
            child.anchoredPosition = new Vector2(startX + currentColumn * CellSize.x, startY + currentRow * CellSize.y);

            currentColumn++;

            if(currentColumn % CellsPerRow == 0)
            {
                currentColumn = 0;
                currentRow++;
            }
        }

        // set the size of the game board
        Rect.sizeDelta = new Vector2(CellsPerRow * CellSize.x, (columnCount + 1) * CellSize.y);
    }

    public void SetLayoutVertical()
    {
    }

    protected override void OnValidate()
    {
        if (CellsPerRow < 1)
            CellsPerRow = 1;

        if (CellSize.x < 1)
            CellSize.x = 1;

        if (CellSize.y < 1)
            CellSize.y = 1;

        // rebuild after chaning values
        SetDirty();
    }

    // happends whenever we resize the object
    protected override void OnRectTransformDimensionsChange()
    {
        SetDirty();
    }

    protected override void OnTransformParentChanged()
    {
        SetDirty();
    }

    protected virtual void OnTranformChildreanChanged()
    {
        SetDirty();
    }

    private void SetDirty()
    {
        if(CanvasUpdateRegistry.IsRebuildingLayout())
        {
            // we are already in the process of rebuilding
            // no reason to ask for it again
            return;
        }

        LayoutRebuilder.MarkLayoutForRebuild(Rect);
    }
}
