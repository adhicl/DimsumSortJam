using System;
using Commons;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Controllers
{
    /// <summary>
    /// Shows a bottom-anchored adaptive banner ad for the scene it lives in.
    /// Attach one instance to each scene that should display a banner (Home, Game).
    /// The Google Mobile Ads SDK is initialized once for the whole app lifetime.
    /// </summary>
    public class BannerAdController : MonoBehaviour
    {
        // Banner ad unit for this project. Google's test id is used automatically
        // inside the Editor / development builds so real impressions are never sent.
        private const string AndroidAdUnitId = "ca-app-pub-8590881680208951/8740207165";
        private const string IosAdUnitId = "ca-app-pub-8590881680208951/8740207165";
        private const string TestAdUnitId = "ca-app-pub-3940256099942544/6300978111";

        [Tooltip("Use Google's sample banner id instead of the live one (recommended while testing).")]
        [SerializeField] private bool useTestAd = true;

        [Tooltip("GameSetting asset — banners are suppressed once the player buys remove_ads.")]
        [SerializeField] private GameSetting gameSetting;

        // Shared across scenes so MobileAds.Initialize only runs once.
        private static bool _sdkInitialized;

        /// <summary>
        /// Height of the on-screen banner in device pixels, or 0 when no banner is being shown.
        /// UI that must stay clear of the ad (see <see cref="UI.SafeAreaPanel"/>) reads this and
        /// listens to <see cref="BannerHeightChanged"/>, because an adaptive banner only reports
        /// its real height once the ad has finished loading.
        /// </summary>
        public static float BannerHeightPixels { get; private set; }

        /// <summary>Raised on the Unity main thread whenever <see cref="BannerHeightPixels"/> changes.</summary>
        public static event Action<float> BannerHeightChanged;

        private BannerView _bannerView;
        private bool _loadRequested;

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

        private void Start()
        {
            if (AdsRemoved)
            {
                SetBannerHeight(0f);
                return;
            }

            // Marshal ad callbacks onto the Unity main thread so we can touch
            // GameObjects/UI safely from the event handlers.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            if (_sdkInitialized)
            {
                RequestBanner();
                return;
            }

            // The Ads SDK must not start until UMP has an answer, or we risk serving a
            // personalized ad to a player who has not consented.
            ConsentController.WhenAdsAllowed(() =>
                MobileAds.Initialize(_ => { _sdkInitialized = true; }));
        }

        private void Update()
        {
            if (AdsRemoved)
            {
                // Covers a purchase made while this scene is already live.
                DestroyBanner();
                SetBannerHeight(0f);
                return;
            }

            // Wait for one-time SDK init to complete, then load a single banner.
            if (_sdkInitialized && !_loadRequested)
            {
                RequestBanner();
            }
        }

        private void RequestBanner()
        {
            _loadRequested = true;
            DestroyBanner();

            AdSize adaptiveSize =
                AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

            string unitId = useTestAd ? TestAdUnitId : AdUnitId;
            _bannerView = new BannerView(unitId, adaptiveSize, AdPosition.Bottom);

            // An adaptive banner does not know its height until the ad is back, so the UI can
            // only be inset once this fires. Handlers run on the main thread (see Start).
            BannerView loadedView = _bannerView;
            loadedView.OnBannerAdLoaded += () =>
            {
                // A newer request may have replaced this view while the ad was in flight.
                if (_bannerView == loadedView)
                {
                    SetBannerHeight(loadedView.GetHeightInPixels());
                }
            };

            _bannerView.LoadAd(new AdRequest());
        }

        private void DestroyBanner()
        {
            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
            }
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
            DestroyBanner();

            // The last known height is deliberately kept across a scene change: the next scene
            // shows a banner of the same size, so holding the inset avoids the UI jumping down
            // and back up again while that banner loads.
        }
    }
}
