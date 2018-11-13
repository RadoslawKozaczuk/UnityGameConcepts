using UnityEngine;

public struct EdgeVertices
{
    // vertices are ordered clockwise along the cell's edge
    public Vector3 v1, v2, v3, v4;

    public EdgeVertices(Vector3 startPoint, Vector3 endPoint)
    {
        v1 = startPoint;
        v2 = Vector3.Lerp(startPoint, endPoint, 1f / 3f);
        v3 = Vector3.Lerp(startPoint, endPoint, 2f / 3f);
        v4 = endPoint;
    }
    
    /// <summary>
    /// Performs the terrace interpolation between all four pairs of two edge vertices.
    /// </summary>
    public static EdgeVertices TerraceLerp(EdgeVertices begin, EdgeVertices end, int step)
    {
        EdgeVertices result;
        result.v1 = HexMetrics.TerraceLerp(begin.v1, end.v1, step);
        result.v2 = HexMetrics.TerraceLerp(begin.v2, end.v2, step);
        result.v3 = HexMetrics.TerraceLerp(begin.v3, end.v3, step);
        result.v4 = HexMetrics.TerraceLerp(begin.v4, end.v4, step);
        return result;
    }
}