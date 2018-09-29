using UnityEngine;

public class NucleonSpawner : MonoBehaviour
{
    public float TimeBetweenSpawns;
    public float SpawnDistance;
    public Nucleon[] NucleonPrefabs;

    float _timeSinceLastSpawn;

    // Using FixedUpdate keeps the spawning independent of the frame rate.
    // If the configured time between spawns is shorter than the frame time, using Update would cause spawn delays.
    // And as the point of this scene is to tank our frame rate, that will happen.
    void FixedUpdate()
    {
        _timeSinceLastSpawn += Time.deltaTime;
        if (_timeSinceLastSpawn >= TimeBetweenSpawns)
        {
            _timeSinceLastSpawn -= TimeBetweenSpawns;
            SpawnNucleon();
        }
    }

    void SpawnNucleon()
    {
        Nucleon prefab = NucleonPrefabs[Random.Range(0, NucleonPrefabs.Length)];
        Nucleon spawn = Instantiate(prefab);
        spawn.transform.localPosition = Random.onUnitSphere * SpawnDistance;
    }
}