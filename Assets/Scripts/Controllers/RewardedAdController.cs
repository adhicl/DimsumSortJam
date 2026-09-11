using System;
using Commons;
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

        /// <summary>
        /// True from the moment a rewarded ad is handed to the SDK until it closes or fails.
        /// A rewarded video can run for 30 seconds or more, and the level countdown must not
        /// drain underneath it. Static so the timer can ask without holding a reference.
        /// </summary>
        public static bool IsShowingAd { get; private set; }

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

            // The Ads SDK must not start until UMP has an answer, or we risk serving a
            // personalized ad to a player who has not consented.
            ConsentController.WhenAdsAllowed(() =>
                MobileAds.Initialize(_ =>
                {
                    _sdkInitialized = true;
                    LoadAd();
                }));
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
            ad.OnAdFullScreenContentClosed += () =>
            {
                IsShowingAd = false;
                LoadAd();
            };
            ad.OnAdFullScreenContentFailed += (AdError err) =>
            {
                IsShowingAd = false;
                Debug.LogWarning($"[RewardedAd] Failed to present: {err}");
                LoadAd();
            };
        }

        public bool IsReady => _rewardedAd != null && _rewardedAd.CanShowAd();

        /// <summary>
        /// Whether the "watch an ad" option should be offered to the player. Deliberately not the
        /// same as <see cref="IsReady"/>: in the Editor <see cref="ShowAd"/> grants the reward
        /// without a real ad, so gating a button on IsReady would leave it greyed out forever and
        /// the flow could never be exercised in Play mode.
        /// </summary>
        public bool IsAvailable
        {
#if UNITY_EDITOR
            get => true;
#else
            get => IsReady;
#endif
        }

        /// <summary>
        /// Shows a rewarded ad. <paramref name="onReward"/> fires once the user has
        /// earned the reward. <paramref name="onUnavailable"/> fires when no ad is ready
        /// (device only) so the caller can decide how to fall back.
        /// </summary>
        /// <param name="placement">
        /// Which button this ad was offered from, for analytics — one of the
        /// <c>GameAnalytics.Placement*</c> constants. Optional so the existing two-argument call
        /// sites keep compiling; they just report as "unknown".
        /// </param>
        public void ShowAd(Action onReward, Action onUnavailable = null, string placement = null)
        {
            if (IsReady)
            {
                // Cleared by the closed/failed handlers registered in RegisterReloadHandlers.
                IsShowingAd = true;
                _rewardedAd.Show(_ =>
                {
                    // Inside the reward callback, not next to Show: this fires when the user has
                    // actually earned the reward, which is the number worth having. Show only
                    // means the ad was put on screen, and a user who backs out never rewards.
                    GameAnalytics.RewardedAdCompleted(placement);
                    onReward?.Invoke();
                });
                return;
            }

#if UNITY_EDITOR
            // The in-editor SDK is a stub with no real fill; grant the reward so the
            // three ad flows can be exercised in Play mode.
            Debug.Log("[RewardedAd] Editor fallback: granting reward without a real ad.");
            onReward?.Invoke();
#else
            Debug.LogWarning("[RewardedAd] No ad ready; invoking unavailable fallback.");
            GameAnalytics.RewardedAdUnavailable(placement);
            onUnavailable?.Invoke();
            LoadAd();
#endif
        }

        private void OnDestroy()
        {
            // Never leave the timer frozen because the controller died mid-ad.
            IsShowingAd = false;

            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
            if (Instance == this) Instance = null;
        }
    }
}
