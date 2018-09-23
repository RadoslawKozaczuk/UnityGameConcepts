using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;

public class PlanetSystem : JobComponentSystem
{
    struct Steer : IJobParallelFor
    {
        // its recommended for all the incoming arrays to have the same length
        public ComponentDataArray<Heading> headings;
        public ComponentDataArray<Position> planetPositions;
        public ComponentDataArray<MoveSpeed> speeds;
        public Vector3 sunPosition;

        public void Execute(int index)
        {
            float3 newHeading = headings[index].Value;
            float orbitalSpeed = 10;

            float3 sunPos = new float3(sunPosition.x, sunPosition.y, sunPosition.z);
            float3 difference = math.normalize(sunPos - planetPositions[index].Value);
            float distance = math.lengthSquared(difference) + 0.1f; // + 0.1f to be sure it is not zero
            float gravity = math.clamp(distance / 100.0f, 0, 1);
            newHeading += math.lerp(headings[index].Value, difference, gravity);

            orbitalSpeed = math.sqrt(500 / distance);
            headings[index] = new Heading { Value = math.normalize(newHeading) };
            speeds[index] = new MoveSpeed { speed = orbitalSpeed };
        }
    }

    // properties
    ComponentGroup _allPlanets;
    static GameObject _sun;
    
    protected override void OnCreateManager(int capacity)
    {
        // get all that contains all these three components
        // in case of other object exists that have same components just add another custom one to distinguish
        _allPlanets = GetComponentGroup(typeof(Position), typeof(Heading), typeof(MoveSpeed));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitWithScene()
    {
        _sun = GameObject.Find("Sun");
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var positions = _allPlanets.GetComponentDataArray<Position>();
        var headings = _allPlanets.GetComponentDataArray<Heading>();
        var speeds = _allPlanets.GetComponentDataArray<MoveSpeed>();

        var steerJob = new Steer
        {
            headings = headings,
            planetPositions = positions,
            speeds = speeds,
            sunPosition = _sun.transform.position
        };

        var steerJobHandle = steerJob.Schedule(_allPlanets.CalculateLength(), 64);
        steerJobHandle.Complete();

        inputDeps = steerJobHandle;
    
        return inputDeps;
    }
}
