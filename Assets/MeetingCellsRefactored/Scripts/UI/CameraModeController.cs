using UnityEngine;
using Unity.Cinemachine;
using System;
using UnityEngine.InputSystem;

namespace MeetingCellsRefactored.UI
{
    /// <summary>
    /// Manages first-person and third-person camera switching
    /// Integrates with Cinemachine for smooth transitions
    /// </summary>
    public class CameraModeController : MonoBehaviour
    {
        public static CameraModeController Instance { get; private set; }

        [Header("Camera References")]
        [SerializeField] private CinemachineCamera firstPersonCamera;
        [SerializeField] private CinemachineCamera thirdPersonCamera;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform cameraFollowTarget;

        [Header("Camera Settings")]
        [SerializeField] private float transitionBlendTime = 0.5f;
        [SerializeField] private float firstPersonFOV = 60f;
        [SerializeField] private float thirdPersonFOV = 50f;
        [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0, 1.6f, -3f);
        [SerializeField] private float thirdPersonDistance = 3f;

        [Header("Input Settings")]
        [SerializeField] private bool allowCameraSwitch = true;
        [SerializeField] private KeyCode switchKey = KeyCode.V;

        public event Action<GamePlayerUI.CameraMode> OnCameraModeChanged;

        private GamePlayerUI.CameraMode currentMode = GamePlayerUI.CameraMode.ThirdPerson;
        private CinemachineBrain cinemachineBrain;
        private bool isTransitioning = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            cinemachineBrain = Camera.main?.GetComponent<CinemachineBrain>();
        }

        private void Start()
        {
            // Subscribe to GamePlayerUI events
            if (GamePlayerUI.Instance != null)
            {
                GamePlayerUI.Instance.OnCameraModeChanged += OnCameraModeChangedFromUI;
            }

            // Initialize camera states
            InitializeCameras();
        }

        private void OnDestroy()
        {
            if (GamePlayerUI.Instance != null)
            {
                GamePlayerUI.Instance.OnCameraModeChanged -= OnCameraModeChangedFromUI;
            }
        }

        private void Update()
        {
            // Keyboard shortcut for camera switch
            if (allowCameraSwitch && Keyboard.current != null && IsKeyPressedThisFrame(switchKey))
            {
                ToggleCameraMode();
            }
        }

        private bool IsKeyPressedThisFrame(KeyCode keyCode)
        {
            var key = GetKeyFromKeyCode(keyCode);
            return key != null && key.wasPressedThisFrame;
        }

