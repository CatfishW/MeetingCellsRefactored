using UnityEngine;
using System;
using System.Collections;

namespace MeetingCellsRefactored.UI
{
    /// <summary>
    /// Base class for all UI panels
    /// Provides show/hide animations and lifecycle management
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        [Header("Panel Settings")]
        [SerializeField] private string panelName;
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private bool blockInputWhenOpen = true;
        [SerializeField] private bool closeOnEscape = true;

        [Header("Animation")]
        [SerializeField] private PanelAnimationType showAnimation = PanelAnimationType.Fade;
        [SerializeField] private PanelAnimationType hideAnimation = PanelAnimationType.Fade;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Visual Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private GameObject backgroundBlocker;

        // Events
        public event Action OnPanelShow;
        public event Action OnPanelHide;
        public event Action OnPanelShown;
        public event Action OnPanelHidden;

        // State
        private bool isVisible = false;
        private bool isAnimating = false;
        private Coroutine currentAnimation;
        private UIManager uiManager;
        private Vector2 originalPosition;

        public enum PanelAnimationType
        {
            None,
            Fade,
            Scale,
            SlideFromLeft,
            SlideFromRight,
            SlideFromTop,
            SlideFromBottom,
            Pop
        }

        public string PanelName => string.IsNullOrEmpty(panelName) ? gameObject.name : panelName;
        public bool IsVisible => isVisible;
        public bool IsAnimating => isAnimating;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (panelTransform == null)
                panelTransform = GetComponent<RectTransform>();

            originalPosition = panelTransform != null ? panelTransform.anchoredPosition : Vector2.zero;
        }

        public void Initialize(UIManager manager)
        {
            uiManager = manager;
        }

        #region Show/Hide

        public void Show()
        {
            if (isVisible || isAnimating) return;

            gameObject.SetActive(true);
            OnPanelShow?.Invoke();

            if (pauseGameWhenOpen)
                Time.timeScale = 0f;

            if (blockInputWhenOpen)
            {
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = true;
            }

            if (backgroundBlocker != null)
                backgroundBlocker.SetActive(true);

            // Stop any existing animation
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);

            currentAnimation = StartCoroutine(AnimateShow());
        }

        public void Hide()
        {
            if (!isVisible || isAnimating) return;

            OnPanelHide?.Invoke();

            // Stop any existing animation
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);

            currentAnimation = StartCoroutine(AnimateHide());
        }

        public void ShowImmediate()
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);

            gameObject.SetActive(true);
            isVisible = true;
            isAnimating = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.one;
                panelTransform.anchoredPosition = originalPosition;
            }

            if (backgroundBlocker != null)
                backgroundBlocker.SetActive(true);

            OnPanelShown?.Invoke();
        }

        public void HideImmediate()
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);

            isVisible = false;
            isAnimating = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            if (pauseGameWhenOpen)
                Time.timeScale = 1f;

            if (backgroundBlocker != null)
                backgroundBlocker.SetActive(false);

            gameObject.SetActive(false);
            OnPanelHidden?.Invoke();
        }

        #endregion

        #region Animations

        private IEnumerator AnimateShow()
        {
            isAnimating = true;
            float elapsed = 0f;

            // Initialize starting state
            switch (showAnimation)
            {
                case PanelAnimationType.Fade:
                    if (canvasGroup != null) canvasGroup.alpha = 0f;
                    break;
                case PanelAnimationType.Scale:
                    if (panelTransform != null) panelTransform.localScale = Vector3.zero;
                    break;
                case PanelAnimationType.SlideFromLeft:
                    if (panelTransform != null)
                        panelTransform.anchoredPosition = originalPosition + new Vector2(-Screen.width, 0);
                    break;
                case PanelAnimationType.SlideFromRight:
                    if (panelTransform != null)
                        panelTransform.anchoredPosition = originalPosition + new Vector2(Screen.width, 0);
                    break;
                case PanelAnimationType.SlideFromTop:
                    if (panelTransform != null)
                        panelTransform.anchoredPosition = originalPosition + new Vector2(0, Screen.height);
                    break;
                case PanelAnimationType.SlideFromBottom:
                    if (panelTransform != null)
                        panelTransform.anchoredPosition = originalPosition + new Vector2(0, -Screen.height);
                    break;
                case PanelAnimationType.Pop:
                    if (panelTransform != null) panelTransform.localScale = Vector3.zero;
                    break;
            }

            // Animate
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = animationCurve.Evaluate(elapsed / animationDuration);

                ApplyAnimation(showAnimation, t);

                yield return null;
            }

            // Final state
            ApplyAnimation(showAnimation, 1f);
            isAnimating = false;
            isVisible = true;
            OnPanelShown?.Invoke();
        }

        private IEnumerator AnimateHide()
        {
            isAnimating = true;
            float elapsed = 0f;

            // Animate
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = animationCurve.Evaluate(1f - (elapsed / animationDuration));

                ApplyAnimation(hideAnimation, t);

                yield return null;
            }

            // Final state
            ApplyAnimation(hideAnimation, 0f);
            isAnimating = false;
            isVisible = false;

            if (pauseGameWhenOpen)
                Time.timeScale = 1f;

            if (backgroundBlocker != null)
                backgroundBlocker.SetActive(false);

            gameObject.SetActive(false);
            OnPanelHidden?.Invoke();
        }

        private void ApplyAnimation(PanelAnimationType animation, float t)
        {
            switch (animation)
            {
                case PanelAnimationType.Fade:
                    if (canvasGroup != null)
                        canvasGroup.alpha = t;
                    break;

                case PanelAnimationType.Scale:
                    if (panelTransform != null)
                        panelTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                    break;

                case PanelAnimationType.SlideFromLeft:
                case PanelAnimationType.SlideFromRight:
                case PanelAnimationType.SlideFromTop:
                case PanelAnimationType.SlideFromBottom:
                    // Handled in initialization and final state
                    if (t >= 1f && panelTransform != null)
                        panelTransform.anchoredPosition = originalPosition;
                    break;

                case PanelAnimationType.Pop:
                    if (panelTransform != null)
                    {
                        float scaleT = Mathf.Sin(t * Mathf.PI); // Pop effect
                        panelTransform.localScale = Vector3.one * scaleT;
                    }
                    break;
            }
        }

        #endregion

        #region Virtual Methods

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnShown() { }
        protected virtual void OnHidden() { }

        #endregion
    }
}
