using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/*
    The ConvertToEntity MonoBehaviour converts a GameObject and its children into Entities and ECS Components on load.
    Currently the set of built-in Unity MonoBehaviours that ConvertToEntity can convert includes the Transform and MeshRenderer.
    You can use the Entity Debugger (menu: Window > Analysis > Entity Debugger)
    to inspect the ECS Entities and Components created by the conversion.
 
    You can implement the IConvertGameObjectEntity interface on your own MonoBehaviours to supply a conversion function
    that ConvertToEntity will use to convert the data  stored in the MonoBehaviour to an ECS Component. 
 
    In this example, the RotationSpeedProxy MonoBehaviour uses IConvertGameObjectEntity to add the RotationSpeed Component
    to the Entity on conversion.
*/
namespace Samples.HelloCube
{
    // This system updates all entities in the scene with both a RotationSpeed and Rotation component.
    // In ECS all systems needs to derive from ComponentSystem and implements the OnUpdate method.
    public class RotationSpeedSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            // Entities.ForEach processes each set of ComponentData on the main thread. This is not the recommended
            // method for best performance. However, we start with it here to demonstrate the clearer separation
            // between ComponentSystem Update (logic) and ComponentData (data).
            // There is no update logic on the individual ComponentData.
            Entities.ForEach((ref RotationSpeed rotationSpeed, ref Rotation rotation) =>
            {
                var deltaTime = Time.deltaTime;
                rotation.Value = math.mul(math.normalize(rotation.Value),
                    quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * deltaTime));
            });
        }
    }
}
