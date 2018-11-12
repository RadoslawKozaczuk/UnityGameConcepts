using UnityEngine;

public static class HexMetrics
{
    public const float ElevationStep = 5f;

    // Hexagon's anatomy
    // the edge's length (so also the distance from the center to any corner) is equal to 10
    // the outer radius is equal to 10 as well
    public const float OuterRadius = 10f;
    // the inner radius is equal to sqrt(3)/2 times the outer radius
    public const float InnerRadius = OuterRadius * 0.866025404f;

    public const float SolidFactor = 0.75f;
    public const float BlendFactor = 1f - SolidFactor;

    public const int TerracesPerSlope = 2;
    public const int TerraceSteps = TerracesPerSlope * 2 + 1;
    public const float HorizontalTerraceStepSize = 1f / TerraceSteps;
    public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

    public static Vector3[] Corners = {
        new Vector3(0f, 0f, OuterRadius),
        new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(0f, 0f, -OuterRadius),
        new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(0f, 0f, OuterRadius) // seventh and first are exactly the same to prevent IndexOutOfBonds exception
    };

    public static Vector3 GetFirstCorner(HexDirection direction) => Corners[(int)direction];
    public static Vector3 GetSecondCorner(HexDirection direction) => Corners[(int)direction + 1];

    public static Vector3 GetFirstSolidCorner(HexDirection direction) => Corners[(int)direction] * SolidFactor;
    public static Vector3 GetSecondSolidCorner(HexDirection direction) => Corners[(int)direction + 1] * SolidFactor;

    public static Vector3 GetBridge(HexDirection direction) 
        => (Corners[(int)direction] + Corners[(int)direction + 1]) * BlendFactor;

    // Interpolation between two values a and b is done with a third interpolator t. 
    // When t is 0, the result is a. When it is 1, the result is b. 
    // When t lies somewhere in between 0 and 1, a and b are mixed proportionally. 
    // Thus the formula for the interpolated result is (1 − t)a + tb.
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HorizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;

        // To only adjust Y on odd steps, we can use (step + 1) / 2. 
        // If we use an integer division, it will convert the sequence 1, 2, 3, 4 into 1, 1, 2, 2.
        float v = (step + 1) / 2 * VerticalTerraceStepSize;
        a.y += (b.y - a.y) * v;

        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HorizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
        {
            return HexEdgeType.Flat;
        }

        // If the level difference is exactly one step, then we have a slope. It doesn't matter whether the slope goes up or down.
        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1)
        {
            return HexEdgeType.Slope;
        }

        // in all other cases we have a cliff
        return HexEdgeType.Cliff;
    }
}