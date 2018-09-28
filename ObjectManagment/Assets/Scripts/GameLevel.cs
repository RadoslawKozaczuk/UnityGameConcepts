using UnityEngine;

public class GameLevel : MonoBehaviour
{
    [SerializeField] SpawnZone _spawnZone;

    void Start()
    {
        Game.Instance.SpawnZoneOfLevel = _spawnZone;
    }
}