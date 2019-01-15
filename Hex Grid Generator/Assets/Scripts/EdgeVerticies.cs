using System.Text;
using UnityEngine;

/// <summary>
/// Stores coordinates of the 5 edge verticies coordinates ordered from the left to the right (clockwise) along the cell's edge.
/// </summary>
public struct EdgeVertices
{
    // vertices are ordered clockwise along the cell's edge
    public Vector3 V1, V2, V3, V4, V5; // v3 is used by rivers
    
    /// <summary>
    /// Points V2, V3 and V4 are evenly distributed along the edge.
    /// </summary>
    public EdgeVertices(Vector3 left, Vector3 right)
    {
        V1 = left;
        V2 = Vector3.Lerp(left, right, 0.25f);
        V3 = Vector3.Lerp(left, right, 0.5f);
        V4 = Vector3.Lerp(left, right, 0.75f);
        V5 = right;
    }

    /// <summary>
    /// Distance parameter determines the distance between the V1 and V2, and V4 and V5 points.
    /// Point V3 is always in the middle.
    /// </summary>
    public EdgeVertices(Vector3 left, Vector3 right, float distance)
    {
        V1 = left;
        V2 = Vector3.Lerp(left, right, distance);
        V3 = Vector3.Lerp(left, right, 0.5f);
        V4 = Vector3.Lerp(left, right, 1f - distance);
        V5 = right;
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

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("(");
        sb.Append(V1);
        sb.Append(", ");
        sb.Append(V2);
        sb.Append(", ");
        sb.Append(V3);
        sb.Append(", ");
        sb.Append(V4);
        sb.Append(", ");
        sb.Append(V5);
        sb.Append(")");
        return sb.ToString();
    }
}