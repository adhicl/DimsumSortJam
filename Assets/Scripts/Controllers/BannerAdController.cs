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

        // Shared across scenes so MobileAds.Initialize only runs once.
        private static bool _sdkInitialized;

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

        private void Start()
        {
            // Marshal ad callbacks onto the Unity main thread so we can touch
            // GameObjects/UI safely from the event handlers.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            if (_sdkInitialized)
            {
                RequestBanner();
                return;
            }

            MobileAds.Initialize(_ => { _sdkInitialized = true; });
        }

        private void Update()
        {
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

        private void OnDestroy()
        {
            DestroyBanner();
        }
    }
}
