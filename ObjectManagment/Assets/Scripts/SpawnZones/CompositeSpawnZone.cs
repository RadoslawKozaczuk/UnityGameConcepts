using UnityEngine;

public class CompositeSpawnZone : SpawnZone
{
    public SpawnZone[] _spawnZones;
    // for true spawner will iterate through spawning zones, otherwise the spawning zone is chosen randomly
    [SerializeField] bool _sequential;
    int _nextSequentialIndex;

    public override Vector3 SpawnPoint
    {
        get
        {
            int index;
            if(_sequential)
            {
                index = _nextSequentialIndex++;
                if(_nextSequentialIndex >= _spawnZones.Length)
                    _nextSequentialIndex = 0;
            }
            else
            {
                index = Random.Range(0, _spawnZones.Length);
            }
            return _spawnZones[index].SpawnPoint;
        }
    }

    public override void Save(GameDataWriter writer) => writer.Write(_nextSequentialIndex);

    public override void Load(GameDataReader reader) => _nextSequentialIndex = reader.ReadInt();
}