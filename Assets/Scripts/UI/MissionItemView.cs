using System;
using System.Collections.Generic;
using DG.Tweening;
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

        [Tooltip("Seconds each colour variant stays on screen before the icon moves to the next.")]
        [SerializeField] private float variantCycleSeconds = 1.2f;

        private Action _onClaimClicked;
        private readonly List<Sprite> _icons = new List<Sprite>();

        /// <summary>Fills every field for one mission and reconnects the claim button.</summary>
        public void Bind(Sprite jajananIcon, Sprite[] variantIcons, string title, int currentCount,
            int targetCount, int rewardCoins, Action onClaimClicked)
        {
            _onClaimClicked = onClaimClicked;

            _icons.Clear();
            if (jajananIcon != null) _icons.Add(jajananIcon);
            if (variantIcons != null)
            {
                foreach (var variant in variantIcons)
                {
                    if (variant != null && !_icons.Contains(variant)) _icons.Add(variant);
                }
            }
            if (icon != null && _icons.Count > 0) icon.sprite = _icons[0];
            if (titleText != null) titleText.text = title;
            if (rewardText != null) rewardText.text = $"{rewardCoins}";

            if (rewardButton != null)
            {
                rewardButton.onClick.RemoveAllListeners();
                rewardButton.onClick.AddListener(() => _onClaimClicked?.Invoke());
            }

            ShowCount(currentCount, targetCount);
        }

        /// <summary>The claim button, so a caller can fly coins out of it.</summary>
        public RectTransform RewardButton => rewardButton != null ? (RectTransform)rewardButton.transform : null;

        /// <summary>Puts a count on the bar and lights the claim button once it is full.</summary>
        public void ShowCount(float count, int targetCount)
        {
            float clamped = Mathf.Clamp(count, 0f, targetCount);
            int shown = Mathf.FloorToInt(clamped);
            bool readyToClaim = shown >= targetCount;

            if (progressSlider != null)
            {
                progressSlider.maxValue = targetCount;
                progressSlider.value = clamped;
            }
            if (progressText != null) progressText.text = $"{shown}/{targetCount}";

            if (rewardIcon != null)
            {
                rewardIcon.color = readyToClaim ? readyToClaimTint : inProgressTint;
            }

            if (rewardButton != null) rewardButton.interactable = readyToClaim;
        }

        /// <summary>Fills the bar from one count to another, for the collection panel after a win.</summary>
        public Tween AnimateCount(int from, int to, int targetCount, float duration)
        {
            float value = from;
            ShowCount(from, targetCount);
            return DOTween.To(() => value, v =>
                {
                    value = v;
                    ShowCount(v, targetCount);
                }, to, duration)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject);
        }

        /// <summary>The claim button, held dead regardless of the count (while coins are flying).</summary>
        public void LockClaim()
        {
            if (rewardButton != null) rewardButton.interactable = false;
        }

        // Derived from the clock rather than a per-row timer, so every row with variants flips
        // in step.
        private void Update()
        {
            if (icon == null || _icons.Count < 2 || variantCycleSeconds <= 0f) return;

            int index = (int)(Time.unscaledTime / variantCycleSeconds) % _icons.Count;
            if (icon.sprite != _icons[index]) icon.sprite = _icons[index];
        }
    }
}
