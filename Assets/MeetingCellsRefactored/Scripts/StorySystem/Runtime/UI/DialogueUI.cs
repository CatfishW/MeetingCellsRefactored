using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StorySystem.Core;
using StorySystem.Nodes;
using StorySystem.Execution;

namespace StorySystem.UI
{
    /// <summary>
    /// UI component for displaying dialogues
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image speakerPortrait;
        [SerializeField] private Image continueIndicator;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private float defaultTextSpeed = 0.03f;
        [SerializeField] private float fadeSpeed = 5f;
        [SerializeField] private bool useTypewriter = true;
        [SerializeField] private AudioSource typingAudioSource;
        [SerializeField] private AudioClip typingSound;

        private StoryPlayer currentPlayer;
        private DialogueNode currentDialogue;
        private Coroutine typewriterCoroutine;
        private bool isTyping;
        private bool skipTyping;

        public bool IsTyping => isTyping;
        public bool IsVisible => dialoguePanel.activeSelf;

        public event Action OnDialogueStart;
        public event Action OnDialogueEnd;
        public event Action OnDialogueContinue;

        private void Start()
        {
            Hide();
            
            // Subscribe to story events
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.OnPlayerCreated += OnPlayerCreated;
                if (StoryManager.Instance.CurrentPlayer != null)
                {
                    SubscribeToPlayer(StoryManager.Instance.CurrentPlayer);
                }
            }
        }

        private void OnDestroy()
        {
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.OnPlayerCreated -= OnPlayerCreated;
            }
            
            UnsubscribeFromPlayer();
        }

        private void OnPlayerCreated(StoryPlayer player)
        {
            SubscribeToPlayer(player);
        }

        private void SubscribeToPlayer(StoryPlayer player)
        {
            UnsubscribeFromPlayer();
            currentPlayer = player;
            player.OnNodeEnter += OnNodeEnter;
            player.OnNodeExit += OnNodeExit;
        }

        private void UnsubscribeFromPlayer()
        {
            if (currentPlayer != null)
            {
                currentPlayer.OnNodeEnter -= OnNodeEnter;
                currentPlayer.OnNodeExit -= OnNodeExit;
                currentPlayer = null;
            }
        }

        private void OnNodeEnter(StoryNode node)
        {
            if (node is DialogueNode dialogue)
            {
                ShowDialogue(dialogue);
            }
        }

        private void OnNodeExit(StoryNode node)
        {
            if (node is DialogueNode)
            {
                // Don't hide immediately - let the next node decide
            }
        }

        public void ShowDialogue(DialogueNode dialogue)
        {
            currentDialogue = dialogue;
            dialoguePanel.SetActive(true);
            OnDialogueStart?.Invoke();

            // Set speaker info
            speakerNameText.text = dialogue.SpeakerName ?? "";
            
            if (speakerPortrait != null)
            {
                if (dialogue.SpeakerPortrait != null)
                {
                    speakerPortrait.sprite = dialogue.SpeakerPortrait;
                    speakerPortrait.gameObject.SetActive(true);
                }
                else
                {
                    speakerPortrait.gameObject.SetActive(false);
                }
            }

            // Get processed text from context or use raw text
            string text = dialogue.DialogueText;
            if (currentPlayer?.Context != null)
            {
                var processedText = currentPlayer.Context.GetTempData<string>("processedDialogueText");
                if (!string.IsNullOrEmpty(processedText))
                {
                    text = processedText;
                }
            }

            // Show text
            if (useTypewriter)
            {
                StartTypewriter(text, dialogue.TextSpeed > 0 ? dialogue.TextSpeed : defaultTextSpeed);
            }
            else
            {
                dialogueText.text = text;
            }

            // Show continue indicator if waiting for input
            if (continueIndicator != null)
            {
                continueIndicator.gameObject.SetActive(dialogue.WaitForInput && !isTyping);
            }
        }

        private void StartTypewriter(string text, float speed)
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }
            typewriterCoroutine = StartCoroutine(TypewriterEffect(text, speed));
        }

        private IEnumerator TypewriterEffect(string text, float speed)
        {
            isTyping = true;
            skipTyping = false;
            dialogueText.text = "";

            if (continueIndicator != null)
            {
                continueIndicator.gameObject.SetActive(false);
            }

            foreach (char c in text)
            {
                if (skipTyping)
                {
                    dialogueText.text = text;
                    break;
                }

                dialogueText.text += c;

                // Play typing sound
                if (typingAudioSource != null && typingSound != null && !char.IsWhiteSpace(c))
                {
                    typingAudioSource.PlayOneShot(typingSound);
                }

                yield return new WaitForSeconds(speed);
            }

            isTyping = false;
            
            if (continueIndicator != null && currentDialogue?.WaitForInput == true)
            {
                continueIndicator.gameObject.SetActive(true);
            }
        }

        public void OnClick()
        {
            if (isTyping)
            {
                // Skip typewriter
                skipTyping = true;
            }
            else if (currentDialogue != null && currentDialogue.WaitForInput)
            {
                OnDialogueContinue?.Invoke();
                currentPlayer?.SendInput();
            }
        }

        public void Hide()
        {
            dialoguePanel.SetActive(false);
            OnDialogueEnd?.Invoke();
        }
    }
}
/* NOTE: This file was truncated in the original conversation and has been manually closed with basic logic. */
