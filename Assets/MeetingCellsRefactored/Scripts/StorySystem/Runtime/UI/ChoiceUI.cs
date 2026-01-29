using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StorySystem.Core;
using StorySystem.Execution;
using StorySystem.Nodes;

namespace StorySystem.UI
{
    /// <summary>
    /// UI component for displaying choice prompts and options
    /// </summary>
    public class ChoiceUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private ChoiceButtonUI choiceButtonPrefab;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Behavior")]
        [SerializeField] private bool autoHideOnSelect = true;
        [SerializeField] private float fadeSpeed = 6f;

        private StoryPlayer currentPlayer;
        private ChoiceNode currentChoice;
        private readonly List<ChoiceButtonUI> spawnedButtons = new List<ChoiceButtonUI>();
        private readonly List<ChoiceEntry> currentEntries = new List<ChoiceEntry>();
        private Coroutine timeoutCoroutine;

        public bool IsVisible => choicePanel != null && choicePanel.activeSelf;

        public event Action OnChoicesShown;
        public event Action OnChoicesHidden;

        private void Start()
        {
            HideImmediate();

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
            if (node is ChoiceNode choice)
            {
                ShowChoices(choice);
            }
        }

        private void OnNodeExit(StoryNode node)
        {
            if (node is ChoiceNode)
            {
                if (autoHideOnSelect)
                {
                    Hide();
                }
            }
        }

        public void ShowChoices(ChoiceNode choiceNode)
        {
            currentChoice = choiceNode;
            BuildEntries(choiceNode);

            if (choicePanel != null)
            {
                choicePanel.SetActive(true);
            }

            if (promptText != null)
            {
                promptText.text = choiceNode.PromptText ?? string.Empty;
            }

            ClearButtons();
            SpawnButtons();

            OnChoicesShown?.Invoke();

            StartChoiceTimeout();
        }

        private void BuildEntries(ChoiceNode choiceNode)
        {
            currentEntries.Clear();

            List<Choice> available = null;
            if (currentPlayer?.Context != null)
            {
                available = currentPlayer.Context.GetTempData<List<Choice>>("availableChoices");
            }

            if (available == null || available.Count == 0)
            {
                available = choiceNode.Choices;
            }

            for (int i = 0; i < choiceNode.Choices.Count; i++)
            {
                var choice = choiceNode.Choices[i];
                if (available.Contains(choice))
                {
                    currentEntries.Add(new ChoiceEntry { choice = choice, index = i });
                }
            }

            if (choiceNode.ShuffleChoices && currentEntries.Count > 1)
            {
                for (int i = 0; i < currentEntries.Count; i++)
                {
                    int swapIndex = UnityEngine.Random.Range(i, currentEntries.Count);
                    var temp = currentEntries[i];
                    currentEntries[i] = currentEntries[swapIndex];
                    currentEntries[swapIndex] = temp;
                }
            }
        }

        private void SpawnButtons()
        {
            if (choiceButtonPrefab == null || choicesContainer == null)
            {
                Debug.LogWarning("ChoiceUI is missing prefab or container reference.");
                return;
            }

            foreach (var entry in currentEntries)
            {
                var button = Instantiate(choiceButtonPrefab, choicesContainer);
                button.Setup(entry.choice, entry.index, OnChoiceSelected);
                spawnedButtons.Add(button);
            }
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            StopChoiceTimeout();

            currentPlayer?.SelectChoice(choiceIndex);

            if (autoHideOnSelect)
            {
                Hide();
            }
        }

        private void StartChoiceTimeout()
        {
            StopChoiceTimeout();

            if (currentChoice == null || currentChoice.ChoiceTimeout <= 0f)
            {
                return;
            }

            timeoutCoroutine = StartCoroutine(ChoiceTimeoutRoutine(currentChoice.ChoiceTimeout));
        }

        private IEnumerator ChoiceTimeoutRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (currentChoice == null)
            {
                yield break;
            }

            int fallbackIndex = currentChoice.DefaultChoiceIndex;
            if (!TrySelectChoiceByOriginalIndex(fallbackIndex))
            {
                if (currentEntries.Count > 0)
                {
                    OnChoiceSelected(currentEntries[0].index);
                }
            }
        }

        private bool TrySelectChoiceByOriginalIndex(int originalIndex)
        {
            if (originalIndex < 0)
            {
                return false;
            }

            foreach (var entry in currentEntries)
            {
                if (entry.index == originalIndex)
                {
                    OnChoiceSelected(entry.index);
                    return true;
                }
            }

            return false;
        }

        private void StopChoiceTimeout()
        {
            if (timeoutCoroutine != null)
            {
                StopCoroutine(timeoutCoroutine);
                timeoutCoroutine = null;
            }
        }

        private void ClearButtons()
        {
            for (int i = 0; i < spawnedButtons.Count; i++)
            {
                if (spawnedButtons[i] != null)
                {
                    Destroy(spawnedButtons[i].gameObject);
                }
            }
            spawnedButtons.Clear();
        }

        public void Hide()
        {
            StopChoiceTimeout();
            ClearButtons();

            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
            }

            currentChoice = null;
            OnChoicesHidden?.Invoke();
        }

        private void HideImmediate()
        {
            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
            }
        }

        private struct ChoiceEntry
        {
            public Choice choice;
            public int index;
        }
    }
}
