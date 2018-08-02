using UnityEngine;

public abstract class Transformation : MonoBehaviour {

	public abstract Vector3 Apply (Vector3 point);

    // Homogeneous matrix version
    public abstract Matrix4x4 Matrix { get; }
    public Vector3 ApplyV2(Vector3 point)
    {
        return Matrix.MultiplyPoint(point);
    }
}