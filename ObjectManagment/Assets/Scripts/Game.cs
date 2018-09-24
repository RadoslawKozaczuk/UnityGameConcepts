using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game : PersistableObject
{
    const int SaveVersion = 1;
    const int PowerCreationAmount = 5;

    public ShapeFactory shapeFactory;
    public KeyCode createKey = KeyCode.C;
    public KeyCode newGameKey = KeyCode.N;
    public KeyCode saveKey = KeyCode.S;
    public KeyCode loadKey = KeyCode.L;
    public KeyCode destroyKey = KeyCode.X;
    public KeyCode powerKey = KeyCode.LeftShift;
    public PersistentStorage storage;
    public Transform ShapeParent;

    public Text CreationSpeedLabel;

    // properties do not show up in the editor
    public float CreationSpeed { get; set; }

    List<Shape> _shapes;
    float _creationProgress;

    void Awake() =>_shapes = new List<Shape>();

    // Update is called once per frame
    void Update ()
    {
        HandlePlayerInput();

        CreationSpeedLabel.text = $"Creation Speed = {(int)CreationSpeed} shapes/s";

        // we create the (int)CreationSpeed number of shapes per second
        _creationProgress += Time.deltaTime * CreationSpeed;

        // It might be possible that so much progress was made since the last frame that we end up with a value that's 2 or more. 
        // This could happen during a frame rate dip, in combination with a high creation speed.
        // Usage of while loop allows us to catch up as quickly as possible.
        while (_creationProgress >= 1f)
        {
            _creationProgress -= 1f;
            CreateShape();
        }
    }

    void HandlePlayerInput()
    {
        if (Input.GetKey(powerKey) && Input.GetKeyDown(createKey))
        {
            for (int i = 0; i < PowerCreationAmount; i++)
                CreateShape();
        }
        else if (Input.GetKeyDown(createKey))
        {
            CreateShape();
        }
        else if (Input.GetKeyDown(destroyKey))
        {
            DestroyShape();
        }
        else if (Input.GetKeyDown(newGameKey))
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

    void CreateShape()
    {
        Shape shape = shapeFactory.GetRandom();
        Transform t = shape.transform;
        t.localPosition = Random.insideUnitSphere * 5f;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * Random.Range(0.3f, 1f);
        t.SetParent(ShapeParent);

        shape.SetColor(Random.ColorHSV(
            hueMin: 0f, hueMax: 1f,
            saturationMin: 0.5f, saturationMax: 1f,
            valueMin: 0.25f, valueMax: 1f,
            alphaMin: 1f, alphaMax: 1f
        ));
        
        _shapes.Add(shape);
    }

    // destroy random object
    void DestroyShape()
    {
        if (_shapes.Count > 0)
        {
            int index = Random.Range(0, _shapes.Count);
            // === Zombie components === 
            // Although we have destroyed the shape, we haven't removed it from the shapes list. 
            // Thus, the list still contains references to the components of the destroyed game objects. 
            // They still exist in memory, in a zombie-like state. 
            // When trying to destroy such an object a second time, Unity reports an error.
            Destroy(_shapes[index].gameObject);

            // === List Optimization ===
            // The List class is implemented with arrays and the gap is eliminated 
            // by shifting the next element into this gap until the gap reach the end.
            // While we cannot technically avoid it, we can skip nearly all the work by manually grabbing the last element 
            // and putting that in the place of the destroyed element, effectively teleporting the gap to the end of the list.
            int lastIndex = _shapes.Count - 1;
            _shapes[index] = _shapes[lastIndex];

            // The solution is to properly get rid of the references to the shape that we just destroyed. 
            // So after destroying a shape, remove it from the list.
            // The zombie component will be then removed by the Garbage Collector.
            _shapes.RemoveAt(lastIndex);
        }
    }

    void BeginNewGame() {
        for (int i = 0; i < _shapes.Count; i++)
            // Destroy works on either a game object, a component, or an asset. 
            // To get rid of the entire shape object and not just its Shape component, 
            // we have to explicitly destroy the game object that the component is a part of. 
            // We can access it via the component's gameObject property.
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
            shape.transform.SetParent(ShapeParent);
            _shapes.Add(shape);
        }
    }
}
