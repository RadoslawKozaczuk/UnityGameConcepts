using UnityEngine;

public class GameLevel : PersistableObject
{
    public static GameLevel Current { get; private set; }

    [SerializeField] SpawnZone _spawnZone;
    [SerializeField] PersistableObject[] _persistentObjects;

    void OnEnable()
    {
        Current = this;
        if(_persistentObjects == null)
            _persistentObjects = new SpawnZone[0];
    }

    public Vector3 SpawnPoint
    {
        get
        {
            return _spawnZone.SpawnPoint;
        }
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(_persistentObjects.Length);
        for(int i = 0; i < _persistentObjects.Length; i++)
            _persistentObjects[i].Save(writer);
    }

    public override void Load(GameDataReader reader)
    {
        int savedCount = reader.ReadInt();
        for(int i = 0; i < savedCount; i++)
            _persistentObjects[i].Load(reader);
    }
}