using UnityEngine;
using System.Collections.Generic;

public class TransformationGrid : MonoBehaviour
{
    public Transform prefab;
    public int gridResolution = 10;
    
    Transform[] grid;
    List<Transformation> transformations;
    
    // advanced version
    Matrix4x4 transformation;
    public bool useAdvancedCalculation = true;

    void Awake()
    {
        grid = new Transform[gridResolution * gridResolution * gridResolution];
        transformations = new List<Transformation>();

        for (int i = 0, z = 0; z < gridResolution; z++)
            for (int y = 0; y < gridResolution; y++)
                for (int x = 0; x < gridResolution; x++, i++)
                    grid[i] = CreateGridPoint(x, y, z);
    }

    // Why get the components each update?
    // This allows use to mess around with the transformation components while remaining in play mode, immediately seeing the results.
    void Update()
    {
        if(useAdvancedCalculation)
            UpdateTransformation();

        // it is a good habit to use the list variant whenever you're grabbing components often.
        GetComponents(transformations);
        for (int i = 0, z = 0; z < gridResolution; z++)
            for (int y = 0; y < gridResolution; y++)
                for (int x = 0; x < gridResolution; x++, i++)
                    grid[i].localPosition = TransformPoint(x, y, z);
    }

    // multiply matrixes
    void UpdateTransformation()
    {
        GetComponents(transformations);
        if (transformations.Count > 0)
        {
            transformation = transformations[0].Matrix;
            for (int i = 1; i < transformations.Count; i++)
                transformation = transformations[i].Matrix * transformation;
        }
    }

    Transform CreateGridPoint(int x, int y, int z)
    {
        Transform point = Instantiate(prefab);
        point.localPosition = GetCoordinates(x, y, z);

        // kolorujemy od brak koloru do maks kolor w danym kanale
        point.GetComponent<MeshRenderer>().material.color = new Color(
            (float)x / gridResolution,
            (float)y / gridResolution,
            (float)z / gridResolution
        );
        return point;
    }

    // The most obvious shape of our grid is a cube, so let's go with that. 
    // We center it at the origin, so transformations – specifically rotation and scaling – are relative to the midpoint of the grid cube.
    Vector3 GetCoordinates(int x, int y, int z)
    {
        return new Vector3(
            x - (gridResolution - 1) * 0.5f,
            y - (gridResolution - 1) * 0.5f,
            z - (gridResolution - 1) * 0.5f
        );
    }

    // Transforming each point is done by getting the original coordinates, and then applying each transformation. 
    // We cannot rely on the actual position of each point, because those have already been transformed and we don't want to accumulate transformations each frame.
    Vector3 TransformPoint(int x, int y, int z)
    {
        Vector3 coordinates = GetCoordinates(x, y, z);
        if (useAdvancedCalculation) return transformation.MultiplyPoint(coordinates);

        for (int i = 0; i < transformations.Count; i++)
            coordinates = transformations[i].Apply(coordinates);

        return coordinates;
    }
}