using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class Orbit : MonoBehaviour {

    public GameObject Sun;
    GameObject[] planets;
    public int numPlanets = 50;

    Transform[] planetTransforms;
    TransformAccessArray planetTranformAccessArray;
    PositionUpdateJob planetJob;
    JobHandle planetPositionJobHandle;

    struct PositionUpdateJob : IJobParallelForTransform
    {
        public Vector3 sunPos;

        public void Execute(int index, TransformAccess transform)
        {
            Vector3 direction = sunPos - transform.position;
            float gravity = Mathf.Clamp(direction.magnitude / 1000.0f, 0, 1);
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, gravity);

            var vectorForward = new Vector3(0, 0, 0.04f);

            float orbitalSpeed = Mathf.Sqrt(50 / direction.magnitude);
            transform.position += transform.rotation * vectorForward * orbitalSpeed;
        }
    }

    // Use this for initialization
    void Start() {
        planets = new GameObject[numPlanets];
        planetTransforms = new Transform[numPlanets];
        planetTranformAccessArray = new TransformAccessArray(planetTransforms);

        for (int i = 0; i < numPlanets; i++)
        {
            planets[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planets[i].transform.position = Sun.transform.position + Random.insideUnitSphere * 50;
            planets[i].transform.SetParent(transform);
            planetTranformAccessArray[i] = planets[i].transform;
        }
    }

    // Update is called once per frame
    void Update() {
        planetJob = new PositionUpdateJob()
        {
            sunPos = Sun.transform.position
        };

        planetPositionJobHandle = planetJob.Schedule(planetTranformAccessArray);
    }

    public void LateUpdate()
    {
        // wait until the job we have scheduled has completed
        planetPositionJobHandle.Complete();
    }

    private void OnDestroy()
    {
        planetTranformAccessArray.Dispose();
    }
}
