static class GameSettings
{
	static readonly int[] _movementCost = new int[3] { 3, 3, 5 };

	public static int GetMovementCost(TerrainTypes terrainType) => _movementCost[(int)terrainType];
}
