using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace MeetingCellsRefactored.UI
{
    /// <summary>
    /// Centralized input manager handling all platform inputs
    /// Bridges Unity Input System with mobile touch inputs
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Input Actions Asset")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Action Map Names")]
        [SerializeField] private string playerActionMap = "Player";
        [SerializeField] private string uiActionMap = "UI";

        [Header("Mobile Joystick References")]
        [SerializeField] private FloatingJoystick leftJoystick;
        [SerializeField] private FloatingJoystick rightJoystick; // Legacy - kept for compatibility

        [Header("Touch Look")]
        [SerializeField] private ScreenTouchLook touchLookArea;
        [SerializeField] private bool useScreenTouchLook = true;

        [Header("Mobile Settings")]
        [SerializeField] private float mobileLookSensitivity = 3f;
        [SerializeField] private bool invertY = false;

        // Input Actions
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction interactAction;
        private InputAction attackAction;
        private InputAction crouchAction;
        private InputAction pauseAction;

        // Events
        public event Action<Vector2> OnMoveInput;
        public event Action<Vector2> OnLookInput;
        public event Action OnJumpPressed;
        public event Action OnSprintPressed;
        public event Action OnSprintReleased;
        public event Action OnInteractPressed;
        public event Action OnAttackPressed;
        public event Action OnCrouchPressed;
        public event Action OnPausePressed;

        // State
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool isMobile;
        private bool isUIActive;
        private bool wasSprinting;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Only call DontDestroyOnLoad if this is a root GameObject
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            SetupInputActions();
            DetectPlatform();
        }

        private void OnEnable()
        {
            EnablePlayerInput();
        }

        private void OnDisable()
        {
            DisableAllInput();
        }

        private void Update()
        {
            if (isUIActive) return;

            // Handle mobile joystick input
            if (isMobile)
            {
                HandleMobileInput();
            }
        }

        private void DetectPlatform()
        {
#if UNITY_ANDROID || UNITY_IOS
            SetMobileMode(true);
#elif UNITY_WEBGL
            SetMobileMode(Application.isMobilePlatform);
#else
            SetMobileMode(false);
#endif
        }

        private void SetupInputActions()
        {
            if (inputActions == null)
            {
                // Try to find InputSystem_Actions first (Unity's default)
                inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");

                // Try to find a default input action asset in Resources
                if (inputActions == null)
                {
                    inputActions = Resources.Load<InputActionAsset>("InputActions/PlayerInputActions");
                }
                if (inputActions == null)
                {
                    inputActions = Resources.Load<InputActionAsset>("PlayerInputActions");
                }

                // Try to find in project
                if (inputActions == null)
                {
                    var guids = UnityEditor.AssetDatabase.FindAssets("t:InputActionAsset");
                    foreach (var guid in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        // Prefer InputSystem_Actions or similar player control assets
                        if (path.Contains("InputSystem_Actions") || path.Contains("Player") || path.Contains("Input"))
                        {
                            inputActions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                            if (inputActions != null)
                            {
                                Debug.Log($"[InputManager] Auto-loaded Input Action Asset: {path}");
                                break;
                            }
                        }
                    }
                }

                if (inputActions == null)
                {
                    Debug.LogError("[InputManager] Input Action Asset not assigned! Please assign InputSystem_Actions in the inspector.");
                    enabled = false;
                    return;
                }
            }

            var playerMap = inputActions.FindActionMap(playerActionMap);
            if (playerMap == null)
            {
                Debug.LogError($"[InputManager] Action map '{playerActionMap}' not found!");
                return;
            }

            moveAction = playerMap.FindAction("Move");
            lookAction = playerMap.FindAction("Look");
            jumpAction = playerMap.FindAction("Jump");
            sprintAction = playerMap.FindAction("Sprint");
            interactAction = playerMap.FindAction("Interact");
            attackAction = playerMap.FindAction("Attack");
            crouchAction = playerMap.FindAction("Crouch");
            pauseAction = playerMap.FindAction("Pause");

            // Subscribe to input events
            if (jumpAction != null)
                jumpAction.performed += ctx => OnJumpPressed?.Invoke();
            if (sprintAction != null)
            {
                sprintAction.performed += ctx => OnSprintPressed?.Invoke();
                sprintAction.canceled += ctx => OnSprintReleased?.Invoke();
            }
            if (interactAction != null)
                interactAction.performed += ctx => OnInteractPressed?.Invoke();
            if (attackAction != null)
                attackAction.performed += ctx => OnAttackPressed?.Invoke();
            if (crouchAction != null)
                crouchAction.performed += ctx => OnCrouchPressed?.Invoke();
            if (pauseAction != null)
                pauseAction.performed += ctx => OnPausePressed?.Invoke();

            // Subscribe to move/look for PC
            if (moveAction != null)
            {
                moveAction.performed += ctx =>
                {
                    if (!isMobile)
                    {
                        moveInput = ctx.ReadValue<Vector2>();
                        OnMoveInput?.Invoke(moveInput);
                    }
                };
                moveAction.canceled += ctx =>
                {
                    if (!isMobile)
                    {
                        moveInput = Vector2.zero;
                        OnMoveInput?.Invoke(moveInput);
                    }
                };
            }

            if (lookAction != null)
            {
                lookAction.performed += ctx =>
                {
                    if (!isMobile)
                    {
                        lookInput = ctx.ReadValue<Vector2>();
                        OnLookInput?.Invoke(lookInput);
                    }
                };
                lookAction.canceled += ctx =>
                {
                    if (!isMobile)
                    {
                        lookInput = Vector2.zero;
                        OnLookInput?.Invoke(lookInput);
                    }
                };
            }
        }

        private void HandleMobileInput()
        {
            // Get left joystick input for movement - process every frame for smooth movement
            if (leftJoystick != null)
            {
                Vector2 newMoveInput = leftJoystick.Direction;
                if (newMoveInput != moveInput)
                {
                    moveInput = newMoveInput;
                    OnMoveInput?.Invoke(moveInput);
                }
            }

            // Get look input from screen touch area (preferred) or right joystick (legacy)
            Vector2 newLookInput = Vector2.zero;

            if (useScreenTouchLook && touchLookArea != null && touchLookArea.IsTouching)
            {
                // Use screen touch drag for look
                newLookInput = touchLookArea.LookInput;
            }
            else if (rightJoystick != null)
            {
                // Fallback to right joystick
                newLookInput = rightJoystick.Direction;
                newLookInput *= mobileLookSensitivity;

                // Invert Y if needed
                if (invertY)
                    newLookInput.y *= -1;
            }

            // Always invoke look input for smooth camera control
            lookInput = newLookInput;
            OnLookInput?.Invoke(lookInput);
        }

        #region Public Methods

        public void EnablePlayerInput()
        {
            inputActions?.FindActionMap(playerActionMap)?.Enable();
            inputActions?.FindActionMap(uiActionMap)?.Disable();
            isUIActive = false;
        }

        public void EnableUIInput()
        {
            inputActions?.FindActionMap(playerActionMap)?.Disable();
            inputActions?.FindActionMap(uiActionMap)?.Enable();
            isUIActive = true;
        }

        public void DisableAllInput()
        {
            inputActions?.Disable();
        }

        public void SetMobileMode(bool mobile)
        {
            isMobile = mobile;

            if (isMobile)
            {
                lookAction?.Disable();
                Debug.Log("[InputManager] Mobile mode enabled");
            }
            else
            {
                lookAction?.Enable();
                Debug.Log("[InputManager] PC mode enabled");
            }
        }

        /// <summary>
        /// Set joystick references (called by GamePlayerUI)
        /// </summary>
        public void SetJoysticks(FloatingJoystick left, FloatingJoystick right)
        {
            leftJoystick = left;
            rightJoystick = right;

            Debug.Log($"[InputManager] Joysticks assigned - Left: {left != null}, Right: {right != null}");
        }

        /// <summary>
        /// Set the screen touch look area for camera control
        /// </summary>
        public void SetTouchLookArea(ScreenTouchLook touchLook)
        {
            touchLookArea = touchLook;
            Debug.Log($"[InputManager] Touch look area assigned: {touchLook != null}");
        }

        // Called by mobile UI buttons
        public void OnMobileJump()
        {
            OnJumpPressed?.Invoke();
        }

        public void OnMobileSprint()
        {
            OnSprintPressed?.Invoke();
        }

        public void OnMobileInteract()
        {
            OnInteractPressed?.Invoke();
        }

        #endregion

        #region Properties

        public Vector2 MoveInput => isMobile ? moveInput : moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        public Vector2 LookInput => isMobile ? lookInput : lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
        public bool IsSprinting => sprintAction?.IsPressed() ?? false;
        public bool IsMobile => isMobile;

        #endregion
    }
}
