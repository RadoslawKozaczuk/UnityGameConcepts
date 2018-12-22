public static class HexDirectionExtensions
{
    /// <summary>
    /// Returns the opposite direction to the given direction
    /// </summary>
    public static HexDirection Opposite(this HexDirection direction) 
        => (int)direction < 3 ? (direction + 3) : (direction - 3);

    /// <summary>
    /// Returns next direction from the given direction anti-clockwise
    /// </summary>
    public static HexDirection Previous(this HexDirection direction) 
        => direction == HexDirection.NorthEast ? HexDirection.NorthWest : (direction - 1);

    /// <summary>
    /// Returns next direction from the given direction clockwise
    /// </summary>
    public static HexDirection Next(this HexDirection direction) 
        => direction == HexDirection.NorthWest ? HexDirection.NorthEast : (direction + 1);

    /// <summary>
    /// Returns second next direction from the given direction anti-clockwise
    /// </summary>
    public static HexDirection Previous2(this HexDirection direction)
    {
        direction -= 2;
        return direction >= HexDirection.NorthEast ? direction : (direction + 6);
    }

    /// <summary>
    /// Returns second next direction from the given direction clockwise
    /// </summary>
    public static HexDirection Next2(this HexDirection direction)
    {
        direction += 2;
        return direction <= HexDirection.NorthWest ? direction : (direction - 6);
    }
}
