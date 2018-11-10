public static class HexDirectionExtensions
{
    public static HexDirection Opposite(this HexDirection direction) 
        => (int)direction < 3 ? (direction + 3) : (direction - 3);
}
