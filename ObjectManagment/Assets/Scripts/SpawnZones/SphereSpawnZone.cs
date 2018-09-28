using UnityEngine;

public class SphereSpawnZone : SpawnZone
{
    [SerializeField] bool _surfaceOnly;

    public override Vector3 SpawnPoint =>
        transform.TransformPoint(_surfaceOnly ? Random.onUnitSphere : Random.insideUnitSphere);

    // This is a special Unity method that gets invoked each time the scene window is drawn.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(Vector3.zero, 1f);
    }
}