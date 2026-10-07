using System;
using System.Collections.Generic;
using Controllers;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using UnityEngine;

namespace Commons
{
    /// <summary>
    /// Google Analytics for Firebase — the measurement Google Ads reads. UGS Analytics (see
    /// <see cref="GameAnalytics"/>) stays the gameplay dashboard; this exists for a different
    /// audience: Google Ads app campaigns attribute installs and optimise bids only on events that
    /// arrive through a Firebase-linked GA4 property.
    ///
    /// What it covers:
    /// <list type="bullet">
    /// <item><b>App acquisition</b> — <c>first_open</c>, <c>session_start</c> and the install
    /// referrer are logged by the native SDK on its own; nothing here has to fire them. This class
    /// only keeps collection and consent in the right state so they are kept.</item>
    /// <item><b>Conversion events</b> — the recommended <c>level_start</c> / <c>level_end</c> plus
    /// once-per-install <c>level_complete_N</c> milestones, which are what you mark as conversions
    /// in Firebase and import into Google Ads. Google Ads conversions key on the event *name*, not
    /// its parameters, which is why the milestones are separate names rather than a parameter.</item>
    /// <item><b>Advertising measurement</b> — Consent Mode v2 (<c>ad_storage</c>,
    /// <c>ad_user_data</c>, <c>ad_personalization</c>, <c>analytics_storage</c>), without which
    /// Google Ads cannot use EEA/UK traffic at all. <c>ad_impression</c> (AdMob revenue) and
    /// <c>in_app_purchase</c> (Play revenue) are *not* logged here: both are collected
    /// automatically once AdMob and Play are linked in the Firebase console, and logging them by
    /// hand as well would double the revenue Google Ads optimises against. next_step.md, §9.</item>
    /// </list>
    ///
    /// Like <see cref="GameAnalytics"/>, nothing here throws, and events logged before Firebase is
    /// ready are dropped rather than queued — the only events that matter for attribution before
    /// the first scene are the native ones, which do not pass through here.
    /// </summary>
    public static class FirebaseTracker
    {
        /// <summary>1-based level numbers whose first completion gets its own conversion event.</summary>
        private static readonly int[] MilestoneLevels = { 1, 3, 5, 10, 20, 30, 50, 75, 100 };

        private const string MilestonePrefsKeyPrefix = "fb_milestone_";

        /// <summary>True once dependencies check out and the default app exists.</summary>
        public static bool IsReady { get; private set; }

        private static bool _initStarted;

        /// <summary>
        /// Runs before the first scene so that, on a first launch, consent is pinned to denied as
        /// early as Unity code can reach — the native SDK starts before any C# does, so this cannot
        /// beat <c>first_open</c> itself; see next_step.md §9 for the manifest default that can.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (_initStarted) return;
            _initStarted = true;

            AnalyticsConsent.OnChanged -= OnAnalyticsConsentChanged;
            AnalyticsConsent.OnChanged += OnAnalyticsConsentChanged;
            ConsentController.OnConsentUpdated -= ApplyConsent;
            ConsentController.OnConsentUpdated += ApplyConsent;

            try
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled || task.Result != DependencyStatus.Available)
                    {
                        Debug.LogWarning($"[Firebase] Not available: " +
                                         $"{(task.IsFaulted ? task.Exception?.GetBaseException().Message : task.Result.ToString())}");
                        return;
                    }

