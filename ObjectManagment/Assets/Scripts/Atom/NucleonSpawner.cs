using UnityEngine;

public class NucleonSpawner : MonoBehaviour
{

    public float timeBetweenSpawns;

    public float spawnDistance;

    public Nucleon[] nucleonPrefabs;

    float timeSinceLastSpawn;

    // Using FixedUpdate keeps the spawning independent of the frame rate.
    // If the configured time between spawns is shorter than the frame time, using Update would cause spawn delays.
    // And as the point of this scene is to tank our frame rate, that will happen.
   void FixedUpdate()
    {
        timeSinceLastSpawn += Time.deltaTime;
        if(timeSinceLastSpawn >= timeBetweenSpawns)
        {
            timeSinceLastSpawn -= timeBetweenSpawns;
            SpawnNucleon();
        }
    }

    void SpawnNucleon()
    {
        Nucleon prefab = nucleonPrefabs[Random.Range(0, nucleonPrefabs.Length)];
        Nucleon spawn = Instantiate<Nucleon>(prefab);
        spawn.transform.localPosition = Random.onUnitSphere * spawnDistance;
    }
}