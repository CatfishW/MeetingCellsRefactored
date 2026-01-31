using UnityEngine;
using UnityEngine.EventSystems;

namespace MeetingCellsRefactored.UI
{
    /// <summary>
    /// Touch drag on right side of screen to control camera view
    /// Replaces right joystick for more natural mobile FPS controls
    /// </summary>
    public class ScreenTouchLook : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Touch Settings")]
        [SerializeField] private float sensitivity = 2f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private float deadZone = 0.05f;

        [Header("Screen Partition")]
        [SerializeField, Range(0, 1)] private float screenPartitionX = 0.5f;
        [SerializeField] private bool useRightHalf = true;

        [Header("Visual Feedback")]
        [SerializeField] private bool showTouchIndicator = true;
        [SerializeField] private GameObject touchIndicatorPrefab;

        private Vector2 currentInput;
        private Vector2 previousTouchPosition;
        private bool isTouching = false;
        private int currentTouchId = -1;
        private Canvas canvas;
        private GameObject currentIndicator;
        private RectTransform rectTransform;

        public Vector2 LookInput => currentInput;
        public bool IsTouching => isTouching;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();

            // Make sure this covers the entire screen portion
            SetupRectTransform();
        }

        private void SetupRectTransform()
        {
            if (rectTransform == null) return;

            rectTransform.anchorMin = useRightHalf ? new Vector2(screenPartitionX, 0) : new Vector2(0, 0);
            rectTransform.anchorMax = useRightHalf ? new Vector2(1, 1) : new Vector2(screenPartitionX, 1);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private void Update()
        {
            // Input is set during drag events and decays when not dragging
            // Don't reset to zero here as it happens before drag events are processed
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Only accept touches on the correct side of screen
            if (!IsValidTouchPosition(eventData.position))
                return;

            isTouching = true;
            currentTouchId = eventData.pointerId;
            previousTouchPosition = eventData.position;

            if (showTouchIndicator && touchIndicatorPrefab != null)
            {
                ShowTouchIndicator(eventData.position);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (currentTouchId != eventData.pointerId) return;
            previousTouchPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isTouching || currentTouchId != eventData.pointerId)
                return;

            Vector2 delta = eventData.position - previousTouchPosition;

            // Normalize by screen size for consistent sensitivity across devices
            delta.x /= Screen.width;
            delta.y /= Screen.height;

            // Apply sensitivity
            delta *= sensitivity * 100f;

            // Apply deadzone
            if (delta.magnitude < deadZone)
            {
                delta = Vector2.zero;
            }

            // Invert Y if configured
            if (invertY)
            {
                delta.y *= -1;
            }

            currentInput = delta;
            previousTouchPosition = eventData.position;

            // Update indicator position
            if (currentIndicator != null)
            {
                UpdateIndicatorPosition(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (currentTouchId != eventData.pointerId) return;
            currentInput = Vector2.zero;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (currentTouchId != eventData.pointerId) return;

            isTouching = false;
            currentTouchId = -1;
            currentInput = Vector2.zero;

            HideTouchIndicator();
        }

        private bool IsValidTouchPosition(Vector2 screenPosition)
        {
            float partitionX = Screen.width * screenPartitionX;

            if (useRightHalf)
            {
                return screenPosition.x >= partitionX;
            }
            else
            {
                return screenPosition.x < partitionX;
            }
        }

        private void ShowTouchIndicator(Vector2 screenPosition)
        {
            if (touchIndicatorPrefab == null || canvas == null) return;

            currentIndicator = Instantiate(touchIndicatorPrefab, canvas.transform);
            UpdateIndicatorPosition(screenPosition);
        }

        private void UpdateIndicatorPosition(Vector2 screenPosition)
        {
            if (currentIndicator == null) return;

            RectTransform indicatorRect = currentIndicator.GetComponent<RectTransform>();
            if (indicatorRect != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    screenPosition,
                    canvas.worldCamera,
                    out localPoint))
                {
                    indicatorRect.anchoredPosition = localPoint;
                }
            }
        }

        private void HideTouchIndicator()
        {
            if (currentIndicator != null)
            {
                Destroy(currentIndicator);
                currentIndicator = null;
            }
        }

        public void SetSensitivity(float newSensitivity)
        {
            sensitivity = newSensitivity;
        }

        public void SetInvertY(bool invert)
        {
            invertY = invert;
        }

        private void OnValidate()
        {
            SetupRectTransform();
        }
    }
}
