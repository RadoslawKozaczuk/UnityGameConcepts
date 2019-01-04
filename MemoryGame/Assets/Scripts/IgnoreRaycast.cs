using UnityEngine;

public class IgnoreRaycast : MonoBehaviour, ICanvasRaycastFilter
{
    // it tells the Unity when the object should pass through the raycast
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        return false;
    }
}
