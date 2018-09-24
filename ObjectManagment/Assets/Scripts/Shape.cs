using UnityEngine;

public class Shape : PersistableObject
{
    #region Properties
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
        _meshRenderer.material = material;
        MaterialId = materialId;
    }

    public void SetColor(Color32 color)
    {
        _color = color;
        _meshRenderer.material.color = color;
    }
    Color32 _color;
    #endregion

    MeshRenderer _meshRenderer;

    void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    public override void Save(GameDataWriter writer)
    {
        base.Save(writer);
        writer.Write(_color);
    }

    public override void Load(GameDataReader reader)
    {
        base.Load(reader);
        SetColor(reader.ReadColor());
    }
}
