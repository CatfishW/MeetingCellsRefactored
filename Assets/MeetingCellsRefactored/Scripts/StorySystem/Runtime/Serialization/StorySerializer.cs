using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Serialization
{
    /// <summary>
    /// Handles serialization/deserialization of story graphs to/from JSON
    /// </summary>
    public static class StorySerializer
    {
        #region JSON Structure Classes

        [Serializable]
        private class StoryGraphJson
        {
            public string graphId;
            public string graphName;
            public string description;
            public List<StoryNodeJson> nodes;
            public List<StoryConnectionJson> connections;
            public List<StoryVariableJson> variables;
            public ViewStateJson viewState;
        }

        [Serializable]
        private class StoryNodeJson
        {
            public string nodeId;
            public string nodeType;
            public string nodeName;
            public string nodeDescription;
            public Vector2Json position;
            public Dictionary<string, object> data;
        }

        [Serializable]
        private class StoryConnectionJson
        {
            public string connectionId;
            public string outputNodeId;
            public string outputPortId;
            public string inputNodeId;
            public string inputPortId;
        }

        [Serializable]
        private class StoryVariableJson
        {
            public string name;
            public string type;
            public string defaultValue;
        }

        [Serializable]
        private class Vector2Json
        {
            public float x;
            public float y;
        }

        [Serializable]
        private class ViewStateJson
        {
            public Vector2Json offset;
            public float scale;
        }

        #endregion

        public static string ToJson(StoryGraph graph, bool prettyPrint = true)
        {
            var json = new StoryGraphJson
            {
                graphId = graph.GraphId,
                graphName = graph.GraphName,
                description = graph.Description,
                nodes = new List<StoryNodeJson>(),
                connections = new List<StoryConnectionJson>(),
                variables = new List<StoryVariableJson>(),
                viewState = new ViewStateJson
                {
                    offset = new Vector2Json { x = graph.ViewOffset.x, y = graph.ViewOffset.y },
                    scale = graph.ViewScale
                }
            };

            // Serialize nodes
            foreach (var node in graph.Nodes)
            {
                var nodeJson = new StoryNodeJson
                {
                    nodeId = node.NodeId,
                    nodeType = node.GetType().FullName,
                    nodeName = node.NodeName,
                    nodeDescription = node.NodeDescription,
                    position = new Vector2Json { x = node.Position.x, y = node.Position.y },
                    data = node.GetSerializationData()
                };
                json.nodes.Add(nodeJson);
            }

            // Serialize connections
            foreach (var connection in graph.Connections)
            {
                json.connections.Add(new StoryConnectionJson
                {
                    connectionId = connection.ConnectionId,
                    outputNodeId = connection.OutputNodeId,
                    outputPortId = connection.OutputPortId,
                    inputNodeId = connection.InputNodeId,
                    inputPortId = connection.InputPortId
                });
            }

            // Serialize variables
            foreach (var variable in graph.Variables)
            {
                json.variables.Add(new StoryVariableJson
                {
                    name = variable.Name,
                    type = variable.Type.ToString(),
                    defaultValue = variable.GetDefaultValue()?.ToString()
                });
            }

            return JsonUtility.ToJson(json, prettyPrint);
        }

        public static StoryGraph FromJson(string jsonString)
        {
            try
            {
                var json = JsonUtility.FromJson<StoryGraphJson>(jsonString);
                
                // For complex nested objects, we need custom parsing
                var fullJson = MiniJson.Deserialize(jsonString) as Dictionary<string, object>;
                
                var graph = ScriptableObject.CreateInstance<StoryGraph>();
                graph.Initialize(json.graphId, json.graphName);
                graph.SetDescription(json.description);

                if (fullJson.TryGetValue("viewState", out var viewStateObj) && 
                    viewStateObj is Dictionary<string, object> viewState)
                {
                    if (viewState.TryGetValue("offset", out var offsetObj) && 
                        offsetObj is Dictionary<string, object> offset)
                    {
                        graph.ViewOffset = new Vector2(
                            Convert.ToSingle(offset["x"]),
                            Convert.ToSingle(offset["y"])
                        );
                    }
                    if (viewState.TryGetValue("scale", out var scale))
                    {
                        graph.ViewScale = Convert.ToSingle(scale);
                    }
                }

                // Deserialize nodes
                if (fullJson.TryGetValue("nodes", out var nodesObj) && nodesObj is List<object> nodesList)
                {
                    foreach (var nodeObj in nodesList)
                    {
                        if (nodeObj is Dictionary<string, object> nodeData)
                        {
                            var node = NodeFactory.CreateNode(nodeData);
                            if (node != null)
                            {
                                graph.AddNode(node);
                            }
                        }
                    }
                }

                // Deserialize connections
                if (fullJson.TryGetValue("connections", out var connsObj) && connsObj is List<object> connsList)
                {
                    foreach (var connObj in connsList)
                    {
                        if (connObj is Dictionary<string, object> connData)
                        {
                            graph.CreateConnection(
                                connData["outputNodeId"].ToString(),
                                connData["outputPortId"].ToString(),
                                connData["inputNodeId"].ToString(),
                                connData["inputPortId"].ToString()
                            );
                        }
                    }
                }

                // Deserialize variables
                if (fullJson.TryGetValue("variables", out var varsObj) && varsObj is List<object> varsList)
                {
                    foreach (var varObj in varsList)
                    {
                        if (varObj is Dictionary<string, object> varData)
                        {
                            var varType = (VariableType)Enum.Parse(typeof(VariableType), varData["type"].ToString());
                            var variable = new StoryVariable(varData["name"].ToString(), varType);
                            if (varData.TryGetValue("defaultValue", out var defVal))
                            {
                                variable.SetDefaultValue(defVal);
                            }
                            graph.AddVariable(variable);
                        }
                    }
                }

                return graph;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse story JSON: {ex.Message}");
                return null;
            }
        }

        public static void SaveToFile(StoryGraph graph, string filePath)
        {
            string json = ToJson(graph);
            File.WriteAllText(filePath, json);
            Debug.Log($"Story graph saved to: {filePath}");
        }

        public static StoryGraph LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found: {filePath}");
                return null;
            }

            string json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        public static StoryGraph LoadFromJson(string path)
        {
            string fullPath;
            
            // Check if path is absolute or relative to StreamingAssets
            if (Path.IsPathRooted(path))
            {
                fullPath = path;
            }
            else
            {
                fullPath = Path.Combine(Application.streamingAssetsPath, path);
            }

            return LoadFromFile(fullPath);
        }
    }

    /// <summary>
    /// Simple JSON parser for handling complex nested structures
    /// </summary>
    public static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            return Parser.Parse(json);
        }

        private sealed class Parser : IDisposable
        {
            private const string WORD_BREAK = "{}[],:\"";
            private StringReader json;

            private Parser(string jsonString)
            {
                json = new StringReader(jsonString);
            }

            public static object Parse(string jsonString)
            {
                using (var instance = new Parser(jsonString))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                json.Dispose();
                json = null;
            }

            private Dictionary<string, object> ParseObject()
            {
                Dictionary<string, object> table = new Dictionary<string, object>();
                json.Read(); // {
                while (true)
                {
                    switch (NextToken)
                    {
                        case TOKEN.NONE: return null;
                        case TOKEN.CURLY_CLOSE: return table;
                        default:
                            string name = ParseString();
                            if (name == null) return null;
                            if (NextToken != TOKEN.COLON) return null;
                            json.Read();
                            table[name] = ParseValue();
                            break;
                    }
                }
            }

            private List<object> ParseArray()
            {
                List<object> array = new List<object>();
                json.Read(); // [
                bool parsing = true;
                while (parsing)
                {
                    TOKEN nextToken = NextToken;
                    switch (nextToken)
                    {
                        case TOKEN.NONE: return null;
                        case TOKEN.SQUARED_CLOSE: parsing = false; break;
                        default:
                            object value = ParseByToken(nextToken);
                            array.Add(value);
                            break;
                    }
                }
                return array;
            }

            private object ParseValue()
            {
                TOKEN nextToken = NextToken;
                return ParseByToken(nextToken);
            }

            private object ParseByToken(TOKEN token)
            {
                switch (token)
                {
                    case TOKEN.STRING: return ParseString();
                    case TOKEN.NUMBER: return ParseNumber();
                    case TOKEN.CURLY_OPEN: return ParseObject();
                    case TOKEN.SQUARED_OPEN: return ParseArray();
                    case TOKEN.TRUE: return true;
                    case TOKEN.FALSE: return false;
                    case TOKEN.NULL: return null;
                    default: return null;
                }
            }

            private string ParseString()
            {
                System.Text.StringBuilder s = new System.Text.StringBuilder();
                char c;
                json.Read(); // "
                bool parsing = true;
                while (parsing)
                {
                    if (json.Peek() == -1)
                    {
                        parsing = false;
                        break;
                    }
                    c = NextChar;
                    switch (c)
                    {
                        case '"': parsing = false; break;
                        case '\\':
                            if (json.Peek() == -1)
                            {
                                parsing = false;
                                break;
                            }
                            c = NextChar;
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/': s.Append(c); break;
                                case 'b': s.Append('\b'); break;
                                case 'f': s.Append('\f'); break;
                                case 'n': s.Append('\n'); break;
                                case 'r': s.Append('\r'); break;
                                case 't': s.Append('\t'); break;
                                case 'u':
                                    var hex = new char[4];
                                    for (int i = 0; i < 4; i++) hex[i] = NextChar;
                                    s.Append((char)Convert.ToInt32(new string(hex), 16));
                                    break;
                            }
                            break;
                        default: s.Append(c); break;
                    }
                }
                return s.ToString();
            }

            private object ParseNumber()
            {
                string number = NextWord;
                if (number.IndexOf('.') == -1)
                {
                    long parsedInt;
                    Int64.TryParse(number, out parsedInt);
                    return parsedInt;
                }
                double parsedDouble;
                Double.TryParse(number, out parsedDouble);
                return parsedDouble;
            }

            private void EatWhitespace()
            {
                while (Char.IsWhiteSpace(PeekChar)) { json.Read(); if (json.Peek() == -1) break; }
            }

            private char PeekChar => Convert.ToChar(json.Peek());
            private char NextChar => Convert.ToChar(json.Read());

            private string NextWord
            {
                get
                {
                    System.Text.StringBuilder word = new System.Text.StringBuilder();
                    while (!IsWordBreak(PeekChar))
                    {
                        word.Append(NextChar);
                        if (json.Peek() == -1) break;
                    }
                    return word.ToString();
                }
            }

            private TOKEN NextToken
            {
                get
                {
                    EatWhitespace();
                    if (json.Peek() == -1) return TOKEN.NONE;
                    switch (PeekChar)
                    {
                        case '{': return TOKEN.CURLY_OPEN;
                        case '}': json.Read(); return TOKEN.CURLY_CLOSE;
                        case '[': return TOKEN.SQUARED_OPEN;
                        case ']': json.Read(); return TOKEN.SQUARED_CLOSE;
                        case ',': json.Read(); return TOKEN.COMMA;
                        case '"': return TOKEN.STRING;
                        case ':': return TOKEN.COLON;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-': return TOKEN.NUMBER;
                    }
                    switch (NextWord)
                    {
                        case "false": return TOKEN.FALSE;
                        case "true": return TOKEN.TRUE;
                        case "null": return TOKEN.NULL;
                    }
                    return TOKEN.NONE;
                }
            }

            private static bool IsWordBreak(char c) => Char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;

            private enum TOKEN
            {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARED_OPEN,
                SQUARED_CLOSE,
                COLON,
                COMMA,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            }
        }
    }
}