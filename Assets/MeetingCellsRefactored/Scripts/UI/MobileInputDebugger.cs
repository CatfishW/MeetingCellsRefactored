using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MeetingCellsRefactored.UI
{
    /// <summary>
    /// Debug display for mobile input values
    /// Helps verify joysticks and touch look are working
    /// </summary>
    public class MobileInputDebugger : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private bool showDebugPanel = true;

        [Header("Debug Settings")]
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private float updateInterval = 0.1f;

        private float lastUpdateTime;
        private Vector2 lastMoveInput;
        private Vector2 lastLookInput;

        private void Start()
        {
            // Create debug UI if not assigned
            if (debugText == null)
            {
                CreateDebugUI();
            }

            // Subscribe to input events
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnMoveInput += OnMoveInput;
                InputManager.Instance.OnLookInput += OnLookInput;
            }
        }

        private void OnDestroy()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnMoveInput -= OnMoveInput;
                InputManager.Instance.OnLookInput -= OnLookInput;
            }
        }

        private void CreateDebugUI()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Create debug panel
            var panel = new GameObject("DebugPanel");
            panel.transform.SetParent(canvas.transform, false);

            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(300, 150);

            var image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.7f);

            // Create text
            var textObj = new GameObject("DebugText");
            textObj.transform.SetParent(panel.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            debugText = textObj.AddComponent<TextMeshProUGUI>();
            debugText.fontSize = 14;
            debugText.color = Color.green;
            debugText.alignment = TextAlignmentOptions.TopLeft;
        }

        private void OnMoveInput(Vector2 input)
        {
            lastMoveInput = input;
            if (logToConsole && Time.time > lastUpdateTime + updateInterval)
            {
                Debug.Log($"[MobileDebug] Move: {input}");
            }
        }

        private void OnLookInput(Vector2 input)
        {
            lastLookInput = input;
            if (logToConsole && Time.time > lastUpdateTime + updateInterval)
            {
                Debug.Log($"[MobileDebug] Look: {input}");
            }
        }

        private void Update()
        {
            if (!showDebugPanel || debugText == null) return;

            if (Time.time > lastUpdateTime + updateInterval)
            {
                UpdateDebugDisplay();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdateDebugDisplay()
        {
            bool isMobile = InputManager.Instance != null && InputManager.Instance.IsMobile;
            string platform = isMobile ? "MOBILE" : "PC";

            debugText.text = $"Platform: {platform}\n" +
                            $"Move: {lastMoveInput:F2}\n" +
                            $"Look: {lastLookInput:F2}\n" +
                            $"IsMobile: {isMobile}\n" +
                            $"Screen: {Screen.width}x{Screen.height}";
        }
    }
}
