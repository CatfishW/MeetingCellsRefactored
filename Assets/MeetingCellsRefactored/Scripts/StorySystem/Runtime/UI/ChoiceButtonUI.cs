using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StorySystem.Nodes;

namespace StorySystem.UI
{
    /// <summary>
    /// UI component for a single choice button
    /// </summary>
    public class ChoiceButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image icon;

        private int choiceIndex = -1;
        private Action<int> onSelected;

        public void Setup(Choice choice, int index, Action<int> onSelectedCallback)
        {
            choiceIndex = index;
            onSelected = onSelectedCallback;

            if (label != null)
            {
                label.text = choice != null ? choice.text : string.Empty;
            }

            if (icon != null)
            {
                if (choice != null && choice.icon != null)
                {
                    icon.sprite = choice.icon;
                    icon.gameObject.SetActive(true);
                }
                else
                {
                    icon.gameObject.SetActive(false);
                }
            }

            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            onSelected?.Invoke(choiceIndex);
        }
    }
}
