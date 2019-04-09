using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/*
    In a Job implemented with IJobChunk, the ECS framework passes an ArchetypeChunk instance to your Execute() function
    for each chunk of memory containing the required Components. You can then iterate through
    the arrays of Components stored in that chunk.

    Notice that you have to do a little more manual setup for an IJobChunkJob.
    This includes constructing a ComponentGroup that identifies which Component types the System operates upon.
    You must also pass ArchetypeChunkComponentType objects to the Job, which you then use inside the Job
    to get the NativeArray instances required to access the Component arrays themselves. 

    Systems using IJobChunk can handle more complex situations than those supported by IJobProcessComponentData,
    while maintaining maximum efficiency.
*/

namespace Samples.HelloCube
{
    // This system updates all entities in the scene with both a RotationSpeed and Rotation component.
    public class RotationSpeedSystemJobChunk : JobComponentSystem
    {
        ComponentGroup _componentGroup;

        protected override void OnCreateManager()
        {
            // Cached access to a set of ComponentData based on a specific query
            _componentGroup = GetComponentGroup(typeof(Rotation), ComponentType.ReadOnly<RotationSpeed>());
        }

        // Use the [BurstCompile] attribute to compile a job with Burst. You may see significant speed ups, so try it!
        [BurstCompile]
        struct RotationSpeedJob : IJobChunk
        {
            public float DeltaTime;
            public ArchetypeChunkComponentType<Rotation> RotationType;
            [ReadOnly] public ArchetypeChunkComponentType<RotationSpeed> RotationSpeedType;
    
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkRotations = chunk.GetNativeArray(RotationType);
                var chunkRotationSpeeds = chunk.GetNativeArray(RotationSpeedType);
                for (var i = 0; i < chunk.Count; i++)
                {
                    var rotation = chunkRotations[i];
                    var rotationSpeed = chunkRotationSpeeds[i];
                    
                    // Rotate something about its up vector at the speed given by RotationSpeed.
                    chunkRotations[i] = new Rotation
                    {
                        Value = math.mul(
                            math.normalize(rotation.Value),
                            quaternion.AxisAngle(math.up(),
                            rotationSpeed.RadiansPerSecond * DeltaTime))
                    };
                }
            }
        }
    
        // OnUpdate runs on the main thread.
        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            // Explicitly declare:
            // - Read-Write access to Rotation
            // - Read-Only access to RotationSpeed
            var rotationType = GetArchetypeChunkComponentType<Rotation>(false);
            var rotationSpeedType = GetArchetypeChunkComponentType<RotationSpeed>(true);
            
            var job = new RotationSpeedJob()
            {
                RotationType = rotationType,
                RotationSpeedType = rotationSpeedType,
                DeltaTime = Time.deltaTime
            };
    
            return job.Schedule(_componentGroup, inputDependencies);
        }
    }
}
