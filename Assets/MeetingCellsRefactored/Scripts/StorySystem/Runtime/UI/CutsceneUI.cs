using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using StorySystem.Core;
using StorySystem.Execution;
using StorySystem.Nodes;
namespace StorySystem.UI
{
    /// <summary>
    /// UI component for handling cutscene playback and skipping
    /// </summary>
    public class CutsceneUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject cutscenePanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button skipButton;

        [Header("Playback")]
        [SerializeField] private PlayableDirector timelineDirector;
        [SerializeField] private float fadeSpeed = 6f;

        private StoryPlayer currentPlayer;
        private CutsceneNode currentCutscene;
        private Coroutine skipHoldCoroutine;

        public bool IsActive => cutscenePanel != null && cutscenePanel.activeSelf;

        public event Action OnCutsceneStart;
        public event Action<bool> OnCutsceneEnd;

        private void Start()
        {
            HideImmediate();

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(SkipCutscene);
                skipButton.onClick.AddListener(SkipCutscene);
            }

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
            if (node is CutsceneNode cutscene)
            {
                PlayCutscene(cutscene);
            }
        }

        private void OnNodeExit(StoryNode node)
        {
            if (node is CutsceneNode)
            {
                // Let the cutscene end logic control hiding
            }
        }

        public void PlayCutscene(CutsceneNode cutscene)
        {
            currentCutscene = cutscene;

            if (cutscenePanel != null)
            {
                cutscenePanel.SetActive(true);
            }

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(cutscene.Skippable);
            }

            OnCutsceneStart?.Invoke();

            if (cutscene.CutsceneType == CutsceneType.Timeline && timelineDirector != null)
            {
                timelineDirector.stopped -= OnTimelineStopped;
                timelineDirector.playableAsset = cutscene.TimelineAsset;
                timelineDirector.stopped += OnTimelineStopped;
                timelineDirector.Play();
            }
            else
            {
                // For non-timeline cutscenes, external systems should call CompleteCutscene.
            }
        }

        private void OnTimelineStopped(PlayableDirector director)
        {
            CompleteCutscene(false);
        }

        public void CompleteCutscene(bool skipped)
        {
            if (currentCutscene == null || currentPlayer?.Context == null)
            {
                Hide();
                return;
            }

            currentCutscene.OnCutsceneComplete(currentPlayer.Context, skipped);
            OnCutsceneEnd?.Invoke(skipped);
            Hide();
        }

        public void SkipCutscene()
        {
            if (currentCutscene == null || !currentCutscene.Skippable)
            {
                return;
            }

            if (currentCutscene.SkipHoldTime > 0f)
            {
                if (skipHoldCoroutine == null)
                {
                    skipHoldCoroutine = StartCoroutine(SkipHoldRoutine(currentCutscene.SkipHoldTime));
                }
                return;
            }

            ForceSkip();
        }

        public void CancelSkipHold()
        {
            if (skipHoldCoroutine != null)
            {
                StopCoroutine(skipHoldCoroutine);
                skipHoldCoroutine = null;
            }
        }

        private IEnumerator SkipHoldRoutine(float holdTime)
        {
            yield return new WaitForSeconds(holdTime);
            skipHoldCoroutine = null;
            ForceSkip();
        }

        private void ForceSkip()
        {
            if (timelineDirector != null && timelineDirector.state == PlayState.Playing)
            {
                timelineDirector.Stop();
            }

            CompleteCutscene(true);
        }

        public void Hide()
        {
            if (cutscenePanel != null)
            {
                cutscenePanel.SetActive(false);
            }

            currentCutscene = null;
        }

        private void HideImmediate()
        {
            if (cutscenePanel != null)
            {
                cutscenePanel.SetActive(false);
            }
        }
    }
}
