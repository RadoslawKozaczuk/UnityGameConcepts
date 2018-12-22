public enum HexDirection
{
    NorthEast, East, SouthEast, SouthWest, West, NorthWest
}

public enum HexEdgeType
{
    Flat,  // same elevation
    Slope, // elevation difference is 1
    Cliff  // elevation difference is 2 or more
}

public enum RiverToggle
{
    Ignore, Add, Remove
}