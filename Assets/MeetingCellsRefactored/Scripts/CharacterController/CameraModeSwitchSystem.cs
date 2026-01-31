using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.CharacterController;

namespace MeetingCellsRefactored.CharacterController
{
    public enum CameraMode : byte
    {
        FirstPerson = 0,
        ThirdPerson = 1
    }

    /// <summary>
    /// Component that marks an entity as having switchable camera modes
    /// </summary>
    public struct HybridCameraController : IComponentData
    {
        public CameraMode CurrentMode;
        public CameraMode TargetMode;
        public float TransitionProgress;
        public byte IsTransitioning;
        public float TransitionDuration;
        public Entity FirstPersonCamera;
        public Entity ThirdPersonCamera;
    }

    /// <summary>
    /// Input command to switch camera mode
    /// </summary>
    public struct CameraModeSwitchRequest : IComponentData
    {
        public CameraMode RequestedMode;
    }

    /// <summary>
    /// System that handles smooth camera mode transitions
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct CameraModeSwitchSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder()
                .WithAll<HybridCameraController>()
                .Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Handle switch requests
            foreach (var (controller, request, entity) in
                SystemAPI.Query<RefRW<HybridCameraController>, RefRO<CameraModeSwitchRequest>>()
                    .WithEntityAccess())
            {
                if (controller.ValueRO.IsTransitioning == 0 &&
                    controller.ValueRO.CurrentMode != request.ValueRO.RequestedMode)
                {
                    controller.ValueRW.TargetMode = request.ValueRO.RequestedMode;
                    controller.ValueRW.IsTransitioning = 1;
                    controller.ValueRW.TransitionProgress = 0f;
                }

                // Remove the request after processing
                state.EntityManager.RemoveComponent<CameraModeSwitchRequest>(entity);
            }

            // Update transitions
            foreach (var controller in SystemAPI.Query<RefRW<HybridCameraController>>())
            {
                if (controller.ValueRO.IsTransitioning == 0) continue;

                float progress = controller.ValueRW.TransitionProgress +
                                 (deltaTime / controller.ValueRO.TransitionDuration);

                if (progress >= 1f)
                {
                    // Transition complete
                    controller.ValueRW.CurrentMode = controller.ValueRO.TargetMode;
                    controller.ValueRW.IsTransitioning = 0;
                    controller.ValueRW.TransitionProgress = 0f;
                }
                else
                {
                    controller.ValueRW.TransitionProgress = progress;
                }
            }

            // Update camera active states based on current mode
            foreach (var (controller, localTransform) in
                SystemAPI.Query<RefRO<HybridCameraController>, RefRO<LocalTransform>>())
            {
                UpdateCameraStates(ref state, controller.ValueRO, localTransform.ValueRO.Position);
            }
        }

        [BurstCompile]
        private void UpdateCameraStates(ref SystemState state, in HybridCameraController controller, float3 characterPosition)
        {
            // First Person Camera
            if (controller.FirstPersonCamera != Entity.Null &&
                SystemAPI.HasComponent<LocalTransform>(controller.FirstPersonCamera))
            {
                bool shouldBeActive = controller.CurrentMode == CameraMode.FirstPerson ||
                                      (controller.IsTransitioning != 0 && controller.TargetMode == CameraMode.FirstPerson);

                var fpTransform = SystemAPI.GetComponent<LocalTransform>(controller.FirstPersonCamera);

                // During transition, blend positions
                if (controller.IsTransitioning != 0)
                {
                    float t = EaseInOutCubic(controller.TransitionProgress);

                    if (controller.TargetMode == CameraMode.FirstPerson)
                    {
                        // Transitioning TO first person
                        var tpTransform = SystemAPI.GetComponent<LocalTransform>(controller.ThirdPersonCamera);
                        float3 targetPos = characterPosition + new float3(0, 1.6f, 0); // Eye level
                        fpTransform.Position = math.lerp(tpTransform.Position, targetPos, t);
                    }
                }

                SystemAPI.SetComponent(controller.FirstPersonCamera, fpTransform);
            }

            // Third Person Camera
            if (controller.ThirdPersonCamera != Entity.Null &&
                SystemAPI.HasComponent<OrbitCamera>(controller.ThirdPersonCamera))
            {
                bool shouldBeActive = controller.CurrentMode == CameraMode.ThirdPerson ||
                                      (controller.IsTransitioning != 0 && controller.TargetMode == CameraMode.ThirdPerson);

                var orbitCamera = SystemAPI.GetComponent<OrbitCamera>(controller.ThirdPersonCamera);

                // During transition, adjust distance
                if (controller.IsTransitioning != 0)
                {
                    float t = EaseInOutCubic(controller.TransitionProgress);

                    if (controller.TargetMode == CameraMode.FirstPerson)
                    {
                        // Transitioning TO first person - shrink distance
                        orbitCamera.TargetDistance = math.lerp(orbitCamera.TargetDistance, 0f, t);
                    }
                    else
                    {
                        // Transitioning TO third person - grow distance
                        orbitCamera.TargetDistance = math.lerp(0f, orbitCamera.TargetDistance, t);
                    }

                    SystemAPI.SetComponent(controller.ThirdPersonCamera, orbitCamera);
                }
            }
        }

        [BurstCompile]
        private float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - math.pow(-2f * t + 2f, 3f) / 2f;
        }
    }
}
