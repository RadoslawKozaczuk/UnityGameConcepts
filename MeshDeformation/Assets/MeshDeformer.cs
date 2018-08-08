using UnityEngine;

// We only require a mesh filter. We don't care how it gets a mesh. 
// Right now we're using our procedural cube sphere, but it could be any mesh.
[RequireComponent(typeof(MeshFilter))]
public class MeshDeformer : MonoBehaviour {

	public float springForce = 20f;
	public float damping = 5f;

    // To perform any deformation, we need access to mesh.
    Mesh deformingMesh;

    // We can extract the original vertex positions. We also have to keep track of the displaced vertices during deformation.
    Vector3[] originalVertices, displacedVertices;
	Vector3[] vertexVelocities;

	float uniformScale = 1f;

	void Start () {
		deformingMesh = GetComponent<MeshFilter>().mesh;
		originalVertices = deformingMesh.vertices;
		displacedVertices = new Vector3[originalVertices.Length];

        // copy the original vertices to the displaced vertices
        for (int i = 0; i < originalVertices.Length; i++)
			displacedVertices[i] = originalVertices[i];

        // Vertices move as the mesh is deformed. So we also have to store the velocity of each vertex.
        vertexVelocities = new Vector3[originalVertices.Length];
	}

	void Update () {
		uniformScale = transform.localScale.x;
		for (int i = 0; i < displacedVertices.Length; i++) {
			UpdateVertex(i);
		}
		deformingMesh.vertices = displacedVertices;
		deformingMesh.RecalculateNormals();
	}

	void UpdateVertex (int i) {
		Vector3 velocity = vertexVelocities[i];
		Vector3 displacement = displacedVertices[i] - originalVertices[i];
		displacement *= uniformScale;
		velocity -= displacement * springForce * Time.deltaTime;
		velocity *= 1f - damping * Time.deltaTime;
		vertexVelocities[i] = velocity;
		displacedVertices[i] += velocity * (Time.deltaTime / uniformScale);
	}

	public void AddDeformingForce (Vector3 point, float force) {
		point = transform.InverseTransformPoint(point);
		for (int i = 0; i < displacedVertices.Length; i++) {
			AddForceToVertex(i, point, force);
		}
	}

	void AddForceToVertex (int i, Vector3 point, float force) {
		Vector3 pointToVertex = displacedVertices[i] - point;
		pointToVertex *= uniformScale;
		float attenuatedForce = force / (1f + pointToVertex.sqrMagnitude);
		float velocity = attenuatedForce * Time.deltaTime;
		vertexVelocities[i] += pointToVertex.normalized * velocity;
	}
}