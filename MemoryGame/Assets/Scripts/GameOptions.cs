using UnityEngine;

public class GameOptions : MonoBehaviour
{
    public int BoardWidth = 3;
    public int BoardHeight = 3;
    public int TilesToMatch = 3;
    public int TypesOfSymbolsCount = 2;

    /// <summary>
    /// How long a single tile is animated during the preview
    /// </summary>
    public float TimeTilesShown = 1f;

    public float GameTimeLimit = 40;

    // sliders pass values as floats
    // thats why we need some sort of method that accepts float value
    public void SetBoardWidth(float value)
    {
        BoardWidth = (int)value;
    }

    public void SetBoardHeight(float value)
    {
        BoardHeight = (int)value;
    }

    public void SetTilesToMatch(float value)
    {
        TilesToMatch = (int)value;
    }

    public void SetTypesOfSymbolsCount(float value)
    {
        TypesOfSymbolsCount = (int)value;
    }

    public void SetTimePerTile(float value)
    {
        TimeTilesShown = value;
    }

    public void SetGameTimeLimit(float value)
    {
        GameTimeLimit = value;
    }
}