        private UnityEngine.InputSystem.Controls.KeyControl GetKeyFromKeyCode(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.V => Keyboard.current.vKey,
                KeyCode.Space => Keyboard.current.spaceKey,
                KeyCode.Return => Keyboard.current.enterKey,
                KeyCode.Escape => Keyboard.current.escapeKey,
                KeyCode.Tab => Keyboard.current.tabKey,
                KeyCode.LeftShift => Keyboard.current.leftShiftKey,
                KeyCode.RightShift => Keyboard.current.rightShiftKey,
                KeyCode.LeftControl => Keyboard.current.leftCtrlKey,
                KeyCode.RightControl => Keyboard.current.rightCtrlKey,
                KeyCode.LeftAlt => Keyboard.current.leftAltKey,
                KeyCode.RightAlt => Keyboard.current.rightAltKey,
                KeyCode.UpArrow => Keyboard.current.upArrowKey,
                KeyCode.DownArrow => Keyboard.current.downArrowKey,
                KeyCode.LeftArrow => Keyboard.current.leftArrowKey,
                KeyCode.RightArrow => Keyboard.current.rightArrowKey,
                KeyCode.A => Keyboard.current.aKey,
                KeyCode.B => Keyboard.current.bKey,
                KeyCode.C => Keyboard.current.cKey,
                KeyCode.D => Keyboard.current.dKey,
                KeyCode.E => Keyboard.current.eKey,
                KeyCode.F => Keyboard.current.fKey,
                KeyCode.G => Keyboard.current.gKey,
                KeyCode.H => Keyboard.current.hKey,
                KeyCode.I => Keyboard.current.iKey,
                KeyCode.J => Keyboard.current.jKey,
                KeyCode.K => Keyboard.current.kKey,
                KeyCode.L => Keyboard.current.lKey,
                KeyCode.M => Keyboard.current.mKey,
                KeyCode.N => Keyboard.current.nKey,
                KeyCode.O => Keyboard.current.oKey,
                KeyCode.P => Keyboard.current.pKey,
                KeyCode.Q => Keyboard.current.qKey,
                KeyCode.R => Keyboard.current.rKey,
                KeyCode.S => Keyboard.current.sKey,
                KeyCode.T => Keyboard.current.tKey,
                KeyCode.U => Keyboard.current.uKey,
                KeyCode.W => Keyboard.current.wKey,
                KeyCode.X => Keyboard.current.xKey,
                KeyCode.Y => Keyboard.current.yKey,
                KeyCode.Z => Keyboard.current.zKey,
                KeyCode.Alpha0 => Keyboard.current.digit0Key,
                KeyCode.Alpha1 => Keyboard.current.digit1Key,
                KeyCode.Alpha2 => Keyboard.current.digit2Key,
                KeyCode.Alpha3 => Keyboard.current.digit3Key,
                KeyCode.Alpha4 => Keyboard.current.digit4Key,
                KeyCode.Alpha5 => Keyboard.current.digit5Key,
                KeyCode.Alpha6 => Keyboard.current.digit6Key,
                KeyCode.Alpha7 => Keyboard.current.digit7Key,
                KeyCode.Alpha8 => Keyboard.current.digit8Key,
                KeyCode.Alpha9 => Keyboard.current.digit9Key,
                KeyCode.F1 => Keyboard.current.f1Key,
                KeyCode.F2 => Keyboard.current.f2Key,
                KeyCode.F3 => Keyboard.current.f3Key,
                KeyCode.F4 => Keyboard.current.f4Key,
                KeyCode.F5 => Keyboard.current.f5Key,
                KeyCode.F6 => Keyboard.current.f6Key,
                KeyCode.F7 => Keyboard.current.f7Key,
                KeyCode.F8 => Keyboard.current.f8Key,
                KeyCode.F9 => Keyboard.current.f9Key,
                KeyCode.F10 => Keyboard.current.f10Key,
                KeyCode.F11 => Keyboard.current.f11Key,
                KeyCode.F12 => Keyboard.current.f12Key,
                _ => null
            };
        }

        private void InitializeCameras()
        {
            if (firstPersonCamera != null)
            {
                firstPersonCamera.Priority = currentMode == GamePlayerUI.CameraMode.FirstPerson ? 10 : 0;
                firstPersonCamera.Lens.FieldOfView = firstPersonFOV;
            }

            if (thirdPersonCamera != null)
            {
                thirdPersonCamera.Priority = currentMode == GamePlayerUI.CameraMode.ThirdPerson ? 10 : 0;
                thirdPersonCamera.Lens.FieldOfView = thirdPersonFOV;

                // Setup third person orbit - using CinemachineFollow component
                var follow = thirdPersonCamera.GetComponent<CinemachineFollow>();
                if (follow != null)
                {
                    follow.FollowOffset = new Vector3(0, 1.5f, -thirdPersonDistance);
                }
            }
        }

        private void OnCameraModeChangedFromUI(GamePlayerUI.CameraMode mode)
        {
            SetCameraMode(mode);
        }

        public void ToggleCameraMode()
        {
            GamePlayerUI.CameraMode newMode = currentMode == GamePlayerUI.CameraMode.FirstPerson
                ? GamePlayerUI.CameraMode.ThirdPerson
                : GamePlayerUI.CameraMode.FirstPerson;

            SetCameraMode(newMode);

            // Update UI to match
            if (GamePlayerUI.Instance != null)
            {
                GamePlayerUI.Instance.SetCameraMode(newMode);
            }
        }

        public void SetCameraMode(GamePlayerUI.CameraMode mode)
        {
            if (currentMode == mode || isTransitioning) return;

            currentMode = mode;

            // Start transition
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(TransitionCamera(mode));
            }
            else
            {
                ApplyCameraMode(mode);
            }

            OnCameraModeChanged?.Invoke(mode);
        }

        private System.Collections.IEnumerator TransitionCamera(GamePlayerUI.CameraMode mode)
        {
            isTransitioning = true;

            ApplyCameraMode(mode);

            // Wait for transition
            yield return new WaitForSeconds(transitionBlendTime);

            isTransitioning = false;
        }

        private void ApplyCameraMode(GamePlayerUI.CameraMode mode)
        {
            if (firstPersonCamera != null)
            {
                firstPersonCamera.Priority = mode == GamePlayerUI.CameraMode.FirstPerson ? 10 : 0;
            }

            if (thirdPersonCamera != null)
            {
                thirdPersonCamera.Priority = mode == GamePlayerUI.CameraMode.ThirdPerson ? 10 : 0;
            }
        }

        #region Properties

        public GamePlayerUI.CameraMode CurrentMode => currentMode;
        public bool IsFirstPerson => currentMode == GamePlayerUI.CameraMode.FirstPerson;
        public bool IsThirdPerson => currentMode == GamePlayerUI.CameraMode.ThirdPerson;
        public bool IsTransitioning => isTransitioning;

        public CinemachineCamera ActiveCamera
        {
            get
            {
                return currentMode == GamePlayerUI.CameraMode.FirstPerson
                    ? firstPersonCamera
                    : thirdPersonCamera;
            }
        }

        #endregion
    }
}
