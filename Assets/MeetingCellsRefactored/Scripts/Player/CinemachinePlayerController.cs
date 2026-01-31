using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using MeetingCellsRefactored.UI;

namespace MeetingCellsRefactored.Player
{
    /// <summary>
    /// Enhanced player controller using Cinemachine for camera management
    /// Supports smooth switching between First and Third person modes
    /// </summary>
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public class CinemachinePlayerController : MonoBehaviour
    {
        [Header("Camera Mode")]
        [SerializeField] private CameraMode currentMode = CameraMode.ThirdPerson;
        [SerializeField] private KeyCode switchModeKey = KeyCode.V;
        [SerializeField] private bool startInFirstPerson = false;
        [SerializeField] private bool lockCursorOnStart = true;

        [Header("Cinemachine Cameras")]
        [SerializeField] private CinemachineCamera firstPersonCamera;
        [SerializeField] private CinemachineCamera thirdPersonCamera;
        [SerializeField] private Transform cameraTarget;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private bool useControllerGrounded = true;

        [Header("First Person Settings")]
        [SerializeField] private float fpLookSensitivity = 0.5f;
        [SerializeField] private float fpPitchClamp = 80f;
        [SerializeField] private Transform fpCameraRoot;

        [Header("Third Person Settings")]
        [SerializeField] private float tpOrbitSpeed = 2f;
        [SerializeField] private float tpMinDistance = 1f;
        [SerializeField] private float tpMaxDistance = 10f;
        [SerializeField] private float tpCurrentDistance = 5f;
        [SerializeField] private Vector2 tpPitchLimits = new Vector2(-30f, 60f);
        [SerializeField] private float tpZoomSpeed = 2f;
        [SerializeField] private bool allowCameraOrbitWhenUnlocked = true; // GTA5 style

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string isGroundedParameter = "Grounded";
        [SerializeField] private string isSprintingParameter = "IsSprinting";
        [SerializeField] private string jumpTriggerParameter = "Jump";
        [SerializeField] private string isWalkingParameter = "IsWalking";
        [SerializeField] private string isFallingParameter = "IsFalling";
        [SerializeField] private string verticalVelocityParameter = "VerticalVelocity";
        [SerializeField] private string heightParameter = "height";
        [SerializeField] private float animationSpeedMultiplier = 1f;
        [SerializeField] private bool debugAnimation = false;

        // Cached Animator property hashes for performance
        private int speedHash;
        private int isGroundedHash;
        private int isSprintingHash;
        private int jumpHash;
        private int isWalkingHash;
        private int isFallingHash;
        private int heightHash;
        private bool animatorHashesInitialized;

        // Components
        private UnityEngine.CharacterController controller;
        private Transform playerModel;

        // State
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool isSprinting;
        private bool isGrounded;
        private bool wasGrounded;
        private bool jumpPressed;
        private Vector3 velocity;
        private float xRotation = 0f;
        private float yRotation = 0f;

        // First person rotation (separate from model)
        private float fpYaw = 0f;

        // Transition
        private bool isTransitioning = false;
        private float transitionProgress = 0f;
        private CameraMode targetMode;

        // Story system integration
        public bool InputEnabled { get; set; } = true;
        public CameraMode CurrentMode => currentMode;
        public bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        private void Awake()
        {
            controller = GetComponent<UnityEngine.CharacterController>();

            // Find PlayerModel first (before finding animator)
            playerModel = transform.Find("PlayerModel");
            if (playerModel == null)
            {
                // Try to find a child that might be the visual model
                foreach (Transform child in transform)
                {
                    if (child.name.Contains("Model") || child.name.Contains("Body") || child.name.Contains("Mesh"))
                    {
                        playerModel = child;
                        break;
                    }
                }
            }

            // Find animator - prefer the PlayerModel's animator if available
            if (animator == null)
            {
                if (playerModel != null)
                {
                    animator = playerModel.GetComponent<Animator>();
                }
                
                if (animator == null)
                {
                    animator = GetComponent<Animator>();
                }
                
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }

            // Initialize animator parameter hashes
            InitializeAnimatorHashes();

            // Auto-create ground check
            if (groundCheck == null)
            {
                var groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -0.9f, 0);
                groundCheck = groundCheckObj.transform;
            }

            // Setup camera target
            if (cameraTarget == null)
                cameraTarget = transform;

            // Setup first person root
            if (fpCameraRoot == null)
            {
                fpCameraRoot = new GameObject("FPCameraRoot").transform;
                fpCameraRoot.SetParent(transform);
                fpCameraRoot.localPosition = new Vector3(0, 1.6f, 0); // Eye level
            }
        }

