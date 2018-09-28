using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : PersistableObject
{
    const int SaveVersion = 1;
    const int PowerCreationAmount = 5;

    public static Game Instance { get; private set; }

    public SpawnZone SpawnZoneOfLevel { get; set; }
    public KeyCode CreateKey = KeyCode.C;
    public KeyCode NewGameKey = KeyCode.N;
    public KeyCode SaveKey = KeyCode.S;
    public KeyCode LoadKey = KeyCode.L;
    public KeyCode DestroyKey = KeyCode.D;
    public KeyCode PowerKey = KeyCode.LeftShift;
    public PersistentStorage Storage;
    public Transform ShapeParent;
    public int LevelCount;

    /* === GUI ===
        Although the screen-space canvas logically doesn't exist in 3D space, it still shows up in the scene window. 
        This allows us to edit it, but that's hard to do while the scene window is in 3D mode. 
        The GUI isn't aligned with the scene camera, and its scale is one unit per pixel, 
        so it ends up like an enormous plane somewhere in the scene. 
        When editing the GUI, you typically switch the scene window to 2D mode, 
        which you can toggle via the 2D button on the left side of its toolbar.
    */
    public Text CreationSpeedLabel;
    public Text DestructionSpeedLabel;

    // properties do not show up in the editor
    public float CreationSpeed { get; set; }
    public float DestructionSpeed { get; set; }
    
    [SerializeField] ShapeFactory _shapeFactory;
    List<Shape> _shapes;
    float _creationProgress, _destructionProgress;
    int _loadedLevelBuildIndex;

    // Awake is called after all objects are initialized.
    // Awake is always called before any Start functions. This allows you to order initialization of scripts. 
    // Awake can not act as a coroutine.
    void Awake()
    {

    }

    // It gets invoked each time a disabled component is enabled. In case of a recompilation while in play mode, 
    // all active components are disabled first, then the game state is saved, compilation happens, the game state is restored, 
    // and previously active components are enabled again.
    // Also, OnEnable gets invoked immediately after a component's Awake method, unless the component was saved in a disabled state.
    void OnEnable()
    {
        Instance = this;
    }

    // Start is called on the frame when a script is enabled just before any of the Update methods is called the first time.
    // Like the Awake function, Start is called exactly once in the lifetime of the script.
    // However, Awake is called when the script object is initialized, regardless of whether or not the script is enabled.
    // Start may not be called on the same frame as Awake if the script is not enabled at initialization time.
    void Start()
    {
        _shapes = new List<Shape>();

        // When playing in the Editor we may accidentally load the scene twice if the scene was open in the Editor already
        // before we clicked the play button.
        // To prevent it from happening we have to check if the scene is loaded already at this point.
        if(Application.isEditor)
        {
            // we want only once scene to be active
            for(int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if(loadedScene.name.Contains("Level "))
                {
                    SceneManager.SetActiveScene(loadedScene);
                    return;
                }
            }
        }

        StartCoroutine(LoadLevel(1));
    }

    // Update is called once per frame
    void Update()
    {
        bool skipUpdate;
        HandlePlayerInput(out skipUpdate);

        if(skipUpdate)
        {
            return;
        }

        CreationSpeedLabel.text = $"Creation Speed = {(int)CreationSpeed} shapes/s";
        DestructionSpeedLabel.text = $"Destruction Speed = {(int)DestructionSpeed} shapes/s";

        // we create the (int)CreationSpeed number of shapes per second
        _creationProgress += Time.deltaTime * CreationSpeed;

        // It might be possible that so much progress was made since the last frame that we end up with a value that's 2 or more. 
        // This could happen during a frame rate dip, in combination with a high creation speed.
        // Usage of while loop allows us to catch up as quickly as possible.
        while(_creationProgress >= 1f)
        {
            _creationProgress -= 1f;
            CreateShape();
        }

        _destructionProgress += Time.deltaTime * DestructionSpeed;
        while(_destructionProgress >= 1f)
        {
            _destructionProgress -= 1f;
            DestroyShape();
        }
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(SaveVersion);
        writer.Write(_shapes.Count);
        writer.Write(_loadedLevelBuildIndex);
        for(int i = 0; i < _shapes.Count; i++)
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
        StartCoroutine(LoadLevel(reader.ReadInt()));
        for(int i = 0; i < count; i++)
        {
            int shapeId = reader.ReadInt();
            int materialId = reader.ReadInt();
            Shape shape = _shapeFactory.Get(shapeId, materialId);
            shape.Load(reader); // load the rest of shape's data
            _shapes.Add(shape);
        }
    }

    void HandlePlayerInput(out bool skipUpdate)
    {
        skipUpdate = false;

        if(Input.GetKey(PowerKey) && Input.GetKeyDown(CreateKey))
        {
            for(int i = 0; i < PowerCreationAmount; i++)
                CreateShape();
        }
        else if(Input.GetKeyDown(CreateKey))
        {
            CreateShape();
        }
        else if(Input.GetKeyDown(DestroyKey))
        {
            DestroyShape();
        }
        else if(Input.GetKeyDown(NewGameKey))
        {
            BeginNewGame();
        }
        else if(Input.GetKeyDown(SaveKey))
        {
            Storage.Save(this);
        }
        else if(Input.GetKeyDown(LoadKey))
        {
            BeginNewGame();
            Storage.Load(this);
        }
        else
        {
            for(int i = 1; i <= LevelCount; i++)
            {
                if(Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    BeginNewGame();
                    StartCoroutine(LoadLevel(i));
                    skipUpdate = true;
                    return;
                }
            }
        }
    }

    void CreateShape()
    {
        Shape shape = _shapeFactory.GetRandom();
        Transform t = shape.transform;
        t.localPosition = SpawnZoneOfLevel.SpawnPoint;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * Random.Range(0.3f, 1f);

        // Having a lot of dynamically changing objects under one parent object can negatively impact our game's performance 
        // when shapes are changed. 
        // Whenever an object's active or transform state changes, all its parent objects are notified of this change.
        // Better solution is to use separate scenes - we will use this approach in the ShapeFactory.
        //shape.transform.SetParent(ShapeParent);

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
        if(_shapes.Count > 0)
        {
            int index = Random.Range(0, _shapes.Count);

            // Although we have destroyed the shape, we haven't removed it from the shapes list. 
            // Thus, the list still contains references to the components of the destroyed game objects.
            // They still exist in memory, in a zombie-like state.
            // When trying to destroy such an object a second time, Unity reports an error.
            _shapeFactory.Destroy(_shapes[index]);

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

    void BeginNewGame()
    {
        for(int i = 0; i < _shapes.Count; i++)
            _shapeFactory.Destroy(_shapes[i]);

        // This leaves us with a list of references to destroyed objects, we must get rid of these as well
        _shapes.Clear();
    }

    // level build index are set in File -> Build Settings
    IEnumerator LoadLevel(int levelBuildIndex)
    {
        enabled = false;
        if(_loadedLevelBuildIndex > 0)
            yield return SceneManager.UnloadSceneAsync(_loadedLevelBuildIndex);

        // Scene loaded with the LoadSceneMode.Additive as an additional argument will be added to already loaded scenes.
        yield return SceneManager.LoadSceneAsync(levelBuildIndex, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(levelBuildIndex));

        _loadedLevelBuildIndex = levelBuildIndex;
        enabled = true;
    }

}
