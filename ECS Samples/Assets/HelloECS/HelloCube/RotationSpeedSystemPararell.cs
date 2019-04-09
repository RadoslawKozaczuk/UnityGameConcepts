using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/*
    Systems using IJobProcessComponentData are the simplest efficient method you can use to process your Component data.
    We recommend starting with this approach for any System that you design.
    In this example, RotationSpeedSystem is now implemented as a JobComponentSystem.
    The class creates an IJobProcessComponentData struct to define the work that needs to be done.
    This Job is scheduled in the System's OnUpdate() function.  
*/

namespace Samples.HelloCube
{
    // This system updates all entities in the scene with both a RotationSpeed and Rotation component.
    public class RotationSpeedSystemPararell : JobComponentSystem
    {
        // Use the [BurstCompile] attribute to compile a job with Burst. You may see significant speed ups, so try it!
        [BurstCompile]
        struct RotationSpeedJob : IJobProcessComponentData<Rotation, RotationSpeed>
        {
            public float DeltaTime;
    
            // The [ReadOnly] attribute tells the job scheduler that this job will not write to rotSpeed
            public void Execute(ref Rotation rotation, [ReadOnly] ref RotationSpeed rotSpeed)
            {
                // Rotate something about its up vector at the speed given by RotationSpeed.
                rotation.Value = math.mul(
                    math.normalize(rotation.Value),
                    quaternion.AxisAngle(math.up(),
                    rotSpeed.RadiansPerSecond * DeltaTime));
            }
        }
    
        // OnUpdate runs on the main thread.
        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            // because it runs on the main thred we have access to all necessary data
            var job = new RotationSpeedJob() { DeltaTime = Time.deltaTime };

            return job.Schedule(this, inputDependencies);
        }
    }
}
