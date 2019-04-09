using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.HelloCube
{
    [RequiresEntityConversion] // everything works even when this is commented out
    public class RotationSpeedProxy : MonoBehaviour,
        IConvertGameObjectToEntity // when this is attached to an object it will be automatically converted to an entity
                                   // and destroyed from the hierarchy (if mode is set to Convert And Destroy)
    {
        public float DegreesPerSecond;
        
        // The MonoBehaviour data is converted to ComponentData on the entity.
        // We are specifically transforming from a good editor representation of the data (Represented in degrees)
        // To a good runtime representation (Represented in radians)
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // all parameters are injected we just need to create data
            var data = new RotationSpeed
            {
                RadiansPerSecond = math.radians(DegreesPerSecond)
            };

            dstManager.AddComponentData(entity, data);
        }
    }
}
