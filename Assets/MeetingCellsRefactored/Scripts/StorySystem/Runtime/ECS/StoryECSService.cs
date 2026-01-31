using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace StorySystem.ECS
{
    /// <summary>
    /// Service to bridge traditional MonoBehaviour code with ECS story system
    /// </summary>
    public class StoryECSService : MonoBehaviour
    {
        private static StoryECSService _instance;
        public static StoryECSService Instance => _instance;

        private EntityManager _entityManager;
        private World _ecsWorld;

        [SerializeField] private bool createDefaultWorld = true;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (createDefaultWorld)
            {
                InitializeECS();
            }
        }

        private void InitializeECS()
        {
            // Get or create the default world
            _ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (_ecsWorld == null)
            {
                _ecsWorld = new World("StoryWorld");
                World.DefaultGameObjectInjectionWorld = _ecsWorld;
            }

            _entityManager = _ecsWorld.EntityManager;
        }

        /// <summary>
        /// Start a story execution from a baked graph entity
        /// </summary>
        public Entity StartStory(Entity graphEntity)
        {
            if (!_entityManager.HasComponent<StoryGraphData>(graphEntity))
            {
                Debug.LogError("Entity does not have StoryGraphData component");
                return Entity.Null;
            }

            // Create story execution entity
            var storyEntity = _entityManager.CreateEntity();

            // Add execution component
            _entityManager.AddComponentData(storyEntity, new StoryExecution
            {
                GraphEntity = graphEntity,
                CurrentNode = Entity.Null,
                State = StoryState.Idle,
                IsPaused = false
            });

            // Copy graph blob reference
            var graphData = _entityManager.GetComponentData<StoryGraphData>(graphEntity);
            _entityManager.AddComponentData(storyEntity, graphData);

            // Initialize variables from graph defaults
            var blob = graphData.GraphBlob;
            var variableBuffer = _entityManager.AddBuffer<StoryVariable>(storyEntity);

            for (int i = 0; i < blob.Value.VariableCount; i++)
            {
                var varDef = blob.Value.Variables[i];
                variableBuffer.Add(new StoryVariable
                {
                    VariableId = varDef.VariableId,
                    Type = varDef.Type,
                    FloatValue = varDef.DefaultFloat,
                    IntValue = varDef.DefaultInt,
                    BoolValue = varDef.DefaultBool
                });
            }

            // Add event buffer
            _entityManager.AddBuffer<StoryEvent>(storyEntity);

            return storyEntity;
        }

        /// <summary>
        /// Send input to a waiting story
        /// </summary>
        public void SendInput(Entity storyEntity, int choiceIndex = -1)
        {
            if (!_entityManager.Exists(storyEntity)) return;

            if (_entityManager.HasComponent<StoryExecution>(storyEntity))
            {
                var execution = _entityManager.GetComponentData<StoryExecution>(storyEntity);
                execution.InputReceived = true;
                execution.NextPortIndex = choiceIndex >= 0 ? choiceIndex : 0;
                _entityManager.SetComponentData(storyEntity, execution);
            }
        }

        /// <summary>
        /// Pause a story
        /// </summary>
        public void PauseStory(Entity storyEntity)
        {
            if (!_entityManager.Exists(storyEntity)) return;

            if (_entityManager.HasComponent<StoryExecution>(storyEntity))
            {
                var execution = _entityManager.GetComponentData<StoryExecution>(storyEntity);
                execution.IsPaused = true;
                _entityManager.SetComponentData(storyEntity, execution);
            }
        }

        /// <summary>
        /// Resume a paused story
        /// </summary>
        public void ResumeStory(Entity storyEntity)
        {
            if (!_entityManager.Exists(storyEntity)) return;

            if (_entityManager.HasComponent<StoryExecution>(storyEntity))
            {
                var execution = _entityManager.GetComponentData<StoryExecution>(storyEntity);
                execution.IsPaused = false;
                _entityManager.SetComponentData(storyEntity, execution);
            }
        }

        /// <summary>
        /// Stop and destroy a story
        /// </summary>
        public void StopStory(Entity storyEntity)
        {
            if (!_entityManager.Exists(storyEntity)) return;

            _entityManager.DestroyEntity(storyEntity);
        }

        /// <summary>
        /// Get the current state of a story
        /// </summary>
        public StoryState GetStoryState(Entity storyEntity)
        {
            if (!_entityManager.Exists(storyEntity)) return StoryState.Complete;

            if (_entityManager.HasComponent<StoryExecution>(storyEntity))
            {
                return _entityManager.GetComponentData<StoryExecution>(storyEntity).State;
            }

            return StoryState.Complete;
        }

        /// <summary>
        /// Set a variable value on a story
        /// </summary>
        public void SetVariable(Entity storyEntity, int variableId, float value)
        {
            if (!_entityManager.Exists(storyEntity)) return;

            if (_entityManager.HasComponent<StoryExecution>(storyEntity))
            {
                var variables = _entityManager.GetBuffer<StoryVariable>(storyEntity);
                StoryVariableOperations.SetVariable(ref variables, variableId, value);
            }
        }

        /// <summary>
        /// Get a variable value from a story
        /// </summary>
        public float GetVariableFloat(Entity storyEntity, int variableId, float defaultValue = 0f)
        {
            if (!_entityManager.Exists(storyEntity)) return defaultValue;

            if (_entityManager.HasComponent<StoryExecution>(storyEntity))
            {
                var variables = _entityManager.GetBuffer<StoryVariable>(storyEntity);
                return StoryVariableOperations.GetFloat(variables, variableId, defaultValue);
            }

            return defaultValue;
        }

        /// <summary>
        /// Query for active story events
        /// </summary>
        public NativeArray<StoryEvent> GetStoryEvents(Entity storyEntity, Allocator allocator)
        {
            if (!_entityManager.Exists(storyEntity))
            {
                return new NativeArray<StoryEvent>(0, allocator);
            }

            var buffer = _entityManager.GetBuffer<StoryEvent>(storyEntity);
            var events = new NativeArray<StoryEvent>(buffer.Length, allocator);

            for (int i = 0; i < buffer.Length; i++)
            {
                events[i] = buffer[i];
            }

            return events;
        }

        /// <summary>
        /// Clear story events
        /// </summary>
        public void ClearStoryEvents(Entity storyEntity)
        {
            if (!_entityManager.Exists(storyEntity)) return;

            if (_entityManager.HasComponent<StoryEvent>(storyEntity))
            {
                var buffer = _entityManager.GetBuffer<StoryEvent>(storyEntity);
                buffer.Clear();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
