using System;
using GoogleMobileAds.Api;

namespace Controllers
{
    /// <summary>
    /// One-time start-up of the Google Mobile Ads SDK, shared by <see cref="BannerAdController"/>
    /// and <see cref="RewardedAdController"/> so neither needs to know whether the other has
    /// already kicked it off. Goes through <see cref="ConsentController.WhenAdsAllowed"/>, so the
    /// SDK never starts before UMP has an answer.
    /// </summary>
    public static class MobileAdsSdk
    {
        private static bool _initialized;
        private static bool _initializing;
        private static Action _onInitialized;

        public static bool IsInitialized => _initialized;

        /// <summary>
        /// Runs <paramref name="action"/> once the SDK is up. Late callers run immediately.
        /// Never runs if the player declined consent, which is the SDK staying off by design.
        /// </summary>
        public static void WhenInitialized(Action action)
        {
            if (action == null) return;

            if (_initialized)
            {
                action();
                return;
            }

            _onInitialized += action;
            if (_initializing) return;
            _initializing = true;

            // Marshal ad callbacks onto the Unity main thread so the handlers can touch
            // GameObjects/UI safely.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            ConsentController.WhenAdsAllowed(() =>
                MobileAds.Initialize(_ =>
                {
                    _initialized = true;
                    Action pending = _onInitialized;
                    _onInitialized = null;
                    pending?.Invoke();
                }));
        }
    }
}
