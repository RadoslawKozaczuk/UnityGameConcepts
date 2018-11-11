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
        AddTriangle(
            center,
            center + HexMetrics.Corners[(int)direction], // first corner, relative to its center
            center + HexMetrics.Corners[(int)direction + 1] // first corner, relative to its center
        );

        // get three neighbours and blend their colors together
        HexCell prevNeighbor = cell.GetNeighbor(direction.Previous()) ?? cell;
        HexCell neighbor = cell.GetNeighbor(direction) ?? cell;
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next()) ?? cell;
        
        AddTriangleColor(
            cell.Color,
            (cell.Color + prevNeighbor.Color + neighbor.Color) / 3f,
            (cell.Color + neighbor.Color + nextNeighbor.Color) / 3f
        );
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
}