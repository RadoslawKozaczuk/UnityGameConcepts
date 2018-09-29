using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Nucleon : MonoBehaviour
{
    public float AttractionForce;
    Rigidbody _body;

    void Awake() =>_body = GetComponent<Rigidbody>();

    void FixedUpdate() => _body.AddForce(transform.localPosition * -AttractionForce);
}