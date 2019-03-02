public enum HexDirection
{
    NorthEast, East, SouthEast, SouthWest, West, NorthWest
}

/// <summary>
/// Edge type describes the elevation difference between hexes.
/// Flat - both hexes are at the same level.
/// Slope - elevation difference is equal to 1.
/// Cliff - elevation difference is equal or greater than 2.
/// </summary>
public enum HexEdgeType
{
    Flat,  // same elevation
    Slope, // elevation difference is 1
    Cliff  // elevation difference is 2 or more
}

public enum EditModes
{
    Ignore, Add, Remove
}

/// <summary>
/// Different terrain types not only look different but also provide different movement speed.
/// </summary>
public enum TerrainTypes
{
	None = -1, Grass, Mud, Sand, Snow, Dirt
}