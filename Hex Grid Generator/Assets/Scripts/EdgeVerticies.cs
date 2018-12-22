using UnityEngine;

public struct EdgeVertices
{
    // vertices are ordered clockwise along the cell's edge
    public Vector3 V1, V2, V3, V4, V5; // v3 is used by rivers

    public EdgeVertices(Vector3 startPoint, Vector3 endPoint)
    {
        V1 = startPoint;
        V2 = Vector3.Lerp(startPoint, endPoint, 0.25f);
        V3 = Vector3.Lerp(startPoint, endPoint, 0.5f);
        V4 = Vector3.Lerp(startPoint, endPoint, 0.75f);
        V5 = endPoint;
    }

    public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
    {
        V1 = corner1;
        V2 = Vector3.Lerp(corner1, corner2, outerStep);
        V3 = Vector3.Lerp(corner1, corner2, 0.5f);
        V4 = Vector3.Lerp(corner1, corner2, 1f - outerStep);
        V5 = corner2;
    }

    /// <summary>
    /// Performs the terrace interpolation between all four pairs of two edge vertices.
    /// </summary>
    public static EdgeVertices TerraceLerp(EdgeVertices begin, EdgeVertices end, int step)
    {
        EdgeVertices result;
        result.V1 = HexMetrics.TerraceLerp(begin.V1, end.V1, step);
        result.V2 = HexMetrics.TerraceLerp(begin.V2, end.V2, step);
        result.V3 = HexMetrics.TerraceLerp(begin.V3, end.V3, step);
        result.V4 = HexMetrics.TerraceLerp(begin.V4, end.V4, step);
        result.V5 = HexMetrics.TerraceLerp(begin.V5, end.V5, step);
        return result;
    }
}