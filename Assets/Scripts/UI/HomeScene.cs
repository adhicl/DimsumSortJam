using Commons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HomeScene : MonoBehaviour
    {
        public GameSetting _gameSetting;

        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI lifeText;

        [Tooltip("The ∞ glyph over the heart. Shown only while an unlimited-lives window runs.")]
        [SerializeField] private GameObject lifeInfinityIcon;

        [Header("Profile")]
        [SerializeField] private AvatarCatalog avatarCatalog;

        [Tooltip("The top bar's avatar picture.")]
        [SerializeField] private Image avatarImage;

        [Tooltip("Optional — the player's name, if the bar shows one.")]
        [SerializeField] private TextMeshProUGUI playerNameText;

        // The life counter ticks in real time, and gold changes the moment something is bought
        // in the shop tab, so the bar is refreshed on a timer rather than once in Start.
        private const float RefreshIntervalSeconds = 1f;
        private float _nextRefreshAt;

        #region singleton
        public static HomeScene Instance { get; private set; }

        private void Awake()
        {
            // If there is an instance, and it's not me, delete myself.

            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }
        #endregion

        public GameSetting GameSetting => _gameSetting;

        private void Start()
        {
            RefreshProfile();
            Refresh();
        }

        /// <summary>
        /// Redraws the name and avatar. Called on load and again by the profile popup after a
        /// save — the profile only changes when the player changes it, so unlike the currencies
        /// it does not belong in the per-second tick.
        /// </summary>
        public void RefreshProfile()
        {
            if (_gameSetting == null) return;

            if (avatarImage != null && avatarCatalog != null)
            {
                avatarImage.sprite = avatarCatalog.Get(_gameSetting.avatarId);
            }

            if (playerNameText != null) playerNameText.text = _gameSetting.playerName;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshAt) return;

            _nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
            Refresh();
        }

        /// <summary>
        /// Redraws the top bar. While an unlimited-lives window is running the heart shows ∞
        /// and counts it down; the rest of the time it shows how many lives the player holds.
        /// </summary>
        private void Refresh()
        {
            if (_gameSetting == null) return;

            // Catches a window running out or a life regenerating while the player sits on this
            // screen, so the heart updates there and then instead of at the next boot.
            _gameSetting.RefreshLives();

            if (coinText != null) coinText.text = _gameSetting.totalGold.ToString("N0");

            var remaining = _gameSetting.UnlimitedLivesRemaining;
            bool unlimited = remaining > System.TimeSpan.Zero;

            if (lifeInfinityIcon != null) lifeInfinityIcon.SetActive(unlimited);

            if (lifeText != null)
            {
                lifeText.text = unlimited
                    ? Settings.GetCountdownFormat(remaining)
                    : _gameSetting.totalLife.ToString("N0");
            }
        }
    }
}
