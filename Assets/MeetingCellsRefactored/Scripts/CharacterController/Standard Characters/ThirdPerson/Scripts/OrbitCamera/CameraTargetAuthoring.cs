using UnityEngine;
using Unity.Entities;

[DisallowMultipleComponent]
public class CameraTargetAuthoring : MonoBehaviour
{
    public GameObject Target;

    class CameraTargetBaker : Baker<CameraTargetAuthoring>
    {
        public override void Bake(CameraTargetAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CameraTarget
            {
                TargetEntity = GetEntity(authoring.Target, TransformUsageFlags.Dynamic),
            });
        }
    }
}
