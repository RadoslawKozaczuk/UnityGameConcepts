static class GameSettings
{
	static int[] _movementCost = new int[3] { 3, 3, 5 };

	public static int GetMovementCost(TerrainTypes terrainType)
	{
		return _movementCost[(int)terrainType];
	}
}
