using UnityEngine;

// To add such an asset to our project, we'll have to add an entry for it to Unity's menu.
// The simplest way to do this is by adding the CreateAssetMenu attribute to our class.
// Thanks to that we can now create our factory via Assets › Create › Shape Factory.
[CreateAssetMenu]
// Factory doesn't need a position, rotation, or scale, or an Update method.
// So it doesn't need to be a component, which would have to be attached to a game object.
// Instead, it can exist on it own, not part of a specific scene, but part of the project.
// In other words, it is an asset. This is possible, by having it extend ScriptableObject instead of MonoBehaviour.
public class ShapeFactory : ScriptableObject
{
    [SerializeField]
    Shape[] _prefabs;

    [SerializeField]
    Material[] _materials;

    public Shape Get(int shapeId = 0, int materialId = 0)
    {
        Shape instance = Instantiate(_prefabs[shapeId]);
        instance.ShapeId = shapeId;
        instance.SetMaterial(_materials[materialId], materialId);
        return instance;
    }

    public Shape GetRandom() => Get(Random.Range(0, _prefabs.Length), Random.Range(0, _materials.Length));
}
