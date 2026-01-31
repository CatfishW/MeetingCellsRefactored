using Unity.Entities;
using Unity.Burst;

namespace StorySystem.ECS
{
    /// <summary>
    /// Bootstrap system that initializes the story ECS world
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct StorySystemBootstrap : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Ensure required systems are present
            state.RequireForUpdate<StoryExecution>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Bootstrap logic if needed
        }
    }
}
