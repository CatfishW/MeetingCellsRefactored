using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StorySystem.UI;
using StorySystem.Execution;
using StorySystem.Triggers;

namespace StorySystem.Editor
{
    /// <summary>
    /// Editor wizard for quickly setting up the story system in a scene
    /// </summary>
    public class StorySceneSetupWizard : EditorWindow
    {
        private bool createCanvas = true;
        private bool createDialogueUI = true;
        private bool createChoiceUI = true;
        private bool createStoryManager = true;
        private bool createDemoTrigger = false;

        [MenuItem("Tools/Story System/Scene Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<StorySceneSetupWizard>("Story Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Story System Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This wizard will set up the basic components needed for the story system to work in your scene.",
                MessageType.Info);

            GUILayout.Space(10);

            createCanvas = EditorGUILayout.Toggle("Create Canvas", createCanvas);
            createDialogueUI = EditorGUILayout.Toggle("Create Dialogue UI", createDialogueUI);
            createChoiceUI = EditorGUILayout.Toggle("Create Choice UI", createChoiceUI);
            createStoryManager = EditorGUILayout.Toggle("Create Story Manager", createStoryManager);
            createDemoTrigger = EditorGUILayout.Toggle("Create Demo Trigger", createDemoTrigger);

            GUILayout.Space(20);

            if (GUILayout.Button("Setup Scene", GUILayout.Height(40)))
            {
                SetupScene();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Quick Test Setup (Everything)"))
            {
                createCanvas = true;
                createDialogueUI = true;
                createChoiceUI = true;
                createStoryManager = true;
                createDemoTrigger = true;
                SetupScene();
            }
        }

        private void SetupScene()
        {
            int createdCount = 0;

            // Create Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (createCanvas && canvas == null)
            {
                GameObject canvasGO = new GameObject("StoryCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Story Canvas");
                createdCount++;
            }

            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Setup Failed", "No Canvas found or created. Please create a Canvas first.", "OK");
                return;
            }

            // Create Dialogue UI
            if (createDialogueUI && FindObjectOfType<DialogueUI>() == null)
            {
                CreateDialogueUI(canvas);
                createdCount++;
            }

            // Create Choice UI
            if (createChoiceUI && FindObjectOfType<ChoiceUI>() == null)
            {
                CreateChoiceUI(canvas);
                createdCount++;
            }

            // Create Story Manager
            if (createStoryManager && FindObjectOfType<StoryManager>() == null)
            {
                GameObject managerGO = new GameObject("StoryManager");
                managerGO.AddComponent<StoryManager>();
                Undo.RegisterCreatedObjectUndo(managerGO, "Create Story Manager");
                createdCount++;
            }

            // Create Demo Trigger
            if (createDemoTrigger)
            {
                GameObject triggerGO = new GameObject("DemoStoryTrigger");
                triggerGO.AddComponent<StoryTrigger>();
                triggerGO.transform.position = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(triggerGO, "Create Demo Trigger");
                createdCount++;
            }

            EditorUtility.DisplayDialog("Setup Complete",
                $"Created {createdCount} story system components.\n\n" +
                "Next steps:\n" +
                "1. Create a Story Graph (Right-click > Create > Story System > Story Graph)\n" +
                "2. Open the Story Graph Editor (Window > Story System > Story Graph Editor)\n" +
                "3. Design your story and assign it to triggers",
                "OK");
        }

        private void CreateDialogueUI(Canvas canvas)
        {
            // Create Dialogue Panel
            GameObject dialoguePanel = new GameObject("DialoguePanel");
            dialoguePanel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = dialoguePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.sizeDelta = new Vector2(-100, 200);
            panelRect.anchoredPosition = new Vector2(0, 50);

            Image panelImage = dialoguePanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);

            // Create Speaker Name
            GameObject speakerText = new GameObject("SpeakerName");
            speakerText.transform.SetParent(dialoguePanel.transform, false);
            TextMeshProUGUI speakerTMP = speakerText.AddComponent<TextMeshProUGUI>();
            speakerTMP.text = "Speaker";
            speakerTMP.fontSize = 24;
            speakerTMP.fontStyle = FontStyles.Bold;
            speakerTMP.color = Color.white;

