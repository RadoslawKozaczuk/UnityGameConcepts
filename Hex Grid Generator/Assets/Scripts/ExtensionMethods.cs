public static class HexDirectionExtensions
{
    public static HexDirection Opposite(this HexDirection direction) 
        => (int)direction < 3 ? (direction + 3) : (direction - 3);

    public static HexDirection Previous(this HexDirection direction) 
        => direction == HexDirection.NE ? HexDirection.NW : (direction - 1);

    public static HexDirection Next(this HexDirection direction) 
        => direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
}
