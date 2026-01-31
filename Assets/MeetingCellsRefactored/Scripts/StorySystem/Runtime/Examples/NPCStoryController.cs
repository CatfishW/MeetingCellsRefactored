using UnityEngine;
using StorySystem.Execution;
using StorySystem.Core;
using UnityEngine.InputSystem;

namespace StorySystem.Runtime.Examples
{
    /// <summary>
    /// Example controller for NPCs that can trigger story conversations
    /// Attach this to NPC GameObjects
    /// </summary>
    public class NPCStoryController : MonoBehaviour
    {
        [Header("Story")]
        [SerializeField] private StoryGraph npcStory;
        [SerializeField] private string conversationStartNodeId;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private bool showInteractionPrompt = true;

        [Header("Events")]
        [SerializeField] private bool triggerOnApproach = false;
        [SerializeField] private bool playOnce = true;

        private bool hasPlayed;
        private bool playerInRange;
        private Transform playerTransform;

        private void Start()
        {
            // Find player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null || npcStory == null)
                return;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            playerInRange = distance <= interactionRange;

            if (playerInRange)
            {
                if (triggerOnApproach && !hasPlayed)
                {
                    StartConversation();
                }
                else if (Keyboard.current != null && IsKeyPressedThisFrame(interactionKey))
                {
                    StartConversation();
                }
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

        public void StartConversation()
        {
            if (playOnce && hasPlayed)
                return;

            if (StoryManager.Instance == null)
            {
                Debug.LogWarning("[NPCStoryController] StoryManager not found!");
                return;
            }

            StoryPlayer player;
            if (!string.IsNullOrEmpty(conversationStartNodeId))
            {
                player = StoryManager.Instance.PlayStory(npcStory, conversationStartNodeId);
            }
            else
            {
                player = StoryManager.Instance.PlayStory(npcStory);
            }

            if (player != null)
            {
                hasPlayed = true;

                // Optional: Face the player
                FacePlayer();

                Debug.Log($"[NPCStoryController] Started conversation with {gameObject.name}");
            }
        }

        private void FacePlayer()
        {
            if (playerTransform != null)
            {
                Vector3 direction = playerTransform.position - transform.position;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            // Draw label
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"NPC: {gameObject.name}");
#endif
        }
    }
}