            RectTransform speakerRect = speakerText.GetComponent<RectTransform>();
            speakerRect.anchorMin = new Vector2(0, 1);
            speakerRect.anchorMax = new Vector2(1, 1);
            speakerRect.pivot = new Vector2(0.5f, 1);
            speakerRect.sizeDelta = new Vector2(-20, 30);
            speakerRect.anchoredPosition = new Vector2(0, -10);

            // Create Dialogue Text
            GameObject dialogueText = new GameObject("DialogueText");
            dialogueText.transform.SetParent(dialoguePanel.transform, false);
            TextMeshProUGUI dialogueTMP = dialogueText.AddComponent<TextMeshProUGUI>();
            dialogueTMP.text = "Dialogue text will appear here...";
            dialogueTMP.fontSize = 18;
            dialogueTMP.color = Color.white;

            RectTransform dialogueRect = dialogueText.GetComponent<RectTransform>();
            dialogueRect.anchorMin = new Vector2(0, 0);
            dialogueRect.anchorMax = new Vector2(1, 1);
            dialogueRect.pivot = new Vector2(0.5f, 0.5f);
            dialogueRect.sizeDelta = new Vector2(-40, -60);
            dialogueRect.anchoredPosition = new Vector2(0, -20);

            // Add DialogueUI component
            DialogueUI dialogueUI = dialoguePanel.AddComponent<DialogueUI>();

            // Set references via serialized object
            SerializedObject so = new SerializedObject(dialogueUI);
            so.FindProperty("dialoguePanel").objectReferenceValue = dialoguePanel;
            so.FindProperty("speakerNameText").objectReferenceValue = speakerTMP;
            so.FindProperty("dialogueText").objectReferenceValue = dialogueTMP;
            so.ApplyModifiedProperties();

            dialoguePanel.SetActive(false);
            Undo.RegisterCreatedObjectUndo(dialoguePanel, "Create Dialogue UI");
        }

        private void CreateChoiceUI(Canvas canvas)
        {
            // Create Choice Panel
            GameObject choicePanel = new GameObject("ChoicePanel");
            choicePanel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = choicePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600, 400);

            Image panelImage = choicePanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.9f);

            // Create Prompt Text
            GameObject promptText = new GameObject("PromptText");
            promptText.transform.SetParent(choicePanel.transform, false);
            TextMeshProUGUI promptTMP = promptText.AddComponent<TextMeshProUGUI>();
            promptTMP.text = "What will you do?";
            promptTMP.fontSize = 24;
            promptTMP.alignment = TextAlignmentOptions.Center;
            promptTMP.color = Color.white;

            RectTransform promptRect = promptText.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0, 1);
            promptRect.anchorMax = new Vector2(1, 1);
            promptRect.pivot = new Vector2(0.5f, 1);
            promptRect.sizeDelta = new Vector2(-40, 50);
            promptRect.anchoredPosition = new Vector2(0, -20);

            // Create Choices Container
            GameObject choicesContainer = new GameObject("ChoicesContainer");
            choicesContainer.transform.SetParent(choicePanel.transform, false);

            RectTransform containerRect = choicesContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(-40, -100);
            containerRect.anchoredPosition = new Vector2(0, -30);

            // Add ChoiceUI component
            ChoiceUI choiceUI = choicePanel.AddComponent<ChoiceUI>();

            // Set references
            SerializedObject so = new SerializedObject(choiceUI);
            so.FindProperty("choicePanel").objectReferenceValue = choicePanel;
            so.FindProperty("promptText").objectReferenceValue = promptTMP;
            so.FindProperty("choicesContainer").objectReferenceValue = choicesContainer.transform;
            so.ApplyModifiedProperties();

            choicePanel.SetActive(false);
            Undo.RegisterCreatedObjectUndo(choicePanel, "Create Choice UI");
        }
    }
}
