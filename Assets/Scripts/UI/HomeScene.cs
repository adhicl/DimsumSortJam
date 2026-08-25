using System.Collections;
using Commons;
using Ricimi;
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

        [Header("Rating prompt")]
        [Tooltip("Rate-Us-Popup. Leave empty to switch the automatic prompt off entirely.")]
        [SerializeField] private GameObject ratePopupPrefab;

        [Tooltip("Seconds to wait after landing on Home before asking, so the prompt does not " +
                 "collide with the scene settling or with anything the win flow left on screen.")]
        [SerializeField] private float ratePromptDelay = 1.5f;

        [Tooltip("Canvas the prompt is parented to. Found by name if left empty - this component " +
                 "lives on SceneContext, which is not itself under a Canvas.")]
        [SerializeField] private Canvas popupCanvas;

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
            StartCoroutine(MaybeShowRatePrompt());
        }

        /// <summary>
        /// Asks for a rating once the player is idle on Home, having just finished a level.
        /// Everything about *whether* it is due lives in <see cref="GameSetting.CanShowRatePrompt"/>;
        /// this only picks the moment.
        /// </summary>
        private IEnumerator MaybeShowRatePrompt()
        {
            // Read first, so one win can only ever raise one prompt however this exits.
            if (!RateUsPopup.ConsumeArmed()) yield break;
            if (ratePopupPrefab == null || _gameSetting == null) yield break;
            if (!_gameSetting.CanShowRatePrompt) yield break;

            yield return new WaitForSeconds(ratePromptDelay);

            // Something else got there first - the out-of-lives gate, a purchase, an ad. Asking
            // over the top of it would be the worst possible moment, and the arm is already spent,
            // so this simply waits for the next level instead.
            if (Popup.AnyOpen) yield break;

            Canvas canvas = ResolvePopupCanvas();
            if (canvas == null) yield break;

            // Scheduled the moment it is shown rather than when it is dismissed: every way out of
            // the popup - Later, the X, or backing out of Play's own card - should buy the same
            // week of quiet.
            RateUsPopup.SnoozeAutomaticPrompt();

            GameObject popup = Instantiate(ratePopupPrefab, canvas.rootCanvas.transform, false);
            popup.SetActive(true);
            popup.GetComponent<Popup>().Open();
        }

        /// <summary>
        /// The canvas to hang the prompt on. This component sits on <c>SceneContext</c>, outside
        /// the UI tree, so there is no parent to walk up to. The scene's own canvas is picked by
        /// name and the screen-space overlays other SDKs inject are skipped - landing on one of
        /// those puts the popup on a canvas that is not the game's.
        /// </summary>
        private Canvas ResolvePopupCanvas()
        {
            if (popupCanvas != null) return popupCanvas.rootCanvas;

            Canvas fallback = null;
            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas) continue;
                if (c.name == "Canvas") return c;
                if (fallback == null && c.renderMode != RenderMode.ScreenSpaceOverlay) fallback = c;
            }
            return fallback;
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
