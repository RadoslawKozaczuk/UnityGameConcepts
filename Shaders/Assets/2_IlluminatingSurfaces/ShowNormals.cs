using UnityEngine;

/* === ExecuteInEditMode ===
    Makes all instances of a script execute in edit mode.

    By default, MonoBehaviours are only executed in play mode.By adding this attribute, 
    any instance of the MonoBehaviour will have its callback functions executed while the Editor is not in playmode.

    The functions are not called constantly like they are in play mode.
    - Update is only called when something in the scene changed.
    - OnGUI is called when the Game View recieves an Event.
    - OnRenderObject and the other rendering callback functions are called on every repaint of the Scene View or Game View. 
*/
[ExecuteInEditMode]
public class ShowNormals : MonoBehaviour
{
    [Range(-10, 10)]
    public float nx;

    [Range(-10, 10)]
    public float ny;

    [Range(-10, 10)]
    public float nz;

    //to display length
    public float normal_length;

    // Update is called once per frame
    void Update()
    {
        // Instantiating mesh due to calling MeshFilter.mesh during edit mode. This will leak meshes. Please use MeshFilter.sharedMesh instead.
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        Vector3 modNormal = new Vector3(normals[0].x * nx, normals[0].y * ny, normals[0].z * nz);
        normal_length = modNormal.magnitude;

        for (var i = 0; i < normals.Length; i++)
        {
            Vector3 pos = vertices[i];
            pos.x *= transform.localScale.x;
            pos.y *= transform.localScale.y;
            pos.z *= transform.localScale.z;
            pos += transform.position;

            Vector3 posRot = transform.rotation * pos;
            normals[i].x *= nx;
            normals[i].y *= ny;
            normals[i].z *= nz;

            Debug.DrawLine(posRot, posRot + normals[i], Color.red);
        }
    }
}

//[ExecuteInEditMode]
//public class ShowNormals
//{
//    public Mesh Mesh;

//    [SerializeField]
//    Vector3[] m_normals;

//    void OnEnable()
//    {
//        Mesh = GetComponent<MeshFilter>().mesh;
//        m_normals = Mesh.normals;
//    }

//    public void ApplyNewNormals()
//    {
//        Vector3[] fixedNormals = new Vector3[m_normals.Length];
//        for (int i = 0; i < m_normals.Length; i++)
//        {
//            fixedNormals[i] = m_normals[i];
//            fixedNormals[i].Normalize();
//        }
//        Mesh.normals = fixedNormals;
//    }
//}