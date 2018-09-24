using UnityEngine;

public class Shape : PersistableObject
{
    // Instead of using a string to name the color property, it is also possible to use an identifier. 
    // These identifiers are setup by Unity. They can change, but remain constant per session. 
    // So we can suffice with getting the identifier of the color property once, storing it in a static field. 
    // The identifier is found by invoking the Shader.PropertyToID method with a name.
    static readonly int colorPropertyId = Shader.PropertyToID("_Color");
    static MaterialPropertyBlock sharedPropertyBlock;

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

        // A downside of setting a material's color is that this results in the creation of a new material, unique to the shape. 
        // This happens each time its color is set. We can avoid this by using a MaterialPropertyBlock instead. 
        // Create a new property block, set a color property named _Color, then use it as the renderer's property block, 
        // by invoking MeshRenderer.SetPropertyBlock.
        if (sharedPropertyBlock == null)
            sharedPropertyBlock = new MaterialPropertyBlock();

        sharedPropertyBlock.SetColor(colorPropertyId, _color);
        _meshRenderer.SetPropertyBlock(sharedPropertyBlock);
        _meshRenderer.material.color = _color;
    }
    Color32 _color;
    #endregion
    
    MeshRenderer _meshRenderer;

    void Awake() =>_meshRenderer = GetComponent<MeshRenderer>();

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
