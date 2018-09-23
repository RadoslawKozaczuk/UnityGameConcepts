using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Transforms;

public class PlanetSpawner
{
    // we cannot create publics here
    // the reason is that this code doesn't go anywhere to the hierarchy
    // Unity just finds it and runs it which makes it hard to pass anything here
    static EntityManager planetManager;
    static MeshInstanceRenderer planetRenderer;
    static EntityArchetype planetArchetype;
    
    // it doesn't matter how we call it
    // we doesn't have access to any Init Away or Update method
    // before the scene load call this method
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        planetManager = World.Active.GetOrCreateManager<EntityManager>();
        planetArchetype = planetManager.CreateArchetype(typeof(Position), // float3 representing the position
                                                        typeof(Heading),  // float3 showing the direction at the object is pointing
                                                        typeof(MoveForward),
                                                        typeof(TransformMatrix), // rotation, position and scaling
                                                        typeof(MoveSpeed)); // move speed plus heading equals velocity
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitWithScene()
    {
        // look in the hierarchy for a MeshInstanceRendererComponent and grab value of it
        planetRenderer = GameObject.FindObjectOfType<MeshInstanceRendererComponent>().Value;
        for(int i = 0; i < 400; i++)
        {
            SpawnPlanet();
        }
    }

    static void SpawnPlanet()
    {
        Entity planetEntity = planetManager.CreateEntity(planetArchetype);
        // here we can use Vector3 (object) because we are still somehow in the Unity
        Vector3 pos = Random.insideUnitSphere * 100;
        planetManager.SetComponentData(planetEntity, new Position { Value = new float3(pos.x, 0, pos.z) });
        planetManager.SetComponentData(planetEntity, new Heading { Value = new float3(1, 0, 0) });
        planetManager.SetComponentData(planetEntity, new MoveSpeed { speed = 15f });

        planetManager.AddSharedComponentData(planetEntity, planetRenderer);
    }
}
