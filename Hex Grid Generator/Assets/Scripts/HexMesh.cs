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

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = _hexMesh = new Mesh();
        _meshCollider = gameObject.AddComponent<MeshCollider>();

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

        for (int i = 0; i < cells.Length; i++)
            Triangulate(cells[i]);

        _hexMesh.vertices = _vertices.ToArray();
        _hexMesh.triangles = _triangles.ToArray();
        _hexMesh.RecalculateNormals();

        _meshCollider.sharedMesh = _hexMesh;
    }
    
    void Triangulate(HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;
        for (int i = 0; i < 6; i++)
            AddTriangle(
                center,
                center + HexMetrics.corners[i], // The other two vertices are the first and second corners, relative to its center.
                center + HexMetrics.corners[i + 1]
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
}