                    try
                    {
                        // Touching DefaultInstance creates the app from google-services.json. It
                        // throws when that file is missing, which is the commonest setup mistake —
                        // see next_step.md §9.
                        _ = FirebaseApp.DefaultInstance;
                        IsReady = true;
                        Debug.Log("[Firebase] Analytics ready.");
                        ApplyConsent();

                        // The UMP lookup finishes after this on a first launch; re-apply once the
                        // region is known so ad_* signals reflect it. Cheap and idempotent.
                        ConsentController.WhenResolved(ApplyConsent);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Firebase] App creation failed: {e.Message}");
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] Init failed: {e.Message}");
            }
        }

        /// <summary>
        /// Re-attaches the consent listeners after <c>BootRetry</c> cleared the static events
        /// they hang off. The Firebase app is process-wide and is not re-created.
        /// </summary>
        public static void RebindAfterRestart()
        {
            if (!_initStarted)
            {
                Initialize();
                return;
            }

            ConsentController.OnConsentUpdated -= ApplyConsent;
            ConsentController.OnConsentUpdated += ApplyConsent;
            if (IsReady) ConsentController.WhenResolved(ApplyConsent);
        }

        // ── Consent ────────────────────────────────────────────────────────────────────────

        private static void OnAnalyticsConsentChanged(AnalyticsConsentState state)
        {
            if (state == AnalyticsConsentState.Denied && IsReady)
            {
                // Mirrors the UGS RequestDataDeletion path: a withdrawal also forgets the app
                // instance id, so whatever was collected can no longer be joined to new data.
                Try("reset", FirebaseAnalytics.ResetAnalyticsData);
            }

            ApplyConsent();
        }

        /// <summary>
        /// Pushes the current answer to Firebase. Safe to call any number of times.
        ///
        /// <list type="bullet">
        /// <item>Analytics answer <b>Denied</b> → collection off entirely. The player said no;
        /// cookieless consent-mode pings would still be data about them.</item>
        /// <item>Answer <b>Unknown</b> → collection on, every consent type denied. That is Consent
        /// Mode's "advanced" behaviour: Google receives identifier-free pings it can use for modelled
        /// conversions, and nothing joinable to the device.</item>
        /// <item><b>Granted</b> → <c>analytics_storage</c> granted; the three ad signals come from
        /// the player's TCF choices where GDPR applies, and are granted elsewhere.</item>
        /// </list>
        /// </summary>
        private static void ApplyConsent()
        {
            if (!IsReady) return;

            var state = AnalyticsConsent.State;

            if (state == AnalyticsConsentState.Denied)
            {
                Try("disable", () => FirebaseAnalytics.SetAnalyticsCollectionEnabled(false));
                return;
            }

            bool analytics = state == AnalyticsConsentState.Granted;
            bool adStorage = false, adUserData = false, adPersonalization = false;

            if (analytics)
            {
                var tcf = TcfPurposes.Read();
                if (tcf.GdprApplies)
                {
                    // Google's published TCF → Consent Mode mapping.
                    adStorage = tcf.Has(1);
                    adUserData = tcf.Has(1) && tcf.Has(7);
                    adPersonalization = tcf.Has(3) && tcf.Has(4);
                }
                else
                {
                    // Outside GDPR — including the US states UMP also flags, which are opt-out
                    // regimes signalled through GPP rather than TCF.
                    adStorage = adUserData = adPersonalization = true;
                }
            }

            Try("consent", () =>
            {
                FirebaseAnalytics.SetConsent(new Dictionary<ConsentType, ConsentStatus>
                {
                    { ConsentType.AnalyticsStorage, analytics ? ConsentStatus.Granted : ConsentStatus.Denied },
                    { ConsentType.AdStorage, adStorage ? ConsentStatus.Granted : ConsentStatus.Denied },
                    { ConsentType.AdUserData, adUserData ? ConsentStatus.Granted : ConsentStatus.Denied },
                    { ConsentType.AdPersonalization, adPersonalization ? ConsentStatus.Granted : ConsentStatus.Denied },
                });
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            });

            Debug.Log($"[Firebase] Consent applied: analytics={analytics}, adStorage={adStorage}, " +
                      $"adUserData={adUserData}, adPersonalization={adPersonalization}.");
        }

        // ── Events ─────────────────────────────────────────────────────────────────────────
        // Names are snake_case per GA4 convention, and must stay ≤ 40 characters. The recommended
        // events (level_start / level_end) use Firebase's own constants so GA4 recognises them.

        /// <param name="levelIndex">0-based, as everywhere else in the game.</param>
        public static void LevelStarted(int levelIndex)
        {
            Log(FirebaseAnalytics.EventLevelStart,
                new Parameter(FirebaseAnalytics.ParameterLevelName, LevelName(levelIndex)),
                new Parameter("level_number", levelIndex + 1));
        }

        public static void LevelCompleted(int levelIndex, int durationSeconds, int reviveCount)
        {
            Log(FirebaseAnalytics.EventLevelEnd,
                new Parameter(FirebaseAnalytics.ParameterLevelName, LevelName(levelIndex)),
                new Parameter("level_number", levelIndex + 1),
                new Parameter(FirebaseAnalytics.ParameterSuccess, 1),
                new Parameter("duration_seconds", durationSeconds),
                new Parameter("revive_count", reviveCount));

            LogMilestone(levelIndex + 1);
        }

        public static void LevelFailed(int levelIndex, string failReason, int durationSeconds, int reviveCount)
        {
            Log(FirebaseAnalytics.EventLevelEnd,
                new Parameter(FirebaseAnalytics.ParameterLevelName, LevelName(levelIndex)),
                new Parameter("level_number", levelIndex + 1),
                new Parameter(FirebaseAnalytics.ParameterSuccess, 0),
                new Parameter("fail_reason", failReason ?? "unknown"),
                new Parameter("duration_seconds", durationSeconds),
                new Parameter("revive_count", reviveCount));
        }

        public static void LevelRevived(int levelIndex, string reviveReason)
        {
            Log("level_revived",
                new Parameter("level_number", levelIndex + 1),
                new Parameter("revive_reason", reviveReason ?? "unknown"));
        }

        /// <summary>
        /// The rewarded-ad engagement signal. Revenue is not attached — AdMob's own automatic
        /// <c>ad_impression</c> carries that once AdMob is linked to Firebase.
        /// </summary>
        public static void RewardedAdCompleted(string placement)
        {
            Log("rewarded_ad_completed", new Parameter("ad_placement", placement ?? "unknown"));
        }

        /// <summary>
        /// A grant, without price. Revenue reaches Google Ads through the automatic
        /// <c>in_app_purchase</c> event, which carries the real store price and currency — a
        /// second, hand-built <c>purchase</c> would count every sale twice.
        /// </summary>
        public static void IapGranted(string productId)
        {
            Log("iap_granted", new Parameter(FirebaseAnalytics.ParameterItemID, productId ?? "unknown"));
        }

        /// <summary>
        /// Fires <c>level_complete_N</c> the first time level N is won on this install. Once per
        /// install because a conversion is a player reaching a depth, not a replay of it.
        /// </summary>
        private static void LogMilestone(int levelNumber)
        {
            if (Array.IndexOf(MilestoneLevels, levelNumber) < 0) return;
            // Not counted until Firebase can take it, so a level won before init is not lost.
            if (!IsReady || AnalyticsConsent.State == AnalyticsConsentState.Denied) return;

            string key = MilestonePrefsKeyPrefix + levelNumber;
            if (PlayerPrefs.GetInt(key, 0) == 1) return;

            Log($"level_complete_{levelNumber}");
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        private static string LevelName(int levelIndex) => $"level_{levelIndex + 1}";

        private static void Log(string name, params Parameter[] parameters)
        {
            if (!IsReady) return;
            Try(name, () => FirebaseAnalytics.LogEvent(name, parameters));
        }

        private static void Try(string what, Action action)
        {
            try { action(); }
            catch (Exception e) { Debug.LogWarning($"[Firebase] '{what}' failed: {e.Message}"); }
        }

        /// <summary>
        /// The IAB TCF v2 strings UMP writes to the app's default SharedPreferences. Unity's
        /// PlayerPrefs is a different file, so they are read through JNI. Off Android — and on a
        /// device where UMP has never written them — GDPR reads as not applying.
        /// </summary>
        private readonly struct TcfPurposes
        {
            public readonly bool GdprApplies;
            private readonly string _consents;

            private TcfPurposes(bool gdprApplies, string consents)
            {
                GdprApplies = gdprApplies;
                _consents = consents ?? "";
            }

            /// <summary>TCF purposes are 1-based; the string is one '0'/'1' per purpose.</summary>
            public bool Has(int purpose) => _consents.Length >= purpose && _consents[purpose - 1] == '1';

            public static TcfPurposes Read()
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    using var manager = new AndroidJavaClass("android.preference.PreferenceManager");
                    using var prefs = manager.CallStatic<AndroidJavaObject>("getDefaultSharedPreferences", activity);
                    int applies = prefs.Call<int>("getInt", "IABTCF_gdprApplies", 0);
                    string consents = prefs.Call<string>("getString", "IABTCF_PurposeConsents", "");
                    return new TcfPurposes(applies == 1, consents);
                }
                catch (Exception e)
                {
                    // Fail closed: a region we cannot read is treated as GDPR with no consents.
                    Debug.LogWarning($"[Firebase] Could not read TCF consent: {e.Message}");
                    return new TcfPurposes(true, "");
                }
#else
                return new TcfPurposes(false, "");
#endif
            }
        }
    }
}
