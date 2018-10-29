using UnityEngine;

public static class HexMetrics
{
    // Hexagon's anatomy
    // the edge's length (so also the distance from the center to any corner) is equal to 10
    // the outer radius is equal to 10 as well
    public const float OuterRadius = 10f;
    // the inner radius is equal to sqrt(3)/2 times the outer radius
    public const float InnerRadius = OuterRadius * 0.866025404f;

    public static Vector3[] corners = {
        new Vector3(0f, 0f, OuterRadius),
        new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(0f, 0f, -OuterRadius),
        new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius)
    };
}