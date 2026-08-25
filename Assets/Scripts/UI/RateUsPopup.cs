using Commons;
using Controllers;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// The "Rate Us" popup, opened from Settings. Five tappable stars, a line of copy that
    /// answers the tap, and a button through to the store listing.
    ///
    /// **The stars do not submit anything.** Only Google's In-App Review API can take a rating
    /// from inside the app, and that package is not installed (see next_step.md), so this opens
    /// the Play Store listing and the player rates there. The stars exist to make the ask feel
    /// like a question rather than a billboard — which is why the copy answers the tap, and why
    /// a low score still goes to the same place rather than being quietly swallowed.
    /// </summary>
    public class RateUsPopup : MonoBehaviour
    {
        [Header("Stars")]
        [Tooltip("The five star buttons, left to right. Index 0 is one star.")]
        [SerializeField] private Button[] starButtons;

        [Tooltip("The image on each star, in the same order as the buttons.")]
        [SerializeField] private Image[] starImages;

        [SerializeField] private Sprite starFilled;
        [SerializeField] private Sprite starEmpty;

        [Header("Copy")]
        [Tooltip("Shown before any star is tapped.")]
        [TextArea] [SerializeField] private string promptMessage = "How are we doing?";

        [Tooltip("One line per rating, low to high. Five entries: 1 star first.")]
        [TextArea] [SerializeField] private string[] messagesByRating =
        {
            "Sorry to hear it! Tell us what went wrong.",
            "Sorry to hear it! Tell us what went wrong.",
            "Thanks! Tell us how we can do better.",
            "Glad you like it! Mind saying so on the store?",
            "You're the best! Mind saying so on the store?"
        };

        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Buttons")]
        [SerializeField] private Button rateButton;
        [SerializeField] private CanvasGroup rateGroup;
        [SerializeField] private Button laterButton;

        [Tooltip("Alpha on the rate button before a rating is picked.")]
        [SerializeField] private float disabledAlpha = 0.45f;

        [Header("Store")]
        [Tooltip("Leave empty to build the URL from the application id, which is almost always " +
                 "what you want. Set it only to point at a different listing.")]
        [SerializeField] private string storeUrlOverride;

        private int _rating;

        // Set when a level is won on the way back to Home, and consumed there. A flag rather than
        // the popup being opened from the win screen directly: the ask lands better once the
        // player is idle on Home than on top of the reward they are still collecting.
        private static bool _armed;

        // Statics survive scene loads, so an arm left over from a session the player already
        // finished would fire on the next Home they see.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetArmed() => _armed = false;

        /// <summary>
        /// Says a level was just completed. Called on the way out of the win popup; Home decides
        /// whether the prompt is actually due.
        /// </summary>
        public static void ArmAfterLevelWin() => _armed = true;

        /// <summary>Reads the arm and clears it, so one win can only ever raise one prompt.</summary>
        public static bool ConsumeArmed()
        {
            bool was = _armed;
            _armed = false;
            return was;
        }

        // Popup.Close() destroys the popup half a second later, so without this the buttons stay
        // live through the closing animation and a second tap opens the store twice.
        private bool _resolved;

        private void Awake()
        {
            // Wired here rather than as prefab UnityEvents: the buttons are already serialized for
            // the star logic, and a per-index handler cannot be pointed at the wrong star.
            if (starButtons != null)
            {
                for (int i = 0; i < starButtons.Length; i++)
                {
                    if (starButtons[i] == null) continue;
                    int rating = i + 1;
                    starButtons[i].onClick.AddListener(() => SetRating(rating));
                }
            }

            if (rateButton != null) rateButton.onClick.AddListener(OpenStore);
            if (laterButton != null) laterButton.onClick.AddListener(Later);
        }

        private void OnEnable()
        {
            _rating = 0;
            _resolved = false;
            Refresh();
        }

        /// <summary>Wired to each star. Fills up to <paramref name="rating"/> and answers the tap.</summary>
        public void SetRating(int rating)
        {
            if (_resolved) return;
            SoundController.Instance.PlayButtonClickClip();
            _rating = Mathf.Clamp(rating, 0, starButtons == null ? 0 : starButtons.Length);
            Refresh();
        }

        private void Refresh()
        {
            if (starImages != null)
            {
                for (int i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] == null) continue;
                    starImages[i].sprite = i < _rating ? starFilled : starEmpty;
                }
            }

            if (messageText != null)
            {
                messageText.text = _rating <= 0 || messagesByRating == null || messagesByRating.Length == 0
                    ? promptMessage
                    : messagesByRating[Mathf.Clamp(_rating - 1, 0, messagesByRating.Length - 1)];
            }

            // Greyed rather than hidden until a star is picked, so the player can see where the
            // flow ends before committing to it.
            bool ready = _rating > 0;
            if (rateButton != null) rateButton.interactable = ready;
            if (rateGroup != null) rateGroup.alpha = ready ? 1f : disabledAlpha;
        }

        /// <summary>
        /// Wired to the rate button. Tries Play's in-app rating card first and falls back to the
        /// store listing when it cannot run — an explicit press is a promise, so it has to lead
        /// somewhere visible either way.
        /// </summary>
        public void OpenStore()
        {
            if (_resolved || _rating <= 0) return;
            _resolved = true;
            SoundController.Instance.PlayButtonClickClip();

            // Recorded before the flow, not after: the card may legitimately show nothing (see
            // InAppReview), and a player who asked to rate should not be asked again either way.
            MarkRated();

            InAppReview.Request(ok =>
            {
                if (!ok) Application.OpenURL(StoreUrl());
                Close();
            });
        }

        private void MarkRated()
        {
            GameSetting setting = Setting;
            if (setting != null) setting.MarkRated();
        }

        /// <summary>
        /// The save data, from whichever scene this popup was opened in. Settings opens it on
        /// Home, the automatic prompt does too, but the level scenes have no HomeScene.
        /// </summary>
        private static GameSetting Setting
        {
            get
            {
                if (HomeScene.Instance != null) return HomeScene.Instance.GameSetting;
                if (GameController.Instance != null) return GameController.Instance.GameSetting;
                return null;
            }
        }

        /// <summary>Wired to the "later" button.</summary>
        public void Later()
        {
            if (_resolved) return;
            _resolved = true;
            SoundController.Instance.PlayButtonClickClip();
            Close();
        }

        /// <summary>
        /// Pushes the automatic prompt a week out. Called by whatever opened this popup
        /// automatically, not by the popup itself: opening it from Settings is the player
        /// choosing to look, and should not reschedule anything.
        /// </summary>
        public static void SnoozeAutomaticPrompt()
        {
            GameSetting setting = Setting;
            if (setting != null) setting.SnoozeRatePrompt();
        }

        /// <summary>Wired to the close button alongside Popup.Close.</summary>
        public void PlaySoundButton()
        {
            SoundController.Instance.PlayButtonClickClip();
        }

        /// <summary>
        /// The listing to open. On Android the <c>market://</c> scheme goes straight to the Play
        /// app with no browser in between; everywhere else (and in the Editor) the https form is
        /// the only one that resolves, and Android treats it as a fallback anyway.
        /// </summary>
        private string StoreUrl()
        {
            if (!string.IsNullOrWhiteSpace(storeUrlOverride)) return storeUrlOverride;

            string id = Application.identifier;
#if UNITY_ANDROID && !UNITY_EDITOR
            return "market://details?id=" + id;
#else
            return "https://play.google.com/store/apps/details?id=" + id;
#endif
        }

        private void Close()
        {
            var popup = GetComponent<Popup>();
            if (popup != null) popup.Close();
        }
    }
}