        private void Start()
        {
            InitializeCameras();
            SetCameraMode(startInFirstPerson ? CameraMode.FirstPerson : currentMode);

            // Initialize rotation
            fpYaw = transform.eulerAngles.y;

            // Lock cursor for gameplay
            if (lockCursorOnStart)
                LockCursor();

            // Subscribe to InputManager events for mobile support
            SubscribeToInputManager();

            // adjust camera based on player scale
            UpdateCameraOrbitsForScale();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInputManager();
        }

        private void SubscribeToInputManager()
        {
            if (InputManager.Instance == null) return;

            // Only subscribe to one-shot events
            // Continuous inputs (Move, Look, Sprint) are polled in HandleInput
            InputManager.Instance.OnJumpPressed += HandleJumpInput;
        }

        private void UnsubscribeFromInputManager()
        {
            if (InputManager.Instance == null) return;

            InputManager.Instance.OnJumpPressed -= HandleJumpInput;
        }


        private void HandleJumpInput()
        {
            if (isGrounded)
            {
                jumpPressed = true;
            }
        }

        private void HandleCameraLookWhenUnlocked()
        {
            // Handle camera look input separately from movement input
            // This allows camera orbiting even when cursor is not locked (GTA5 style)
            if (currentMode == CameraMode.ThirdPerson && allowCameraOrbitWhenUnlocked)
            {
                // Read mouse delta for camera look even when not locked
                Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.1f;
                lookInput = mouseDelta;

                // Directly update orbit when unlocked
                UpdateThirdPersonCameraOrbit();

                // Also handle zoom
                HandleThirdPersonZoom();
            }
        }

