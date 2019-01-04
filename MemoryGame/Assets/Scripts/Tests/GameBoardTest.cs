using System.Linq;
using UnityEngine;

public class GameBoardTest : MonoBehaviour
{
    public GameBoard GameBoard;

    private SymbolDatabase _symbolDatabase;
    private GameOptions _gameOptions;
    bool _isPlaying;

    public void Start()
    {
        // both of these are acuaried by looking in the herarchy
        _symbolDatabase = GetComponentInParent<SymbolDatabase>();
        _gameOptions = GetComponentInParent<GameOptions>();

        GameBoard.TilePlaced += GameBoardOnTilePlaced;
        GameBoard.TileRejected += GameBoardOnTileRejected;
        GameBoard.AllTilesMatched += GameBoardOnAllTilesMatched;
    }

    public void StartGame()
    {
        _symbolDatabase.SetAvailableSymbolCount(3); // hardcoded for now

        GameBoard.SetBoardDimensions(_gameOptions.BoardWidth, _gameOptions.BoardHeight);

        // randomly select tiles to match
        var tilesToMatch = GameBoard.Tiles
            .OrderBy(v => UnityEngine.Random.Range(0f, 1f))
            .Take(_gameOptions.TilesToMatch);

        GameBoard.BeginGame(tilesToMatch, _gameOptions.TimeTilesShown, AfterAnimation);
    }

    public void PlaceInvalidTile()
    {
        var boardTile = GameBoard.Tiles.
            FirstOrDefault( // find the first or none
            t => t != GameBoard.NextTileToMatch // criteria is not the real next one
            && !t.IsPlaced);
        
        GameBoard.PlaceTile(boardTile, boardTile.Type); // and not placed
    }

    public void PlaceValidTile()
    {
        GameBoard.PlaceTile(GameBoard.NextTileToMatch, GameBoard.NextTileToMatch.Type);
    }

    // this method will be invoked when the boardd is one showing the ti;es to the user
    void AfterAnimation(GameBoard gameBoard)
    {
        Debug.Log("Game is Ready");
    }

    void GameBoardOnAllTilesMatched(GameBoard obj)
    {
        Debug.Log("All tiles matched!");
        _isPlaying = false;
    }

    void GameBoardOnTileRejected(GameBoard gameBoard, BoardTile boardTile, SymbolType symbol)
    {
        // allows us to see exactly which tile didnt match
        Debug.Log(string.Format("Tile {0} rejected on slot {1}", symbol.Id, boardTile.Type.Id), boardTile);
    }

    void GameBoardOnTilePlaced(GameBoard gameBoard, BoardTile boardTile)
    {
        Debug.Log(string.Format("Tile {0} placed", boardTile.Type.Id));
    }
}
