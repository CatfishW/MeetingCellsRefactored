using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace StorySystem.ECS
{
    /// <summary>
    /// Performance comparison between traditional and ECS story systems
    /// </summary>
    public class StoryECSPerformanceComparison : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private int storyCount = 1000;
        [SerializeField] private int iterationsPerStory = 100;
        [SerializeField] private bool runOnStart = true;

        [Header("Results")]
        [SerializeField] private float traditionalTimeMs;
        [SerializeField] private float ecsTimeMs;
        [SerializeField] private float speedupFactor;

        private void Start()
        {
            if (runOnStart)
            {
                RunComparison();
            }
        }

        [ContextMenu("Run Performance Comparison")]
        public void RunComparison()
        {
            Debug.Log($"=== Story System Performance Comparison ===");
            Debug.Log($"Stories: {storyCount}, Iterations: {iterationsPerStory}");

            // Traditional approach benchmark
            traditionalTimeMs = BenchmarkTraditional();
            Debug.Log($"Traditional: {traditionalTimeMs:F2}ms");

            // ECS approach benchmark
            ecsTimeMs = BenchmarkECS();
            Debug.Log($"ECS: {ecsTimeMs:F2}ms");

            speedupFactor = traditionalTimeMs / ecsTimeMs;
            Debug.Log($"Speedup: {speedupFactor:F2}x");
        }

        private float BenchmarkTraditional()
        {
            var stopwatch = Stopwatch.StartNew();

            // Simulate traditional story system overhead
            // Dictionary lookups, boxing/unboxing, coroutine overhead
            var contexts = new List<MockStoryContext>(storyCount);

            for (int i = 0; i < storyCount; i++)
            {
                contexts.Add(new MockStoryContext
                {
                    variables = new Dictionary<string, object>(),
                    currentNode = i % 10
                });

                // Initialize variables (boxing overhead)
                contexts[i].variables["health"] = 100f;
                contexts[i].variables["score"] = 0;
                contexts[i].variables["hasKey"] = false;
            }

            // Simulate node execution with condition evaluation
            for (int iter = 0; iter < iterationsPerStory; iter++)
            {
                for (int i = 0; i < storyCount; i++)
                {
                    var ctx = contexts[i];

                    // Variable access with boxing
                    float health = (float)ctx.variables["health"];
                    int score = (int)ctx.variables["score"];
                    bool hasKey = (bool)ctx.variables["hasKey"];

                    // Condition evaluation
                    if (health > 50)
                    {
                        ctx.variables["score"] = score + 10;
                    }

                    if (score % 100 == 0)
                    {
                        ctx.variables["hasKey"] = true;
                    }

                    // Simulate node transition
                    ctx.currentNode = (ctx.currentNode + 1) % 10;
                }
            }

            stopwatch.Stop();
            return (float)stopwatch.Elapsed.TotalMilliseconds;
        }

        private float BenchmarkECS()
        {
            var stopwatch = Stopwatch.StartNew();

            // Simulate ECS approach
            // Struct arrays, no boxing, cache-friendly
            var executions = new MockStoryExecution[storyCount];
            var variables = new MockStoryVariable[storyCount * 3];

            for (int i = 0; i < storyCount; i++)
            {
                executions[i] = new MockStoryExecution
                {
                    currentNode = i % 10,
                    variableOffset = i * 3,
                    variableCount = 3
                };

                int offset = i * 3;
                variables[offset] = new MockStoryVariable { id = 0, type = 0, floatValue = 100f }; // health
                variables[offset + 1] = new MockStoryVariable { id = 1, type = 1, intValue = 0 };    // score
                variables[offset + 2] = new MockStoryVariable { id = 2, type = 2, boolValue = 0 };   // hasKey
            }

            // Simulate Burst-compiled execution
            for (int iter = 0; iter < iterationsPerStory; iter++)
            {
                for (int i = 0; i < storyCount; i++)
                {
                    var exec = executions[i];
                    int offset = exec.variableOffset;

                    // Direct struct access, no boxing
                    float health = variables[offset].floatValue;
                    int score = variables[offset + 1].intValue;
                    int hasKey = variables[offset + 2].boolValue;

                    // Branchless condition evaluation
                    int healthBonus = health > 50f ? 10 : 0;
                    variables[offset + 1].intValue = score + healthBonus;

                    int keyCondition = (score % 100 == 0) ? 1 : 0;
                    variables[offset + 2].boolValue = keyCondition;

                    // Node transition
                    executions[i].currentNode = (exec.currentNode + 1) % 10;
                }
            }

            stopwatch.Stop();
            return (float)stopwatch.Elapsed.TotalMilliseconds;
        }

        // Mock classes for traditional approach
        private class MockStoryContext
        {
            public Dictionary<string, object> variables;
            public int currentNode;
        }

        // Mock structs for ECS approach
        private struct MockStoryExecution
        {
            public int currentNode;
            public int variableOffset;
            public int variableCount;
        }

        private struct MockStoryVariable
        {
            public int id;
            public int type;
            public float floatValue;
            public int intValue;
            public int boolValue;
        }
    }
}
