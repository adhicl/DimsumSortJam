using System;
using System.Collections;
using System.Reflection;
using Commons;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Controllers
{
    /// <summary>
    /// Owns the one bottom-anchored adaptive banner for the whole app. Lives as a singleton
    /// GameObject in the Splash scene and survives scene loads, like the other controllers there.
    ///
    /// The banner is requested once, as soon as the Ads SDK is up, and then only shown or hidden
    /// as scenes come and go. Recreating it per scene meant a fresh ad request on every Home/Game
    /// hop, and each of those could come back with no fill - which is how the banner ended up
    /// missing on Home right after a level while the layout still kept a space for it.
    /// </summary>
    public class BannerAdController : MonoBehaviour
    {
        public static BannerAdController Instance { get; private set; }

        // Banner ad unit for this project. Google's test id is used automatically
        // inside the Editor / development builds so real impressions are never sent.
        private const string AndroidAdUnitId = "ca-app-pub-8590881680208951/8740207165";
        private const string IosAdUnitId = "ca-app-pub-8590881680208951/8740207165";
        private const string TestAdUnitId = "ca-app-pub-3940256099942544/6300978111";

        // A failed request is retried with a doubling delay. No fill is routine for a young app,
        // and a banner that is never re-requested stays missing for the rest of the session.
        private const float FirstRetrySeconds = 10f;
        private const float MaxRetrySeconds = 60f;

        // A request that has neither loaded nor failed by then is treated as failed. The SDK
        // does not promise a callback for every request, and a banner with no callback is a
        // banner with no retry.
        private const float LoadTimeoutSeconds = 30f;

        [Tooltip("Use Google's sample banner id instead of the live one (recommended while testing).")]
        [SerializeField] private bool useTestAd = true;

        [Tooltip("GameSetting asset - banners are suppressed once the player buys remove_ads.")]
        [SerializeField] private GameSetting gameSetting;

        [Tooltip("Scenes that show the banner. Everywhere else it is hidden, not destroyed, so it is back the instant one of these loads.")]
        [SerializeField] private string[] bannerScenes =
        {
            "Home", "Game",
            "Tutorial1", "Tutorial2", "Tutorial3", "Tutorial4", "Tutorial5", "Tutorial6", "Tutorial7",
        };

        /// <summary>
        /// Height of the on-screen banner in device pixels, or 0 when no banner is being shown -
        /// whether because the scene has none, the ad has not loaded, or ads were removed.
        /// UI that must stay clear of the ad (see <see cref="UI.SafeAreaPanel"/>) reads this and
        /// listens to <see cref="BannerHeightChanged"/>, because an adaptive banner only reports
        /// its real height once the ad has finished loading.
        /// </summary>
        public static float BannerHeightPixels { get; private set; }

        /// <summary>
        /// True once an ad has actually come back, whether or not the current scene shows it.
        /// <see cref="BannerHeightPixels"/> cannot answer this: it is the *reserved* height, and
        /// reads 0 in a scene that hides the banner - Splash included, which is exactly where the
        /// loading screen needs to know whether the banner is ready.
        /// </summary>
        public bool IsLoaded => _loaded;

        /// <summary>Raised on the Unity main thread whenever <see cref="BannerHeightPixels"/> changes.</summary>
        public static event Action<float> BannerHeightChanged;

        private BannerView _bannerView;
        private bool _loaded;
        private float _loadedHeight;
        private bool _sceneWantsBanner;
        private float _retryDelay = FirstRetrySeconds;
        private Coroutine _retry;
        private Coroutine _loadWatchdog;

#if UNITY_EDITOR
        // Set once the Editor placeholder has been moved out of the scene, so the work - and any
        // warning about not being able to do it - happens on the first load rather than every one.
        private static bool _placeholderKept;

        // Statics survive a domain reload when that is turned off for faster iteration, and a flag
        // left set from the previous play session would skip the one thing keeping the banner
        // alive - so the second run onwards would lose it again. Matches PlayTopBar's reset.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPlaceholderKept() => _placeholderKept = false;
#endif

        private static string AdUnitId
        {
            get
            {
#if UNITY_ANDROID
                return AndroidAdUnitId;
#elif UNITY_IPHONE
                return IosAdUnitId;
#else
                return AndroidAdUnitId;
#endif
            }
        }

        /// <summary>The remove_ads purchase suppresses banners everywhere.</summary>
        private bool AdsRemoved => gameSetting != null && gameSetting.removeAds;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sceneWantsBanner = ShowsBanner(SceneManager.GetActiveScene());
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            if (AdsRemoved)
            {
                SetBannerHeight(0f);
                return;
            }

            MobileAdsSdk.WhenInitialized(RequestBanner);
        }

        private void Update()
        {
            // Covers a purchase made while the banner is already up.
            if (AdsRemoved && _bannerView != null)
            {
                DestroyBanner();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;

            _sceneWantsBanner = ShowsBanner(scene);
            ApplyVisibility();
        }

        private bool ShowsBanner(Scene scene)
        {
            return Array.IndexOf(bannerScenes, scene.name) >= 0;
        }

        private void RequestBanner()
        {
            if (AdsRemoved) return;

            DestroyBanner();

            AdSize adaptiveSize =
                AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

            // AdTestMode also forces test units on an emulator, where a live one never fills.
            bool testAds = AdTestMode.ShouldUseTestAds(useTestAd);
            string unitId = testAds ? TestAdUnitId : AdUnitId;
            Debug.Log($"[BannerAd] Requesting {(testAds ? "TEST" : "LIVE")} banner {unitId}");

            BannerView view = new BannerView(unitId, adaptiveSize, AdPosition.Bottom);
            _bannerView = view;

            // Handlers are marshalled to the main thread explicitly: ApplyVisibility ends in
            // SafeAreaPanel moving a RectTransform, and an exception thrown off-thread inside an
            // SDK callback is swallowed, not logged. Each also checks it still belongs to the live
            // view: a newer request may have replaced this one while the ad was in flight.
            view.OnBannerAdLoaded += () => MainThreadDispatcher.Run(() =>
            {
                if (_bannerView != view) return;

                CancelLoadWatchdog();

                // An adaptive banner does not know its height until the ad is back, so the UI
                // can only be inset from here.
                _loaded = true;
                _loadedHeight = view.GetHeightInPixels();
                _retryDelay = FirstRetrySeconds;
                KeepBannerAcrossScenes(view);
                ApplyVisibility();
            });
            view.OnBannerAdLoadFailed += (LoadAdError error) => MainThreadDispatcher.Run(() =>
            {
                if (_bannerView != view) return;

                CancelLoadWatchdog();
                Debug.LogWarning($"[BannerAd] Failed to load: {error}");

                // Once a banner is up, the SDK's own refresh keeps the last creative on screen
                // when a later request fails, so only a banner that never arrived needs chasing.
                if (!_loaded) ScheduleRetry();
            });

            // Start hidden; ApplyVisibility shows it once it has loaded in a scene that wants it.
            view.Hide();
            LoadInto(view);
        }

        /// <summary>Issues the request and arms the no-callback watchdog for it.</summary>
        private void LoadInto(BannerView view)
        {
            StartLoadWatchdog();
            view.LoadAd(new AdRequest());
        }

        private void StartLoadWatchdog()
        {
            CancelLoadWatchdog();
            _loadWatchdog = StartCoroutine(LoadWatchdog());
        }

        private void CancelLoadWatchdog()
        {
            if (_loadWatchdog == null) return;
            StopCoroutine(_loadWatchdog);
            _loadWatchdog = null;
        }

        private IEnumerator LoadWatchdog()
        {
            yield return new WaitForSecondsRealtime(LoadTimeoutSeconds);
            _loadWatchdog = null;

            if (_loaded || _bannerView == null) yield break;

            Debug.LogWarning("[BannerAd] No response to load request; retrying.");
            ScheduleRetry();
        }

        /// <summary>
        /// Coming back from the background is when connectivity most often changes. A banner
        /// still waiting is asked for again now rather than after whatever is left of its backoff,
        /// and a banner already up is re-shown because some devices drop the ad's window on resume.
        /// </summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused || _bannerView == null || AdsRemoved) return;

            if (!_loaded)
            {
                CancelRetry();
                _retryDelay = FirstRetrySeconds;
                LoadInto(_bannerView);
                return;
            }

            ApplyVisibility();
        }

        /// <summary>
        /// Keeps the loaded banner alive across scene loads.
        ///
        /// On device this is already true and this method does nothing: the banner is a native
        /// view owned by the Activity, with no Unity object to lose. In the Editor the plugin
        /// fakes it with an ordinary GameObject, created as a root of whichever scene happened to
        /// be active when the ad landed - so loading the next scene destroyed it while this
        /// controller still believed the ad was up, and the layout went on reserving space for a
        /// banner nobody could see.
        ///
        /// Reaching into the plugin's internals is not nice, but the alternative was re-requesting
        /// the banner on every scene change, which on device would throw away a live impression
        /// and ask AdMob for fresh fill on each screen. This costs nothing at runtime and is
        /// compiled out of player builds entirely.
        /// </summary>
        private static void KeepBannerAcrossScenes(BannerView view)
        {
#if UNITY_EDITOR
            if (_placeholderKept || view == null) return;

            const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            try
            {
                var clientField = typeof(BannerView).GetField("_client", Hidden);
                object client = clientField != null ? clientField.GetValue(view) : null;

                var objectField = client != null ? client.GetType().GetField("_gameObject", Hidden) : null;
                var placeholder = objectField != null ? objectField.GetValue(client) as GameObject : null;

                if (placeholder == null)
                {
                    // Field names are the plugin's business and can change when it is updated.
                    // Say so rather than let the banner quietly start vanishing again.
                    Debug.LogWarning("[BannerAd] Could not find the Editor placeholder object to keep " +
                                     "across scenes; the banner will disappear on the next scene load. " +
                                     "The Google Mobile Ads plugin's internals have probably changed - " +
                                     "see next_step.md, \"Ads outside Splash\".");
                    _placeholderKept = true;   // one warning, not one per load
                    return;
                }

                // DontDestroyOnLoad only honours root objects.
                if (placeholder.transform.parent != null) placeholder.transform.SetParent(null, true);
                DontDestroyOnLoad(placeholder);
                _placeholderKept = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BannerAd] Could not persist the Editor placeholder: {e.Message}");
                _placeholderKept = true;
            }
#endif
        }

        private void ScheduleRetry()
        {
            CancelRetry();
            _retry = StartCoroutine(RetryAfter(_retryDelay));
            _retryDelay = Mathf.Min(_retryDelay * 2f, MaxRetrySeconds);
        }

        private IEnumerator RetryAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _retry = null;
            if (_bannerView != null && !_loaded) LoadInto(_bannerView);
        }

        private void CancelRetry()
        {
            if (_retry == null) return;
            StopCoroutine(_retry);
            _retry = null;
        }

        /// <summary>
        /// The banner is on screen only when it has loaded and the current scene is one of
        /// <see cref="bannerScenes"/>; the reserved height follows the same rule, so no scene
        /// keeps a gap for a banner that is not there.
        /// </summary>
        private void ApplyVisibility()
        {
            bool show = _bannerView != null && _loaded && _sceneWantsBanner && !AdsRemoved;

            if (_bannerView != null)
            {
                if (show) _bannerView.Show();
                else _bannerView.Hide();
            }

            SetBannerHeight(show ? _loadedHeight : 0f);
        }

        private void DestroyBanner()
        {
            CancelRetry();
            CancelLoadWatchdog();

            if (_bannerView != null)
            {
                // Guarded because the view underneath may already be gone: in the Editor the
                // placeholder is a scene GameObject, so a scene change destroys it behind the
                // plugin's back and tearing it down again can hit a missing reference. Losing the
                // old view is the point here - a throw must not leave _bannerView dangling.
                try { _bannerView.Destroy(); }
                catch (Exception e) { Debug.LogWarning($"[BannerAd] Destroy failed: {e.Message}"); }

                _bannerView = null;
            }

            _loaded = false;
            _loadedHeight = 0f;
            SetBannerHeight(0f);
        }

        /// <summary>
        /// Publishes the reserved height, notifying listeners only when the value actually moves.
        /// </summary>
        /// <summary>
        /// Clears the reserved height and its listeners for a boot retry. The subscribers are
        /// SafeAreaPanels in a scene that is about to be unloaded.
        /// </summary>
        internal static void ResetForRestart()
        {
            BannerHeightPixels = 0f;
            BannerHeightChanged = null;
#if UNITY_EDITOR
            _placeholderKept = false;
#endif
        }

        private static void SetBannerHeight(float heightPixels)
        {
            heightPixels = Mathf.Max(0f, heightPixels);
            if (Mathf.Approximately(BannerHeightPixels, heightPixels)) return;

            BannerHeightPixels = heightPixels;
            BannerHeightChanged?.Invoke(heightPixels);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            DestroyBanner();
            Instance = null;
        }
    }
}