        private void HandleThirdPersonZoom()
        {
            // Handle zoom with mouse scroll wheel
            float scrollInput = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollInput) > 0.01f && thirdPersonCamera != null)
            {
                var orbiter = thirdPersonCamera.GetComponent<CinemachineOrbitalFollow>();
                if (orbiter != null)
                {
                    // Adjust radial axis (distance) based on scroll input
                    var rAxis = orbiter.RadialAxis;
                    float newDistance = rAxis.Value - scrollInput * tpZoomSpeed * 0.01f;
                    rAxis.Value = Mathf.Clamp(newDistance, tpMinDistance, tpMaxDistance);
                    orbiter.RadialAxis = rAxis;
                    tpCurrentDistance = rAxis.Value;
                }
            }
        }

        private void OnEnable()
        {
            // Ensure ground check is reset when re-enabled
            if (groundCheck == null)
            {
                var groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -0.9f, 0);
                groundCheck = groundCheckObj.transform;
            }
        }

        private void InitializeAnimatorHashes()
        {
            if (animatorHashesInitialized) return;

            speedHash = Animator.StringToHash(speedParameter);
            isGroundedHash = Animator.StringToHash(isGroundedParameter);
            isSprintingHash = Animator.StringToHash(isSprintingParameter);
            jumpHash = Animator.StringToHash(jumpTriggerParameter);
            isWalkingHash = Animator.StringToHash(isWalkingParameter);
            isFallingHash = Animator.StringToHash(isFallingParameter);
            heightHash = Animator.StringToHash(heightParameter);
            
            // Fixed: Added VerticalVelocity hash
            int verticalVelocityHash = Animator.StringToHash(verticalVelocityParameter);

            animatorHashesInitialized = true;
        }

        private void InitializeCameras()
        {
            // Create cameras if not assigned
            if (firstPersonCamera == null)
                firstPersonCamera = CreateFirstPersonCamera();

            if (thirdPersonCamera == null)
                thirdPersonCamera = CreateThirdPersonCamera();

            // Setup camera targets
            if (thirdPersonCamera != null)
            {
                thirdPersonCamera.Follow = cameraTarget;
                // For OrbitalFollow, the component handles its own orientation based on axes
            }

            if (firstPersonCamera != null && fpCameraRoot != null)
            {
                // FP camera is usually parented to root, but let's ensure it has correct targets
                firstPersonCamera.transform.SetParent(fpCameraRoot);
                firstPersonCamera.transform.localPosition = Vector3.zero;
                firstPersonCamera.transform.localRotation = Quaternion.identity;
            }

            // Initialize priorities
            if (firstPersonCamera != null)
                firstPersonCamera.Priority = currentMode == CameraMode.FirstPerson ? 15 : 5;
            if (thirdPersonCamera != null)
                thirdPersonCamera.Priority = currentMode == CameraMode.ThirdPerson ? 15 : 5;
        }

        private CinemachineCamera CreateFirstPersonCamera()
        {
            var camObj = new GameObject("FirstPersonCamera");
            camObj.transform.SetParent(fpCameraRoot);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;

            var cam = camObj.AddComponent<CinemachineCamera>();
            cam.Lens.FieldOfView = 60f;
            cam.Priority = 0;

            // Add input handler
            var inputProvider = camObj.AddComponent<CinemachineInputAxisController>();

            return cam;
        }

        private CinemachineCamera CreateThirdPersonCamera()
        {
            var camObj = new GameObject("ThirdPersonCamera");

            var cam = camObj.AddComponent<CinemachineCamera>();
            cam.Lens.FieldOfView = 60f;
            cam.Priority = 10;

            // Create follow target
            var followTarget = new GameObject("TPCameraFollow").transform;
            followTarget.SetParent(transform);
            followTarget.localPosition = new Vector3(0, 1.5f, 0); // Over shoulder

            cam.Target.TrackingTarget = followTarget;
            cam.Target.CustomLookAtTarget = followTarget;

            // Add orbiting
            var orbiter = camObj.AddComponent<CinemachineOrbitalFollow>();
            orbiter.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing;
            
            // GTA Style: Disable damping for snappy response
            orbiter.TrackerSettings.PositionDamping = Vector3.zero;
            orbiter.TrackerSettings.RotationDamping = Vector3.zero;
            
            // Disable auto-recentering
            // Disable auto-recentering and set range/wrap
            // Note: In Cinemachine 3.x, Axis properties return structs, so we must fetch-modify-set
            var hAxis = orbiter.HorizontalAxis;
            hAxis.Recentering.Enabled = false;
            hAxis.Value = 0f;
            hAxis.Range = new Vector2(-180, 180);
            hAxis.Wrap = true;
            orbiter.HorizontalAxis = hAxis;
            
            var vAxis = orbiter.VerticalAxis;
            vAxis.Value = 20f;
            vAxis.Range = new Vector2(-80, 80); // Clamp vertical
            vAxis.Wrap = false;
            vAxis.Recentering.Enabled = false;
            orbiter.VerticalAxis = vAxis;
            
            var rAxis = orbiter.RadialAxis;
            rAxis.Value = tpCurrentDistance;
            rAxis.Range = new Vector2(tpMinDistance, tpMaxDistance);
            orbiter.RadialAxis = rAxis;

            return cam;
        }

        private void Update()
        {
            // Handle cursor locking first
            HandleCursorInput();

            if (!InputEnabled)
            {
                ApplyGravity();
                return;
            }

            // Check if we're on mobile
            bool isMobile = InputManager.Instance != null && InputManager.Instance.IsMobile;

            // Process input when cursor is locked (PC) or always on mobile
            if (isMobile || IsCursorLocked)
            {
                HandleInput();
            }
            else
            {
                // When cursor is unlocked on PC, only handle certain inputs
                moveInput = Vector2.zero;
                isSprinting = false;
                lookInput = Vector2.zero;

                // For third person, allow camera orbit even when cursor unlocked (GTA5 style)
                if (currentMode == CameraMode.ThirdPerson && allowCameraOrbitWhenUnlocked)
                {
                    HandleCameraLookWhenUnlocked();
                }
            }

            HandleGroundCheck();

            if (isTransitioning)
            {
                UpdateTransition();
                ApplyGravity();
                UpdateAnimator();
                return;
            }

            HandleMovement();
            HandleRotation();
            HandleCameraLook();
            ApplyGravity();
            HandleJump();
            UpdateAnimator();

            wasGrounded = isGrounded;
        }

        private void HandleCursorInput()
        {
            // Skip cursor handling on mobile
            bool isMobile = InputManager.Instance != null && InputManager.Instance.IsMobile;
            if (isMobile)
            {
                // Always ensure cursor is visible on mobile
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            // Press Escape to unlock cursor
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
            }

            // Click to lock cursor
            if (Mouse.current.leftButton.wasPressedThisFrame && !IsCursorLocked)
            {
                LockCursor();
            }
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void UpdateTransition()
        {
            // Camera blend is handled by Cinemachine during transition
            // This method is called during camera mode transitions
        }

        private void HandleInput()
        {
            // Unified Input Handling via InputManager
            if (InputManager.Instance != null)
            {
                // Poll continuous inputs
                moveInput = InputManager.Instance.MoveInput;
                lookInput = InputManager.Instance.LookInput;
                isSprinting = InputManager.Instance.IsSprinting;

                // Apply generic mouse scaling for PC if not mobile
                // This preserves the previous feel of 0.1f scale on Mouse Delta
                if (!InputManager.Instance.IsMobile)
                {
                    lookInput *= 0.1f;
                }

                // Camera mode switch (V key) - Keep localized for now
                if (Keyboard.current != null && Keyboard.current[Key.V].wasPressedThisFrame && !isTransitioning)
                {
                    SwitchMode(currentMode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson);
                }

                // Mouse/Touch Zoom
                if (currentMode == CameraMode.ThirdPerson)
                {
                    HandleThirdPersonZoom();
                }
            }
            else
            {
                // Fallback: Direct Hardware Input (Legacy)
                // Only runs if InputManager is missing
                
                // Movement
                if (Keyboard.current != null)
                {
                    moveInput = new Vector2(
                        (Keyboard.current.dKey.isPressed ? 1f : 0f) + (Keyboard.current.aKey.isPressed ? -1f : 0f),
                        (Keyboard.current.wKey.isPressed ? 1f : 0f) + (Keyboard.current.sKey.isPressed ? -1f : 0f)
                    );
                    if (moveInput.magnitude > 1f) moveInput.Normalize();
                    
                    isSprinting = Keyboard.current.leftShiftKey.isPressed;
                    
                    if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
                        jumpPressed = true;
                        
                    if (Keyboard.current[Key.V].wasPressedThisFrame && !isTransitioning)
                        SwitchMode(currentMode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson);
                }

                if (Mouse.current != null)
                {
                    lookInput = Mouse.current.delta.ReadValue() * 0.1f;
                    
                    if (currentMode == CameraMode.ThirdPerson)
                        HandleThirdPersonZoom();
                }
            }
        }

        private void HandleGroundCheck()
        {
            // Multiple ground check methods for reliability
            bool sphereCheck = false;

            if (groundCheck != null)
            {
                sphereCheck = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            }

            bool controllerGrounded = controller.isGrounded;

            // Use both methods for reliability
            if (useControllerGrounded)
            {
                isGrounded = controllerGrounded || sphereCheck;
            }
            else
            {
                isGrounded = sphereCheck;
            }

            // Reset vertical velocity when grounded
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -0.5f; // Small negative value to keep grounded
            }

            // Debug ground check
            // Debug.Log($"Grounded: {isGrounded} (Controller: {controllerGrounded}, Sphere: {sphereCheck})");
        }

        private void HandleMovement()
        {
            float speed = isSprinting ? sprintSpeed : walkSpeed;

            if (moveInput.magnitude > 0.1f)
            {
                Vector3 moveDirection;

                // Get camera transform for direction calculation
                Transform camTransform = Camera.main != null ? Camera.main.transform : transform;

                if (currentMode == CameraMode.FirstPerson || isTransitioning)
                {
                    // First person: move relative to camera forward
                    Vector3 camForward = Vector3.Scale(camTransform.forward, new Vector3(1, 0, 1)).normalized;
                    Vector3 camRight = Vector3.Scale(camTransform.right, new Vector3(1, 0, 1)).normalized;
                    moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
                }
                else
                {
                    // Third person: move relative to camera forward
                    Vector3 camForward = Vector3.Scale(camTransform.forward, new Vector3(1, 0, 1)).normalized;
                    Vector3 camRight = Vector3.Scale(camTransform.right, new Vector3(1, 0, 1)).normalized;
                    moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
                }

                controller.Move(moveDirection * speed * Time.deltaTime);
            }
        }

        private void HandleRotation()
        {
            if (currentMode == CameraMode.FirstPerson)
            {
                // First Person: Mouse rotates the CAMERA view, not the player model
                // The player model stays facing forward (or the direction set by third person)
                // Only rotate the player transform based on mouse X input
                float yRot = lookInput.x * fpLookSensitivity;
                fpYaw += yRot;

                // Apply rotation to player transform
                transform.rotation = Quaternion.Euler(0f, fpYaw, 0f);

                // In first person, hide the player model if it's visible
                // (handled by CameraLayerController culling mask)
            }
            else if (currentMode == CameraMode.ThirdPerson)
            {
                // Third Person: GTA5-style controls
                // Camera orbits freely, player rotates to face movement direction relative to camera

                // First, update the orbit camera based on mouse input (always, even when not moving)
                UpdateThirdPersonCameraOrbit();

                // Then, handle player rotation based on movement input
                if (moveInput.magnitude > 0.1f)
                {
                    Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
                    Vector3 camForward = Vector3.Scale(camTransform.forward, new Vector3(1, 0, 1)).normalized;
                    Vector3 camRight = Vector3.Scale(camTransform.right, new Vector3(1, 0, 1)).normalized;
                    Vector3 moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    
                    // Fixed: Rotate the PlayerModel CHILD instead of the root transform
                    // This prevents the camera (a sibling/child of root) from inheriting the movement rotation
                    if (playerModel != null)
                    {
                        playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    }
                    else
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    }

                    // Update fpYaw to match so camera doesn't jump when switching
                    fpYaw = transform.eulerAngles.y;
                }
            }
        }

        private void UpdateThirdPersonCameraOrbit()
        {
            if (thirdPersonCamera != null)
            {
                // On PC, lookInput is already Mouse Delta * 0.1f.
                // We apply this to the axis.
                
                if (lookInput.sqrMagnitude > 0.000001f)
                {
                    var orbiter = thirdPersonCamera.GetComponent<CinemachineOrbitalFollow>();
                    if (orbiter != null)
                    {
                        // Sensitivity multiplier
                        float sensitivity = tpOrbitSpeed; 
                        
                        // We accumulate the value manually since we disabled auto-input
                        // Multiply by a scalar to make it feel right (Mouse Delta is usually small)
                        // Removing Time.deltaTime because Mouse Delta is already per-frame displacement
                        
                        var hAxis = orbiter.HorizontalAxis;
                        hAxis.Value += lookInput.x * sensitivity * 2.0f;
                        // Wrap is handled by the axis configuration if Wrap=true
                        orbiter.HorizontalAxis = hAxis;

                        var vAxis = orbiter.VerticalAxis;
                        vAxis.Value -= lookInput.y * sensitivity * 2.0f;
                        vAxis.Value = Mathf.Clamp(vAxis.Value, tpPitchLimits.x, tpPitchLimits.y);
                        orbiter.VerticalAxis = vAxis;

                        // Update stored distance
                        tpCurrentDistance = orbiter.RadialAxis.Value;
                    }
                }
            }
        }

        private void UpdateCameraOrbitsForScale()
        {
            if (thirdPersonCamera == null) return;

            var orbiter = thirdPersonCamera.GetComponent<CinemachineOrbitalFollow>();
            if (orbiter != null)
            {
                // Calculate scale factor based on player height (assuming 2m is standard height)
                // Use lossyScale.y to get true world scale
                float scaleFactor = transform.lossyScale.y;
                
                // If scale is close to 1, keep defaults
                if (Mathf.Abs(scaleFactor - 1f) < 0.1f) return;

                Debug.Log($"[CinemachinePlayerController] Adjusting camera orbits for scale: {scaleFactor}");

                // Scale orbits - GTA Style (Consistent Radii)
                // Instead of hourglass shape (varying radii), use consistent radius for movement
                // This creates a "Sphere/Cylinder" feel rather than an "Orbit" feel
                float baseRadius = 4f * scaleFactor;
                
                var orbits = orbiter.Orbits;
                orbits.Top.Radius = baseRadius;
                orbits.Top.Height = 4f * scaleFactor; // Standard height scaling

                orbits.Center.Radius = baseRadius;
                orbits.Center.Height = 1.6f * scaleFactor; // Shoulder height

                orbits.Bottom.Radius = baseRadius;
                orbits.Bottom.Height = 0.5f * scaleFactor;
                orbiter.Orbits = orbits;

                // Adjust zoom limits based on scale
                tpMinDistance = 2f * scaleFactor; // Don't get too close
                tpMaxDistance = 10f * scaleFactor;
                tpCurrentDistance = 5f * scaleFactor; // Default zoom
                
                var rAxis = orbiter.RadialAxis;
                rAxis.Value = tpCurrentDistance;
                // Also update range if dynamic scaling changed limits
                rAxis.Range = new Vector2(tpMinDistance, tpMaxDistance); 
                orbiter.RadialAxis = rAxis;
            }
        }

        private void HandleCameraLook()
        {
            if (currentMode == CameraMode.FirstPerson)
            {
                // Handle first person look - pitch only (up/down)
                xRotation -= lookInput.y * fpLookSensitivity;
                xRotation = Mathf.Clamp(xRotation, -fpPitchClamp, fpPitchClamp);

                if (firstPersonCamera != null)
                {
                    firstPersonCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                }
            }
            else
            {
                // Call the third person orbit update
                UpdateThirdPersonCameraOrbit();
            }
        }

        private void ApplyGravity()
        {
            // Don't accumulate gravity when grounded
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -0.5f;
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }

            controller.Move(velocity * Time.deltaTime);
        }

        private void HandleJump()
        {
            if (jumpPressed && isGrounded)
            {
                // Calculate jump velocity: v = sqrt(h * -2 * g)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpPressed = false;

                // Trigger jump animation immediately
                if (animator != null)
                {
                    // Force grounded false in animator immediately
                    animator.SetBool(isGroundedHash, false);
                    animator.SetBool("IsGrounded", false);
                    animator.SetBool("Grounded", false);

                    // Use CrossFade for immediate transition, bypassing potential exit times/delays
                    // Using a very short transition time (0.05s) to blend slightly but start almost instantly
                    // Try "Jump" state first, if not found, rely on generic Trigger
                    int jumpState = Animator.StringToHash("Jump");
                    if (HasState(animator, jumpState))
                    {
                        animator.CrossFade(jumpState, 0.05f, 0, 0f);
                    }
                    else
                    {
                        animator.SetTrigger(jumpHash);
                        animator.SetTrigger("Jump");
                    }
                }

                // Debug
                // Debug.Log($"Jump! Velocity: {velocity.y}");
            }
            else if (jumpPressed && !isGrounded)
            {
                jumpPressed = false;
            }
            // Note: We don't reset velocity here if failing to jump, just the flag
        }

        private bool HasState(Animator animator, int stateHash)
        {
            // Check if animator has a state with the given hash
            // This is done by trying to get the current state info
            try
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                return true; // If we can get state info, assume the hash might be valid
            }
            catch
            {
                return false;
            }
        }

        private System.Collections.IEnumerator DelayedJumpTriggerReset()
        {
            // Wait for the jump animation to start
            yield return new WaitForSeconds(0.1f);
            if (animator != null)
            {
                animator.ResetTrigger(jumpTriggerParameter);
            }
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;

            // Apply global animation speed multiplier
            animator.speed = animationSpeedMultiplier;

            // Calculate normalized speed for animator (0 = idle, 0.5 = walk, 1 = sprint)
            float inputMagnitude = moveInput.magnitude;
            bool isWalking = inputMagnitude > 0.1f;
            float targetSpeed = 0f;
            if (isWalking)
            {
                targetSpeed = isSprinting ? 1f : 0.5f;
            }

            // Get current speed from animator for smooth interpolation
            float currentSpeed = animator.GetFloat(speedHash);

            // Smoothing for animation transitions (slowed down from 10f to 6f for better fluidity)
            float smoothedSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 6f);

            // Detect falling state
            bool isFalling = !isGrounded && velocity.y < -0.5f;

            // Update animator parameters
            // Using 0.1s dampTime creates much more natural transitions than immediate setting
            animator.SetFloat(speedHash, smoothedSpeed, 0.1f, Time.deltaTime);
            animator.SetBool(isGroundedHash, isGrounded);
            
            // Set additional parameters if they exist in the animator
            // We set these directly to avoid hash-mapping issues with different animator controllers
            animator.SetBool("IsWalking", isWalking);
            animator.SetBool("IsSprinting", isSprinting);
            animator.SetBool("IsFalling", isFalling);
            animator.SetBool("Grounded", isGrounded); // Fallback for controllers using 'Grounded' instead of 'IsGrounded'
            animator.SetFloat("VerticalVelocity", velocity.y);

            // Set height parameter for custom controller (0 = normal, 1 = crouching)
            // Try to get and set if possible
            try {
                float currentHeight = animator.GetFloat(heightHash);
                float targetHeight = 0f; // Normal height
                float smoothedHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * 5f);
                animator.SetFloat(heightHash, smoothedHeight);
            } catch { /* Parameter might not exist */ }

            // Debug animation state
            if (debugAnimation)
            {
                Debug.Log($"[Animator] Speed: {smoothedSpeed:F2}, Walking: {isWalking}, Grounded: {isGrounded}, Falling: {isFalling}, VelocityY: {velocity.y:F2}");
            }
        }

        private void ResetJumpTrigger()
        {
            if (animator != null)
            {
                animator.ResetTrigger(jumpHash);
            }
        }

        public void SwitchMode(CameraMode newMode)
        {
            if (newMode == currentMode || isTransitioning) return;

            targetMode = newMode;
            isTransitioning = true;
            transitionProgress = 0f;

            // Start transition
            StartCoroutine(TransitionCamera());
        }

        private System.Collections.IEnumerator TransitionCamera()
        {
            // Switch Cinemachine priorities
            if (firstPersonCamera != null)
                firstPersonCamera.Priority = targetMode == CameraMode.FirstPerson ? 20 : 0;

            if (thirdPersonCamera != null)
                thirdPersonCamera.Priority = targetMode == CameraMode.ThirdPerson ? 20 : 0;

            while (transitionProgress < 1f)
            {
                transitionProgress += Time.deltaTime / transitionDuration;
                float t = transitionCurve.Evaluate(Mathf.Clamp01(transitionProgress));

                yield return null;
            }

            currentMode = targetMode;
            isTransitioning = false;

            // Reset camera pitch for first person
            if (currentMode == CameraMode.FirstPerson)
            {
                xRotation = 0f;
            }

            // Refresh camera culling mask
            RefreshCameraCullingMask();
        }

        private void RefreshCameraCullingMask()
        {
            // Find the main camera's CameraLayerController and refresh it
            if (Camera.main != null)
            {
                var layerController = Camera.main.GetComponent<CameraLayerController>();
                if (layerController != null)
                {
                    layerController.RefreshCullingMask();
                }
            }
        }

        public void SetCameraMode(CameraMode mode)
        {
            currentMode = mode;

            if (firstPersonCamera != null)
                firstPersonCamera.Priority = mode == CameraMode.FirstPerson ? 20 : 0;

            if (thirdPersonCamera != null)
                thirdPersonCamera.Priority = mode == CameraMode.ThirdPerson ? 20 : 0;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            }
        }
    }

    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson
    }
}
