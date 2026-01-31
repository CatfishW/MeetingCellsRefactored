using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.CharacterController;
using UnityEngine.InputSystem;

namespace MeetingCellsRefactored.CharacterController
{
    /// <summary>
    /// Controller that allows smooth switching between First Person and Third Person modes
    /// using Unity's ECS CharacterController
    /// </summary>
    [DisallowMultipleComponent]
    public class HybridPlayerController : MonoBehaviour
    {
        [Header("Camera Mode")]
        [SerializeField] private CameraMode currentMode = CameraMode.ThirdPerson;
        [SerializeField] private KeyCode switchModeKey = KeyCode.V;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("References")]
        [SerializeField] private GameObject firstPersonView;
        [SerializeField] private GameObject thirdPersonCamera;
        [SerializeField] private GameObject playerCharacter;

        [Header("First Person Settings")]
        [SerializeField] private float fpLookSensitivity = 0.2f;

        [Header("Third Person Settings")]
        [SerializeField] private float tpOrbitSpeed = 2f;
        [SerializeField] private float tpMinDistance = 0.5f;
        [SerializeField] private float tpMaxDistance = 10f;

        // Runtime state
        private float transitionProgress = 0f;
        private bool isTransitioning = false;
        private CameraMode targetMode;

        public CameraMode CurrentMode => currentMode;
        public bool IsTransitioning => isTransitioning;

        private void Update()
        {
            // Handle mode switch input
            if (Keyboard.current != null && IsKeyPressedThisFrame(switchModeKey) && !isTransitioning)
            {
                SwitchMode(currentMode == CameraMode.FirstPerson
                    ? CameraMode.ThirdPerson
                    : CameraMode.FirstPerson);
            }

            // Handle transition
            if (isTransitioning)
            {
                UpdateTransition();
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

        public void SwitchMode(CameraMode newMode)
        {
            if (newMode == currentMode || isTransitioning) return;

            targetMode = newMode;
            isTransitioning = true;
            transitionProgress = 0f;

            // Trigger transition effects here
            OnTransitionStarted(currentMode, targetMode);
        }

        private void UpdateTransition()
        {
            transitionProgress += Time.deltaTime / transitionDuration;
            float t = transitionCurve.Evaluate(math.saturate(transitionProgress));

            if (transitionProgress >= 1f)
            {
                CompleteTransition();
            }
            else
            {
                // Interpolate camera properties during transition
                UpdateCameraTransition(t);
            }
        }

        private void UpdateCameraTransition(float t)
        {
            // This will be handled by the ECS systems
            // We can set transition parameters on the camera components
        }

        private void CompleteTransition()
        {
            currentMode = targetMode;
            isTransitioning = false;
            transitionProgress = 0f;

            // Update ECS component data
            UpdateECSMode();

            OnTransitionCompleted(currentMode);
        }

        private void UpdateECSMode()
        {
            // In a real implementation, we'd update the ECS world
            // to switch between FirstPerson and ThirdPerson control systems
            // This would involve:
            // 1. Disabling/enabling the appropriate camera entity
            // 2. Updating input handling
            // 3. Adjusting character control parameters
        }

        private void OnTransitionStarted(CameraMode from, CameraMode to)
        {
            Debug.Log($"Switching camera mode from {from} to {to}");
            // Could trigger events, animations, UI updates here
        }

        private void OnTransitionCompleted(CameraMode mode)
        {
            Debug.Log($"Camera mode switched to {mode}");
        }

        /// <summary>
        /// Call this from the StorySystem to disable player control during cutscenes
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            // Update ECS input systems
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                // Toggle input systems based on enabled state
            }
        }
    }
}
