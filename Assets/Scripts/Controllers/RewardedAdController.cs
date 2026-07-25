using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Controllers
{
    /// <summary>
    /// Loads and shows AdMob rewarded ads. Lives as a singleton GameObject in the Game
    /// scene (mirrors GameController.Instance / SoundController.Instance) so popups and
    /// baskets can reach it without Zenject injection.
    ///
    /// Used for three placements: reviving on out-of-moves, unlocking a closed basket,
    /// and doubling the coin bonus on the win screen.
    /// </summary>
    public class RewardedAdController : MonoBehaviour
    {
        public static RewardedAdController Instance { get; private set; }

        // Google's official sample rewarded id — safe for testing (no real impressions).
        private const string AndroidTestAdUnitId = "ca-app-pub-3940256099942544/5224354917";

        [Tooltip("Use Google's sample rewarded id instead of the live one (recommended while testing).")]
        [SerializeField] private bool useTestAd = true;

        [Tooltip("Real rewarded ad unit id for release, e.g. ca-app-pub-8590881680208951/XXXXXXXXXX.")]
        [SerializeField] private string androidLiveAdUnitId = "";

        private static bool _sdkInitialized;
        private RewardedAd _rewardedAd;
        private bool _isLoading;

        private string AdUnitId =>
            (useTestAd || string.IsNullOrEmpty(androidLiveAdUnitId))
                ? AndroidTestAdUnitId
                : androidLiveAdUnitId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            if (_sdkInitialized)
            {
                LoadAd();
                return;
            }

            MobileAds.Initialize(_ =>
            {
                _sdkInitialized = true;
                LoadAd();
            });
        }

        /// <summary>Preloads a rewarded ad so it is ready when the player asks for it.</summary>
        private void LoadAd()
        {
            if (_isLoading) return;

            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }

            _isLoading = true;
            RewardedAd.Load(AdUnitId, new AdRequest(), (ad, error) =>
            {
                _isLoading = false;

                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[RewardedAd] Failed to load: {error}");
                    return;
                }

                _rewardedAd = ad;
                RegisterReloadHandlers(ad);
            });
        }

        private void RegisterReloadHandlers(RewardedAd ad)
        {
            // Rewarded ads are single-use: reload as soon as the current one goes away.
            ad.OnAdFullScreenContentClosed += LoadAd;
            ad.OnAdFullScreenContentFailed += (AdError err) =>
            {
                Debug.LogWarning($"[RewardedAd] Failed to present: {err}");
                LoadAd();
            };
        }

        public bool IsReady => _rewardedAd != null && _rewardedAd.CanShowAd();

        /// <summary>
        /// Shows a rewarded ad. <paramref name="onReward"/> fires once the user has
        /// earned the reward. <paramref name="onUnavailable"/> fires when no ad is ready
        /// (device only) so the caller can decide how to fall back.
        /// </summary>
        public void ShowAd(Action onReward, Action onUnavailable = null)
        {
            if (IsReady)
            {
                _rewardedAd.Show(_ => onReward?.Invoke());
                return;
            }

#if UNITY_EDITOR
            // The in-editor SDK is a stub with no real fill; grant the reward so the
            // three ad flows can be exercised in Play mode.
            Debug.Log("[RewardedAd] Editor fallback: granting reward without a real ad.");
            onReward?.Invoke();
#else
            Debug.LogWarning("[RewardedAd] No ad ready; invoking unavailable fallback.");
            onUnavailable?.Invoke();
            LoadAd();
#endif
        }

        private void OnDestroy()
        {
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
            if (Instance == this) Instance = null;
        }
    }
}
