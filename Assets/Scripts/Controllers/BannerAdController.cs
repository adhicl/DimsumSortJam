using System;
using System.Collections;
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

        /// <summary>Raised on the Unity main thread whenever <see cref="BannerHeightPixels"/> changes.</summary>
        public static event Action<float> BannerHeightChanged;

        private BannerView _bannerView;
        private bool _loaded;
        private float _loadedHeight;
        private bool _sceneWantsBanner;
        private float _retryDelay = FirstRetrySeconds;
        private Coroutine _retry;
        private Coroutine _loadWatchdog;

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

            string unitId = useTestAd ? TestAdUnitId : AdUnitId;
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
                _bannerView.Destroy();
                _bannerView = null;
            }

            _loaded = false;
            _loadedHeight = 0f;
            SetBannerHeight(0f);
        }

        /// <summary>
        /// Publishes the reserved height, notifying listeners only when the value actually moves.
        /// </summary>
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
