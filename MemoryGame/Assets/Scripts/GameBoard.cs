using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class GameBoard : MonoBehaviour
{
    private List<BoardTile> _boardTiles;
    private IEnumerator<BoardTile> _targetSlot;
    bool _isAnimating;
    
    // first parameter is the instigator of the event
    // it is a good patern to do so
    // this is event the game board is going to fire where a tile is placed
    public event Action<GameBoard, BoardTile> TilePlaced;

    // this happends when a tile is not placed properly
    public event Action<GameBoard, BoardTile, SymbolType> TileRejected;

    // this is fired when everything is matched
    public event Action<GameBoard> AllTilesMatched;

    public BoardTile BoardTilePrefab;

    SymbolDatabase _symbolDatabase;

    public IEnumerable<BoardTile> Tiles { get { return _boardTiles; } }

    public BoardTile NextTileToMatch
    {
        get
        {
            return _targetSlot == null 
                ? null 
                : _targetSlot.Current;
        }
    }
    
    public void Awake()
    {
        _symbolDatabase = GetComponentInParent<SymbolDatabase>();
        _boardTiles = new List<BoardTile>();
    }
    
    public void SetBoardDimensions(int width, int height)
    {
        if (_isAnimating)
            throw new InvalidOperationException("Cannot reset board while animating");

        DestroyAllChildten();

        _boardTiles.Clear();

        GetComponent<GameBoardLayoutGroup>().CellsPerRow = width;

        for(var i = 0; i < width * height; i++)
        {
            var boardTile = Instantiate(BoardTilePrefab);
            boardTile.GameBoard = this;
            boardTile.transform.SetParent(transform);

            // we have to reset the scale due to the weird behavior of Unity Editor 
            // when the new objact is instantiated
            boardTile.transform.localScale = new Vector3(1, 1, 1);
            boardTile.transform.localPosition = new Vector3();
            boardTile.Type = _symbolDatabase.GetRandomSymbol();

            _boardTiles.Add(boardTile);
        }

        transform.localPosition = new Vector3();
    }

    void DestroyAllChildten()
    {
        // tranform object is IEnumerable
        // so we can foreach loop over it
        // but unfortunately it is IEnumerable not IEnumerable<T>
        // so we have to use OfType<Transform> on it before we can ToList() it
        // we ToList() it to have a copy that we can modify
        foreach (var child in transform.OfType<Transform>().ToList())
            Destroy(child.gameObject);

        // we can NOT have open iterater why we modify the list
        // that we iterate over it will throw an exception
        //foreach (Transform child in transform)
        //    Destroy(child.gameObject);
    }

    public void ShuffleBoard()
    {
        foreach (var tile in _boardTiles)
            tile.Type = _symbolDatabase.GetRandomSymbol();
    }

    public void ResetBoard()
    {
        foreach (var tile in _boardTiles)
            tile.ResetGame();
    }

    public void BeginGame(IEnumerable<BoardTile> targetTiles, float timePerTile, Action<GameBoard> afterAnimation)
    {
        if (_isAnimating)
            throw new InvalidOperationException("Cannot start game while animating");

        // IEnumerable is a sequence - so we need to flatten it
        // it may safe as potentially a lot of problems with a poorly written IEnumerable logic
        // is is just a good practice
        var goalTiles = targetTiles.ToList();

        _targetSlot = goalTiles.GetEnumerator();

        // someone put the empty IEnumerable
        if(!_targetSlot.MoveNext())
        {
            Debug.LogError("No target slots selected for game!");
            return;
        }

        _isAnimating = true;
        StartCoroutine(BeginGameCoroutine(goalTiles, timePerTile, afterAnimation));
    }

    private IEnumerator BeginGameCoroutine(IEnumerable<BoardTile> targetTiles, float timePerTile, Action<GameBoard> afterAnimation)
    {
        foreach(var tile in targetTiles)
        {
            tile.IsFlipped = true;
            yield return new WaitForSeconds(timePerTile);
            tile.IsFlipped = false;
        }

        _isAnimating = false;
        afterAnimation(this); // inform whoever inforem the coroutine that the animation is over
    }

    public bool PlaceTile(BoardTile boardTile, SymbolType symbolType)
    {
        if (_targetSlot == null || _targetSlot.Current == null)
            throw new InvalidOperationException("Game must be started to place tile!");

        if(_targetSlot.Current != boardTile
            || symbolType != boardTile.Type)
        {
            if (TileRejected != null)
                TileRejected(this, boardTile, _targetSlot.Current.Type);

            return false;
        }

        boardTile.IsPlaced = true;

        // we know player placed the right tile at this moment
        var isFinished = !_targetSlot.MoveNext(); // true if no more tiles to do

        if (TilePlaced != null)
            TilePlaced(this, boardTile);

        if (isFinished && AllTilesMatched != null)
            AllTilesMatched(this);

        if (isFinished)
            _targetSlot = null;

        return true;
    }
}
