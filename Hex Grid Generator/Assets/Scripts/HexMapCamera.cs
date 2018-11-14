using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    public float StickMinZoom, StickMaxZoom;
    public float SwivelMinZoom, SwivelMaxZoom;

    Transform _swivel, _stick;
    float _zoom = 1f;
    
    void Awake()
    {
        _swivel = transform.GetChild(0);
        _stick = _swivel.GetChild(0);
    }

    void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }
    }

    void AdjustZoom(float delta)
    {
        _zoom = Mathf.Clamp01(_zoom + delta);

        float distance = Mathf.Lerp(StickMinZoom, StickMaxZoom, _zoom);
        _stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(SwivelMinZoom, SwivelMaxZoom, _zoom);
        _swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }
}