using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MeetingCellsRefactored.UI
{
    /// <summary>
    /// Floating joystick for mobile touch input
    /// Appears at touch position and follows finger movement
    /// </summary>
    public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Visual Components")]
        [SerializeField] private Image joystickBackground;
        [SerializeField] private Image joystickHandle;

        [Header("Joystick Settings")]
        [SerializeField] private float handleRange = 1f;
        [SerializeField] private float deadZone = 0.1f;
        [SerializeField] private bool snapToFinger = true;
        [SerializeField] private bool fadeWhenNotUsed = true;
        [SerializeField] private float fadeDuration = 0.2f;

        [Header("Scale Settings")]
        [SerializeField] private float joystickScale = 1f;

        private Vector2 input = Vector2.zero;
        private Vector2 origin;
        private float radius;
        private bool isDragging = false;
        private Canvas canvas;
        private Camera cam;
        private RectTransform rectTransform;
        private RectTransform backgroundRect;
        private RectTransform handleRect;
        private CanvasGroup canvasGroup;
        private Vector2 defaultPosition;

        public Vector2 Direction => input;
        public bool IsDragging => isDragging;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (joystickBackground != null)
                backgroundRect = joystickBackground.rectTransform;
            if (joystickHandle != null)
                handleRect = joystickHandle.rectTransform;

            canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                cam = canvas.worldCamera;
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    cam = null;
            }

            // Store default position
            if (backgroundRect != null)
                defaultPosition = backgroundRect.anchoredPosition;

            // Calculate radius based on background size
            if (backgroundRect != null)
            {
                radius = backgroundRect.sizeDelta.x / 2f * joystickScale;
                backgroundRect.localScale = Vector3.one * joystickScale;
            }

            // Initially hide joystick
            if (fadeWhenNotUsed)
            {
                SetOpacity(0.3f);
            }
        }

        private void Start()
        {
            // Center the handle
            if (handleRect != null)
                handleRect.anchoredPosition = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isDragging = true;

            if (snapToFinger && backgroundRect != null)
            {
                // Move joystick to touch position
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    eventData.position,
                    cam,
                    out localPoint))
                {
                    backgroundRect.anchoredPosition = localPoint;
                }
            }

            // Fade in
            if (fadeWhenNotUsed)
            {
                SetOpacity(1f);
            }

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || backgroundRect == null) return;

            // Get the joystick center position in screen space
            Vector2 joystickCenter;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                joystickCenter = RectTransformUtility.WorldToScreenPoint(null, backgroundRect.position);
            }
            else
            {
                joystickCenter = RectTransformUtility.WorldToScreenPoint(cam, backgroundRect.position);
            }

            // Calculate direction from joystick center to touch position
            Vector2 direction = eventData.position - joystickCenter;

            // Handle canvas scaling for different resolutions
            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            direction /= scaleFactor;

            // Normalize input based on radius
            float distance = direction.magnitude;
            if (distance > radius)
            {
                input = direction.normalized;
            }
            else
            {
                input = direction / radius;
            }

            // Apply deadzone
            if (input.magnitude < deadZone)
            {
                input = Vector2.zero;
            }

            // Update handle position visually
            if (handleRect != null)
            {
                handleRect.anchoredPosition = input * radius * handleRange;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
            input = Vector2.zero;

            // Reset handle position
            if (handleRect != null)
                handleRect.anchoredPosition = Vector2.zero;

            // Fade out
            if (fadeWhenNotUsed)
            {
                SetOpacity(0.3f);
            }

            // Return to original position if snapping
            if (snapToFinger && backgroundRect != null)
            {
                backgroundRect.anchoredPosition = defaultPosition;
            }
        }

        private void SetOpacity(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
            else if (joystickBackground != null)
            {
                Color bgColor = joystickBackground.color;
                bgColor.a = alpha;
                joystickBackground.color = bgColor;
            }
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        /// <summary>
        /// Set the handle range (0-1)
        /// </summary>
        public void SetHandleRange(float range)
        {
            handleRange = Mathf.Clamp01(range);
        }

        /// <summary>
        /// Set the dead zone (0-1)
        /// </summary>
        public void SetDeadZone(float zone)
        {
            deadZone = Mathf.Clamp01(zone);
        }

        /// <summary>
        /// Enable/disable snap to finger
        /// </summary>
        public void SetSnapToFinger(bool snap)
        {
            snapToFinger = snap;
        }

        /// <summary>
        /// Enable/disable fade when not used
        /// </summary>
        public void SetFadeWhenNotUsed(bool fade)
        {
            fadeWhenNotUsed = fade;
        }

        /// <summary>
        /// Reset the joystick to center position
        /// </summary>
        public void ResetJoystick()
        {
            isDragging = false;
            input = Vector2.zero;
            if (handleRect != null)
                handleRect.anchoredPosition = Vector2.zero;
            if (backgroundRect != null)
                backgroundRect.anchoredPosition = defaultPosition;
        }
    }
}
