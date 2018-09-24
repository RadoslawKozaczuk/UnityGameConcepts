using System.Collections.Generic;
using UnityEngine;

public class Game : PersistableObject
{
    const int SaveVersion = 1;

    public ShapeFactory shapeFactory;
    public KeyCode createKey = KeyCode.C;
    public KeyCode newGameKey = KeyCode.N;
    public KeyCode saveKey = KeyCode.S;
    public KeyCode loadKey = KeyCode.L;
    public PersistentStorage storage;
    
    List<Shape> _shapes;
    
    void Awake()
    {
        _shapes = new List<Shape>();
    }

    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown(createKey))
        {
            CreateObject();
        }
        else if (Input.GetKey(newGameKey))
        {
            BeginNewGame();
        }
        else if (Input.GetKeyDown(saveKey))
        {
            storage.Save(this);
        }
        else if (Input.GetKeyDown(loadKey))
        {
            BeginNewGame();
            storage.Load(this);
        }
    }

    void CreateObject()
    {
        Shape shape = shapeFactory.GetRandom();
        Transform t = shape.transform;
        t.localPosition = Random.insideUnitSphere * 5f;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * Random.Range(0.3f, 1f);
        shape.SetColor(Random.ColorHSV(
            hueMin: 0f, hueMax: 1f,
            saturationMin: 0.5f, saturationMax: 1f,
            valueMin: 0.25f, valueMax: 1f,
            alphaMin: 1f, alphaMax: 1f
        ));
        _shapes.Add(shape);
    }

    void BeginNewGame() {
        for (int i = 0; i < _shapes.Count; i++)
            Destroy(_shapes[i].gameObject);
        // This leaves us with a list of references to destroyed objects, we must get rid of these as well
        _shapes.Clear();
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(SaveVersion);
        writer.Write(_shapes.Count);
        for (int i = 0; i < _shapes.Count; i++)
        {
            writer.Write(_shapes[i].ShapeId);
            writer.Write(_shapes[i].MaterialId);
            _shapes[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader)
    {
        int saveVersion = reader.ReadInt();
        if(saveVersion != SaveVersion)
        {
            Debug.LogError($"Save version {saveVersion} is unsupported");
            return;
        }

        int count = reader.ReadInt();
        for (int i = 0; i < count; i++)
        {
            int shapeId = reader.ReadInt();
            int materialId = reader.ReadInt();
            Shape shape = shapeFactory.Get(shapeId, materialId);
            shape.Load(reader); // load the rest of shape's data
            _shapes.Add(shape);
        }
    }
}
