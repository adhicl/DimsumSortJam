using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// One row of the jajanan-collection mission list: icon, name, progress bar and a reward
    /// badge whose claim button is disabled until the target count is reached.
    /// </summary>
    public class MissionItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private Image rewardIcon;
        [SerializeField] private Button rewardButton;

        [SerializeField] private Color inProgressTint = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color readyToClaimTint = Color.white;

        private Action _onClaimClicked;

        /// <summary>Fills every field for one mission and reconnects the claim button.</summary>
        public void Bind(Sprite jajananIcon, string title, int currentCount, int targetCount,
            int rewardCoins, Action onClaimClicked)
        {
            _onClaimClicked = onClaimClicked;

            if (icon != null && jajananIcon != null) icon.sprite = jajananIcon;
            if (titleText != null) titleText.text = title;

            int clamped = Mathf.Clamp(currentCount, 0, targetCount);
            bool readyToClaim = clamped >= targetCount;

            if (progressSlider != null)
            {
                progressSlider.maxValue = targetCount;
                progressSlider.value = clamped;
            }
            if (progressText != null) progressText.text = $"{clamped}/{targetCount}";

            if (rewardText != null) rewardText.text = $"{rewardCoins}";

            if (rewardIcon != null)
            {
                rewardIcon.color = readyToClaim ? readyToClaimTint : inProgressTint;
            }

            if (rewardButton != null)
            {
                rewardButton.interactable = readyToClaim;
                rewardButton.onClick.RemoveAllListeners();
                rewardButton.onClick.AddListener(() => _onClaimClicked?.Invoke());
            }
        }
    }
}
