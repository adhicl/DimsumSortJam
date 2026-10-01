using System;
using System.Collections;
using Commons;
using GoogleMobileAds.Api;
using UnityEngine;

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
        // If Initialize has not called back by then, it is called again. The SDK tolerates
        // repeat calls, and a first initialization that stalls on a bad connection otherwise
        // leaves every ad controller waiting on a callback that is not coming.
        private const float InitTimeoutSeconds = 20f;

        private static bool _initialized;
        private static bool _initializing;
        private static Action _onInitialized;
        private static Coroutine _watchdog;

        public static bool IsInitialized => _initialized;

        /// <summary>
        /// Runs <paramref name="action"/> once the SDK is up. Late callers run immediately.
        /// Waits — rather than never running — while ads are not yet allowed; see
        /// <see cref="ConsentController.WhenAdsAllowed"/>.
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

            // Obsolete in plugin 10.7+, but harmless, and if it still does its job every callback
            // below already lands on the main thread and MainThreadDispatcher.Run costs nothing.
            // The explicit marshalling is what is relied on; this is the free extra.
#pragma warning disable 0618
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
#pragma warning restore 0618

            ConsentController.WhenAdsAllowed(Initialize);
        }

        /// <summary>
        /// Forgets that the SDK was ever started, so a boot retry asks for it again.
        ///
        /// The native SDK cannot really be un-initialized and does not need to be - it tolerates
        /// a repeat Initialize. What has to go is *our* memory of it: `_initializing` latches on
        /// the first call and would make every later WhenInitialized a silent no-op, and
        /// `_onInitialized` still holds callbacks belonging to controllers the retry destroys.
        /// </summary>
        internal static void ResetForRestart()
        {
            StopWatchdog();
            _initialized = false;
            _initializing = false;
            _onInitialized = null;
        }

        private static void Initialize()
        {
            // Must be on the main thread: WhenAdsAllowed delivers there, but the SDK's Next-Gen
            // Android path requires it outright, and this is cheap insurance.
            MainThreadDispatcher.Ensure();
            MainThreadDispatcher.Run(() =>
            {
                StartWatchdog();

                MobileAds.Initialize(_ => MainThreadDispatcher.Run(() =>
                {
                    StopWatchdog();

                    if (_initialized) return;
                    _initialized = true;

                    Action pending = _onInitialized;
                    _onInitialized = null;
                    pending?.Invoke();
                }));
            });
        }

        private static void StartWatchdog()
        {
            StopWatchdog();
            _watchdog = CoroutineHost.Instance.StartCoroutine(InitWatchdog());
        }

        private static void StopWatchdog()
        {
            if (_watchdog == null) return;
            if (CoroutineHost.Instance != null) CoroutineHost.Instance.StopCoroutine(_watchdog);
            _watchdog = null;
        }

        private static IEnumerator InitWatchdog()
        {
            yield return new WaitForSecondsRealtime(InitTimeoutSeconds);
            _watchdog = null;

            if (_initialized) yield break;

            Debug.LogWarning("[MobileAds] Initialize did not call back; retrying.");
            Initialize();
        }

        /// <summary>
        /// Somewhere to run a coroutine from a static class. Spawns itself on first use and
        /// survives scene loads; there is nothing to place in a scene.
        /// </summary>
        private class CoroutineHost : MonoBehaviour
        {
            private static CoroutineHost _instance;

            public static CoroutineHost Instance
            {
                get
                {
                    if (_instance != null) return _instance;

                    var go = new GameObject("MobileAdsSdkHost") { hideFlags = HideFlags.HideAndDontSave };
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CoroutineHost>();
                    return _instance;
                }
            }
        }
    }
}
