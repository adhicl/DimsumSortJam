using System;
using System.Collections;
using System.Text;
using Commons;
using Controllers;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class LoadingScene : MonoBehaviour
    {
        public GameSetting _gameSetting;

        [SerializeField] private Slider loadingSlider;
        [SerializeField] private TextMeshProUGUI loadingProgressText;
        [SerializeField] private TextMeshProUGUI loadingText;

        [Tooltip("Longest the loading screen waits for the boot services - sign-in, Cloud Save, " +
                 "IAP, consent and the ads SDK - before starting on whatever is ready. A hard cap: " +
                 "nothing may keep the player on this screen longer than this.")]
        [SerializeField] private float serviceTimeout = 8f;

        [Tooltip("How long, within that wait, to also let a real banner and rewarded ad arrive. " +
                 "Fill is never guaranteed - an empty AdMob account never fills at all - so this is " +
                 "best effort and is allowed to expire on its own without holding the boot up.")]
        [SerializeField] private float adFillTimeout = 4f;

        [Tooltip("Shown when the boot gets stuck. Its wording and artwork live on the prefab. " +
                 "Leave it empty and a plain system dialog stands in.")]
        [SerializeField] private GameObject failurePopupPrefab;

        [Tooltip("If the boot has not left Splash by now, something is wedged and the player is " +
                 "offered a retry. Must comfortably exceed the two timeouts above plus the scene " +
                 "transition, or a merely slow launch would be called a failure.")]
        [SerializeField] private float stuckTimeout = 20f;

        [Tooltip("Used only for the default avatar a brand new player starts with.")]
        [SerializeField] private AvatarCatalog avatarCatalog;

        [Tooltip("Assets/Sounds/GameAudioMixer.mixer. Muting is applied here, at boot, because " +
                 "a build starts every run with the mixer at its authored volumes.")]
        public AudioMixer mixer;

        private float _elapsed;
        private bool _transitioning;

        // CanRequestAds is asked once and remembered. It is a JNI hop on Android and a logging
        // placeholder in the Editor, and this runs from Update - asking per frame spammed the
        // console with "Placeholder ConsentInformationClient" and cost a native call a frame.
        private bool _adsAllowed;
        private bool _adsAllowedKnown;

        // Set once the failure popup is up. Also latches the boot shut: if a service answers late,
        // behind the popup, the scene must not change out from under the player mid-decision.
        private bool _failed;


        private void Start()
        {
            _gameSetting.LoadData();
            StartCoroutine(DoLoading());
        }

        private float progress = 0f;
        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_failed) return;

            // Checked before the transition guard below, deliberately: the commonest way to be
            // stuck is *after* the transition starts but with the scene never actually changing,
            // and an early return there would leave nothing watching.
            if (_elapsed >= stuckTimeout)
            {
                ShowBootFailure();
                return;
            }

            // The scene is on its way out; re-evaluating the gates every frame until it unloads
            // achieves nothing and keeps polling the services.
            if (_transitioning) return;

            // Hold the bar just short of full until the boot services have settled. Which scene to
            // open is decided from currentLevel, so starting before the cloud pull lands could drop
            // a returning player back into a level they already finished - and the ads, the player
            // id and the shop are all things the first screen expects to exist.
            var target = IsBootSettled() ? 1f : 0.9f;
            progress = Mathf.Clamp(progress + Time.deltaTime, 0f, target);

            loadingSlider.value = progress;
            loadingProgressText.text = $"{progress * 100f:N0}%";

            if (progress >= 1f && !_transitioning)
            {
                _transitioning = true;
                ReportOutstanding();

                // Both run only now, after the cloud pull has resolved: refreshing early would
                // regenerate onto lives the download is about to overwrite, and granting early
                // would hand a second free window to someone who claimed today's elsewhere.
                _gameSetting.RefreshLives();
                _gameSetting.TryGrantDailyFreeUnlimitedLives();

                // Seed a name and face for a brand new player. Also after the cloud pull, so a
                // returning player's own profile lands first and this does nothing.
                _gameSetting.EnsureProfile(
                    GameServicesController.Instance != null ? GameServicesController.Instance.PlayerId : null,
                    avatarCatalog != null ? avatarCatalog.DefaultId : null);

                string newScene = "Home";
                if (_gameSetting.currentLevel < 5)
                {
                    newScene = Settings.GetNextLevelScene(_gameSetting.currentLevel, "Home");
                }
                Transition.LoadLevel(newScene, 0f, Settings.TransitionColor);
            }
        }

        /// <summary>
        /// Whether the boot is far enough along to leave this screen.
        ///
        /// Two budgets rather than one. The core services either succeed or give up on their own,
        /// so waiting for them is bounded and worth doing. Ad *fill* is not: AdMob is entitled to
        /// return no ad at all, so waiting on it without a separate, shorter limit would add the
        /// full timeout to every launch of an app that has none.
        ///
        /// Either way <see cref="serviceTimeout"/> is a hard cap. A service that never answers
        /// delays the boot; it cannot stop it.
        /// </summary>
        private bool IsBootSettled()
        {
            if (_elapsed >= serviceTimeout) return true;
            if (!AreCoreServicesReady()) return false;
            if (_elapsed < adFillTimeout && !HasAdFill()) return false;
            return true;
        }

        /// <summary>
        /// Sign-in, Cloud Save, IAP, consent and the ads SDK. Each is skipped when its controller
        /// is absent, so playing a scene directly - or Splash with the services stripped out -
        /// still boots instead of sitting out the timeout.
        /// </summary>
        private bool AreCoreServicesReady()
        {
            var services = GameServicesController.Instance;
            if (services != null && !services.SignInSettled) return false;

            var cloud = CloudSaveController.Instance;
            if (cloud != null && !cloud.HasSynced) return false;

            var iap = IAPController.Instance;
            if (iap != null && !iap.IsReady) return false;

            var consent = ConsentController.Instance;
            if (consent != null && !consent.IsResolved) return false;

            // Only meaningful once consent allows ads at all - the SDK is never started for a
            // player who declined, so waiting on it would be waiting forever.
            if (AdsAllowed() && !MobileAdsSdk.IsInitialized) return false;

            return true;
        }

        /// <summary>
        /// Whether consent permits ads, resolved once and cached. Answers false while consent is
        /// still outstanding, which is correct here: the caller has already refused to proceed on
        /// that, so there is nothing further to wait for.
        /// </summary>
        private bool AdsAllowed()
        {
            if (_adsAllowedKnown) return _adsAllowed;

            var consent = ConsentController.Instance;
            if (consent != null && !consent.IsResolved) return false;

            _adsAllowed = ConsentController.CanRequestAds();
            _adsAllowedKnown = true;
            return _adsAllowed;
        }

        /// <summary>True once an actual banner and rewarded ad are in hand, or there is none to wait for.</summary>
        private bool HasAdFill()
        {
            if (!AdsAllowed()) return true;

            var banner = BannerAdController.Instance;
            if (banner != null && !banner.IsLoaded) return false;

            var rewarded = RewardedAdController.Instance;
            if (rewarded != null && !rewarded.IsReady) return false;

            return true;
        }

        /// <summary>
        /// Names whatever was still outstanding when the boot gave up waiting. Without this a slow
        /// launch is indistinguishable from a broken service - the player waits the full timeout
        /// either way, and nothing says which one was late.
        /// </summary>
        private void ReportOutstanding()
        {
            if (AreCoreServicesReady() && HasAdFill()) return;

            var late = new StringBuilder();
            var services = GameServicesController.Instance;
            if (services != null && !services.SignInSettled) late.Append("sign-in ");

            var cloud = CloudSaveController.Instance;
            if (cloud != null && !cloud.HasSynced) late.Append("cloud-save ");

            var iap = IAPController.Instance;
            if (iap != null && !iap.IsReady) late.Append("iap ");

            var consent = ConsentController.Instance;
            if (consent != null && !consent.IsResolved) late.Append("consent ");

            if (AdsAllowed() && !MobileAdsSdk.IsInitialized) late.Append("ads-sdk ");

            var banner = BannerAdController.Instance;
            if (banner != null && !banner.IsLoaded) late.Append("banner-fill ");

            var rewarded = RewardedAdController.Instance;
            if (rewarded != null && !rewarded.IsReady) late.Append("rewarded-fill ");

            Debug.LogWarning($"[Loading] Started after {_elapsed:F1}s without: {late.ToString().TrimEnd()}. " +
                             "Ad fill not arriving is normal on an account with no demand; a core " +
                             "service listed here is worth chasing.");
        }

        /// <summary>
        /// Tells the player the boot is wedged and offers a way out. The alternative is a loading
        /// bar that sits at 100% forever, which looks like the game has crashed and leaves them
        /// nothing to do but force-close it.
        ///
        /// Falls back to a native dialog, and then to quitting outright: this runs precisely when
        /// something is already broken, so it cannot assume the popup prefab, a canvas, or the UI
        /// itself is in working order.
        /// </summary>
        private void ShowBootFailure()
        {
            _failed = true;
            ReportOutstanding();
            Debug.LogError($"[Loading] Boot still on Splash after {_elapsed:F1}s; offering a retry.");

            try
            {
                if (failurePopupPrefab != null)
                {
                    var popup = Instantiate(failurePopupPrefab, CreateFailureCanvas().transform, false);
                    popup.SetActive(true);

                    // Popup.Open plays a sound through SoundController, which lives in the game
                    // scenes and does not exist during Splash - it threw here and took the whole
                    // popup down with it. What Open adds beyond that is the dimming backdrop,
                    // which is a nicety; the alert has to appear either way.
                    var ricimi = popup.GetComponent<Popup>();
                    if (ricimi != null)
                    {
                        try { ricimi.Open(); }
                        catch (Exception e) { Debug.LogWarning($"[Loading] Popup.Open skipped: {e.Message}"); }
                    }
                    return;
                }

                Debug.LogWarning("[Loading] No failure popup prefab assigned on LoadingScene; " +
                                 "falling back to a native dialog.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Loading] Failure popup could not be shown: {e.Message}");
            }

            ShowNativeFailure();
        }

        /// <summary>
        /// Builds a canvas of its own for the failure popup rather than borrowing one.
        ///
        /// Borrowing was wrong twice over. The first canvas found tends to be Ricimi's
        /// <c>TransitionCanvas</c> - created for the scene fade and *destroyed* when that fade
        /// ends, which would take the popup with it - and a boot that failed early may have no
        /// usable scene canvas at all. Its own canvas also guarantees the popup draws over
        /// everything, including the banner.
        /// </summary>
        private Canvas CreateFailureCanvas()
        {
            var go = new GameObject("BootFailureCanvas");

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            // Matches the game's own scaler, so the popup is the size the art expects.
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            // Without an EventSystem the buttons are decoration. Splash has one, but this runs
            // when the boot is already broken, so it cannot be assumed.
            if (EventSystem.current == null)
            {
                var events = new GameObject("BootFailureEventSystem");
                events.AddComponent<EventSystem>();
                events.AddComponent<StandaloneInputModule>();
            }

            return canvas;
        }

        /// <summary>
        /// Last resort. On Android this is a real system dialog; anywhere it cannot be shown the
        /// call answers immediately with false, which quits - the boot is dead either way, and
        /// leaving the player on a frozen loading screen is the one outcome to avoid.
        /// </summary>
        private void ShowNativeFailure()
        {
            NativeDialog.ShowConfirm(
                "Connection problem",
                "We couldn't finish loading. Please check your connection and try again.",
                "Retry", "Quit",
                retry =>
                {
                    if (retry && BootRetry.TryRestart()) return;
                    BootRetry.QuitApp();
                },
                fallbackAnswer: false);
        }

        private IEnumerator DoLoading()
        {
            AudioMix.Apply(mixer, _gameSetting, this);

            // The level number used to be in here. It is the player's *current* level rather than
            // the one being opened, and it meant nothing to anyone reading it.
            string[] frames = { "Loading", "Loading.", "Loading..", "Loading..." };
            int frame = 0;

            while (true)
            {
                loadingText.text = frames[frame];
                frame = (frame + 1) % frames.Length;
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
}
