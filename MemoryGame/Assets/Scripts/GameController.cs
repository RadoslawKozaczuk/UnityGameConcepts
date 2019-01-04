using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{
    GameManager _manager;
    GameBoard _gameBoard;
    GameOptions _gameOptions;
    SymbolDatabase _symbolDatabase;

    public void Start()
    {
        _gameBoard = GetComponentInChildren<GameBoard>();
        _manager = GetComponent<GameManager>();
        _gameOptions = GetComponent<GameOptions>();
        _symbolDatabase = GetComponent<SymbolDatabase>();

        _gameBoard.TileRejected += GameBoardOnTileRejected;
        _gameBoard.TilePlaced += GameBoardOnTilePlaced;
        _gameBoard.AllTilesMatched += GameBoardOnAllTilesMatched;
    }
    
    public void Update()
    {
        if (!_manager.IsPlaying)
            return;

        _manager.TimeLeft -= Time.deltaTime;

        if (_manager.TimeLeft <= 0)
            _manager.EndGame(Assets.Scripts.GameEndStatus.Lost);
    }

    public void StartGame()
    {
        _symbolDatabase.SetAvailableSymbolCount(_gameOptions.TypesOfSymbolsCount);

        _manager.TimeLeft = _gameOptions.GameTimeLimit;
        _manager.Points = 0;

        _gameBoard.SetBoardDimensions(_gameOptions.BoardWidth, _gameOptions.BoardHeight);
        var tilesToMatch = _gameBoard.Tiles
            .OrderBy(t => UnityEngine.Random.Range(0f, 1f))
            .Take(_gameOptions.TilesToMatch);

        _gameBoard.BeginGame(tilesToMatch, _gameOptions.TimeTilesShown, AfterAnimation);
        _manager.IsGameBoardAnimating = true;
    }

    void AfterAnimation(GameBoard gameBoard)
    {
        _manager.IsGameBoardAnimating = false;
        _manager.StartGame();
    }

    void GameBoardOnAllTilesMatched(GameBoard gameBoard)
    {
        _manager.EndGame(Assets.Scripts.GameEndStatus.Won);
    }

    void GameBoardOnTilePlaced(GameBoard gameBoard, BoardTile tile)
    {
        _manager.TimeLeft += .5f;
        _manager.Points += 10;
    }

    void GameBoardOnTileRejected(GameBoard gameBoard, BoardTile tile, SymbolType symbol)
    {
        _manager.Points -= 5;
    }
}
