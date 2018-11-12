using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    Mesh _hexMesh;
    List<Vector3> _vertices;
    List<int> _triangles;

    // we need mesh collider to make the object clickable
    MeshCollider _meshCollider;
    List<Color> _colors;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = _hexMesh = new Mesh();
        _meshCollider = gameObject.AddComponent<MeshCollider>();
        _colors = new List<Color>();

        _hexMesh.name = "Hex Mesh";
        _vertices = new List<Vector3>();
        _triangles = new List<int>();
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
            {
                TriangulateCornerTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
                return;
            }
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
    
    void Triangulate(HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;

        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            Triangulate(d, cell);
    }

    void Triangulate(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

        AddTriangle(center, v1, v2);
        AddTriangleColor(cell.Color);

        if (direction == HexDirection.NE)
        {
            TriangulateConnection(direction, cell, v1, v2);
        }
        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, v1, v2);
        }
    }

    // Every two hexagons are connected by a single rectangular bridge. And every three hexagons are connected by a single triangle.
    void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null) return;
        
        Vector3 bridge = HexMetrics.GetBridge(direction);
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;
        v3.y = v4.y = neighbor.Elevation * HexMetrics.ElevationStep;

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(v1, v2, cell, v3, v4, neighbor);
        }
        else
        {
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(cell.Color, neighbor.Color);
        }
        
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Elevation * HexMetrics.ElevationStep;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(v2, cell, v4, neighbor, v5, nextNeighbor);
                }
                // If the innermost check fails, it means that the next neighbor is the lowest cell. 
                // We have to rotate the triangle counterclockwise to keep it correctly oriented.
                else
                {
                    TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(v4, neighbor, v5, nextNeighbor, v2, cell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
            }
        }
    }

    void TriangulateEdgeTerraces(Vector3 beginLeft, Vector3 beginRight, HexCell beginCell, 
        Vector3 endLeft, Vector3 endRight, HexCell endCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        // first step
        AddQuad(beginLeft, beginRight, v3, v4);
        AddQuadColor(beginCell.Color, c2);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c2;
            v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
            v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2);
        }
        
        // last step
        AddQuad(v3, v4, endLeft, endRight);
        AddQuadColor(c2, endCell.Color);
    }

    void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = _vertices.Count;

        // add verticles
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);

        // add indexes
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
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        _vertices.Add(v4);
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