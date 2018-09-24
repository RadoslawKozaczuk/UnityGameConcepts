using UnityEngine;

// Having multiple such components makes no sense. 
// We can enforce this by adding the DisallowMultipleComponent attribute to the class.
[DisallowMultipleComponent]
public class PersistableObject : MonoBehaviour {

    public virtual void Save(GameDataWriter writer)
    {
        writer.Write(transform.localPosition);
        writer.Write(transform.localRotation);
        writer.Write(transform.localScale);
    }

    public virtual void Load(GameDataReader reader)
    {
        transform.localPosition = reader.ReadVector3();
        transform.localRotation = reader.ReadQuaternion();
        transform.localScale = reader.ReadVector3();
    }
}
