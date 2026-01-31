using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace MeetingCellsRefactored.Editor
{
    public class CreatePlayerAnimatorEditor
    {
        [MenuItem("Tools/Meeting Cells/Create Player Animator Controller")]
        public static void CreatePlayerAnimator()
        {
            string path = "Assets/MeetingCellsRefactored/Animations/PlayerAnimatorController.controller";

            // Create the controller using AssetDatabase
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // Add parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);

            // Get the state machine from the base layer
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // Find animation clips
            AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/MeetingCellsRefactored/Animations/IDLE.anim");
            AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/MeetingCellsRefactored/Animations/RBC_Walk.anim");
            AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/MeetingCellsRefactored/Animations/Run.anim");
            AnimationClip jumpClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/MeetingCellsRefactored/Animations/JUMP.anim");

            // If clips not found, try alternatives
            if (idleClip == null)
                idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/MeetingCellsRefactored/Animations/IDLE_ANIM.anim");
            if (idleClip == null)
                idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/MeetingCellsRefactored/Animations/Updated_Idle.anim");

            if (walkClip == null)
                walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/MeetingCellsRefactored/Animations/WALK_ANIM.anim");
            if (walkClip == null)
                walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/MeetingCellsRefactored/Animations/Updated Walk.anim");

            // Create states
            AnimatorState idleState = rootStateMachine.AddState("Idle");
            if (idleClip != null) idleState.motion = idleClip;
            idleState.writeDefaultValues = false;

            AnimatorState walkState = rootStateMachine.AddState("Walk");
            if (walkClip != null) walkState.motion = walkClip;
            walkState.writeDefaultValues = false;

            AnimatorState runState = rootStateMachine.AddState("Run");
            if (runClip != null) runState.motion = runClip;
            runState.writeDefaultValues = false;

            AnimatorState jumpState = rootStateMachine.AddState("Jump");
            if (jumpClip != null) jumpState.motion = jumpClip;
            jumpState.writeDefaultValues = false;

            AnimatorState fallState = rootStateMachine.AddState("Fall");
            fallState.writeDefaultValues = false;

            // Set default state
            rootStateMachine.defaultState = idleState;

            // Create transitions

            // Idle -> Walk (Speed > 0.1)
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.exitTime = 0;
            idleToWalk.duration = 0.1f;

            // Walk -> Idle (Speed < 0.1)
            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.exitTime = 0;
            walkToIdle.duration = 0.1f;

            // Walk -> Run (IsSprinting = true)
            AnimatorStateTransition walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.If, 0, "IsSprinting");
            walkToRun.hasExitTime = false;
            walkToRun.exitTime = 0;
            walkToRun.duration = 0.1f;

            // Run -> Walk (IsSprinting = false)
            AnimatorStateTransition runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSprinting");
            runToWalk.hasExitTime = false;
            runToWalk.exitTime = 0;
            runToWalk.duration = 0.1f;

            // Any -> Jump (Jump trigger)
            AnimatorStateTransition anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
            anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
            anyToJump.hasExitTime = false;
            anyToJump.exitTime = 0;
            anyToJump.duration = 0.1f;

            // Jump -> Idle (IsGrounded = true)
            AnimatorStateTransition jumpToIdle = jumpState.AddTransition(idleState);
            jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            jumpToIdle.hasExitTime = true;
            jumpToIdle.exitTime = 0.9f;
            jumpToIdle.duration = 0.2f;

            // Any -> Fall (IsGrounded = false and VerticalVelocity < 0)
            AnimatorStateTransition anyToFall = rootStateMachine.AddAnyStateTransition(fallState);
            anyToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            anyToFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VerticalVelocity");
            anyToFall.hasExitTime = false;
            anyToFall.exitTime = 0;
            anyToFall.duration = 0.1f;

            // Fall -> Idle (IsGrounded = true)
            AnimatorStateTransition fallToIdle = fallState.AddTransition(idleState);
            fallToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            fallToIdle.hasExitTime = false;
            fallToIdle.exitTime = 0;
            fallToIdle.duration = 0.1f;

            // Save the controller
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log($"Player Animator Controller created at: {path}");
            Selection.activeObject = controller;
        }
    }
}
