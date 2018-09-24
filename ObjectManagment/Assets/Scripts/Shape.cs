using UnityEngine;

public class Shape : PersistableObject
{
    public int ShapeId
    {
        get { return _shapeId; }
        set
        {
            // default value cannot be reassinged
            if (_shapeId == int.MinValue && value != int.MinValue)
                _shapeId = value;
            else
                Debug.LogError("Not allowed to change shapeId.");
        }
    }
    int _shapeId = int.MinValue; // we have to assign different default value becasue zero is a meaningful value

    public int MaterialId { get; private set; }

    public void SetMaterial(Material material, int materialId)
    {
        GetComponent<MeshRenderer>().material = material;
        MaterialId = materialId;
    }
}
