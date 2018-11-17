using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    Mesh _hexMesh;

    // The lists that HexMesh uses are effectively temporary buffers. 
    // They are only used during triangulation. And chunks are triangulated one at a time. 
    // So we really only need one set of lists, not one set per hex mesh object therefore they can be static.
    static List<Vector3> _vertices = new List<Vector3>();
    static List<Color> _colors = new List<Color>();
    static List<int> _triangles = new List<int>();

    // we need mesh collider to make the object clickable
    MeshCollider _meshCollider;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = _hexMesh = new Mesh();
        _meshCollider = gameObject.AddComponent<MeshCollider>();
        _colors = new List<Color>();

        _hexMesh.name = "Hex Mesh";
        _vertices = new List<Vector3>();
        _triangles = new List<int>();
    }

    public void Triangulate(HexCell[] cells)
    {
        // This method could be invoked at any time, even when cells have already been triangulated earlier.
        // So we should begin by clearing the old data.
        _hexMesh.Clear();
        _vertices.Clear();
        _triangles.Clear();
        _colors.Clear();

        for (int i = 0; i < cells.Length; i++)
            Triangulate(cells[i]);

        _hexMesh.vertices = _vertices.ToArray();
        _hexMesh.triangles = _triangles.ToArray();
        _hexMesh.RecalculateNormals();
        _hexMesh.colors = _colors.ToArray();

        _meshCollider.sharedMesh = _hexMesh;
    }

    // modify the position of the point accordingly to the noise function
    Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
        //position.y += (sample.y * 2f - 1f) * HexMetrics.CellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;
        return position;
    }

    // Each corner is connected to three edges, which could be flats, slopes, or cliffs. So there are many possible configurations.
    void TriangulateCorner(Vector3 bottom, HexCell bottomCell, Vector3 left, 
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        // If both edges are slopes, then we have terraces on both the left and the right side. 
        // Also, because the bottom cell is the lowest, we know that those slopes go up. 
        // Furthermore, this means that the left and right cell have the same elevation, so the top edge connection is flat.
        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            else if (rightEdgeType == HexEdgeType.Flat)
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            else 
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);

            return;
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            else
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);

            return;
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            else
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);

            return;
        }

        AddTriangle(bottom, left, right);
        AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
    }

    void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, 
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        AddTriangle(begin, v3, v4);
        AddTriangleColor(beginCell.Color, c3, c4);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2, c3, c4);
        }
        
        AddQuad(v3, v4, left, right);
        AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
    }

    void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left,
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float boundaryInterpolator = 1f / (rightCell.Elevation - beginCell.Elevation);

        // boundary interpolators should not be negative
        if (boundaryInterpolator < 0)
            boundaryInterpolator = -boundaryInterpolator;

        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), boundaryInterpolator);
        Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, boundaryInterpolator);

        TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

        // complete the top part
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    // mirrored version of the above
    // this one covers the case when
    // 1 - 2
    //  \ /
    //   0
    void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left, 
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float boundaryInterpolator = 1f / (leftCell.Elevation - beginCell.Elevation);

        // boundary interpolators should not be negative
        if (boundaryInterpolator < 0)
            boundaryInterpolator = -boundaryInterpolator;

        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), boundaryInterpolator);
        Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, boundaryInterpolator);

        TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left, 
        HexCell leftCell, Vector3 boundary, Color boundaryColor)
    {
        Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        AddTriangleUnperturbed(Perturb(begin), v2, boundary);
        AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            AddTriangleUnperturbed(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }

        AddTriangleUnperturbed(v2, Perturb(left), boundary);
        AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }
    
    void Triangulate(HexCell cell)
    {
        Vector3 center = cell.Position;

        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            Triangulate(d, cell);
    }

    void Triangulate(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;
        EdgeVertices e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        if (cell.HasRiverThroughEdge(direction))
            e.v3.y = cell.StreamBedY;

        // hexagon itself is made of 12 triangles to add some variety
        TriangulateEdgeFan(center, e, cell.Color);

        if (direction == HexDirection.NE)
            TriangulateConnection(direction, cell, e);

        if (direction <= HexDirection.SE)
            TriangulateConnection(direction, cell, e);
    }

    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        AddTriangle(center, edge.v1, edge.v2);
        AddTriangleColor(color);
        AddTriangle(center, edge.v2, edge.v3);
        AddTriangleColor(color);
        AddTriangle(center, edge.v3, edge.v4);
        AddTriangleColor(color);
        AddTriangle(center, edge.v4, edge.v5);
        AddTriangleColor(color);
    }

    void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2)
    {
        AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        AddQuadColor(c1, c2);
        AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        AddQuadColor(c1, c2);
        AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        AddQuadColor(c1, c2);
        AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        AddQuadColor(c1, c2);
    }

    // Every two hexagons are connected by a single rectangular bridge. And every three hexagons are connected by a single triangle.
    void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices edgeBegin)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null) return;
        
        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y;
        var edgeEnd = new EdgeVertices(edgeBegin.v1 + bridge, edgeBegin.v5 + bridge);

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
            TriangulateEdgeTerraces(edgeBegin, cell, edgeEnd, neighbor);
        else
            TriangulateEdgeStrip(edgeBegin, cell.Color, edgeEnd, neighbor.Color);
        
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = edgeBegin.v5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(edgeBegin.v5, cell, edgeEnd.v5, neighbor, v5, nextNeighbor);
                }
                // If the innermost check fails, it means that the next neighbor is the lowest cell. 
                // We have to rotate the triangle counterclockwise to keep it correctly oriented.
                else
                {
                    TriangulateCorner(v5, nextNeighbor, edgeBegin.v5, cell, edgeEnd.v5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(edgeEnd.v5, neighbor, v5, nextNeighbor, edgeBegin.v5, cell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, edgeBegin.v5, cell, edgeEnd.v5, neighbor);
            }
        }
    }

    void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        TriangulateEdgeStrip(begin, beginCell.Color, e2, c2);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            TriangulateEdgeStrip(e1, c1, e2, c2);
        }

        TriangulateEdgeStrip(e2, c2, end, endCell.Color);
    }

    void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = _vertices.Count;

        // add verticles
        _vertices.Add(Perturb(v1));
        _vertices.Add(Perturb(v2));
        _vertices.Add(Perturb(v3));

        // add indexes
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
    }


    void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = _vertices.Count;

        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);

        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
    }

    void AddTriangleColor(Color color)
    {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
    }

    void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c3);
    }

    void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = _vertices.Count;

        // add verticles
        _vertices.Add(Perturb(v1));
        _vertices.Add(Perturb(v2));
        _vertices.Add(Perturb(v3));
        _vertices.Add(Perturb(v4));

        // add indexes
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 3);
    }
    
    void AddQuadColor(Color c1, Color c2)
    {
        _colors.Add(c1);
        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c2);
    }

    void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c3);
        _colors.Add(c4);
    }
}