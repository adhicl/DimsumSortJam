using System;
using System.Collections;
using System.Collections.Generic;
using Commons;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Controllers
{
    /// <summary>
    /// Runs Google's User Messaging Platform (UMP) consent flow before any ad is requested.
    /// Lives as a singleton GameObject in the Splash scene and survives scene loads.
    ///
    /// This is the privacy prompt AdMob requires: players in the EEA/UK (and regulated US
    /// states) must be shown a consent form before personalized ads can be served. Google
    /// decides whether the form is needed — outside those regions it never appears, so this
    /// is invisible to most players.
    ///
    /// Ad controllers must not call MobileAds.Initialize directly; they go through
    /// <see cref="WhenAdsAllowed"/> so initialization waits for the consent answer.
    /// </summary>
    public class ConsentController : MonoBehaviour
    {
        public static ConsentController Instance { get; private set; }

        [Tooltip("Treat the player as under the age of consent (disables personalized ads).")]
        [SerializeField] private bool tagForUnderAgeOfConsent = false;

        [Tooltip("Force a geography while testing so the form appears outside the EEA. Disabled = real behaviour.")]
        [SerializeField] private DebugGeography debugGeography = DebugGeography.Disabled;

        [Tooltip("Hashed device ids that should see the debug geography. Printed in logcat by the Ads SDK.")]
        [SerializeField] private List<string> testDeviceHashedIds = new List<string>();

        // A failed UMP lookup is retried with a doubling delay. The first launch on a flaky
        // connection is exactly when this fails, and without a retry that whole session had no
        // ads: the SDK is never initialized, so none of the ad controllers' own retries ever run.
        private const float FirstRetrySeconds = 10f;
        private const float MaxRetrySeconds = 60f;

        /// <summary>True once the consent flow has finished, whether or not a form was shown.</summary>
        public bool IsResolved { get; private set; }

        /// <summary>Fires when consent is first resolved. Late subscribers are invoked immediately.</summary>
        public event Action OnResolved;

        /// <summary>
        /// Fires every time UMP reports fresh consent information — the first resolution, each
        /// successful retry after a failed lookup, and the privacy options form closing. Static so
        /// listeners that outlive this object (the analytics prompt) need no instance.
        /// </summary>
        public static event Action OnConsentUpdated;

        // Ad initializers waiting for CanRequestAds to come true. Held rather than dropped: a
        // lookup that failed, or a player who declines and later changes their mind in Settings,
        // both flip that answer after the first resolution, and the SDK must start when they do.
        private static readonly List<Action> AdWaiters = new List<Action>();

        private float _retryDelay = FirstRetrySeconds;
        private Coroutine _retry;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Records which thread is main, before the first UMP callback can arrive on some
            // other one. Everything below marshals through it.
            MainThreadDispatcher.Ensure();

            RequestConsent();
        }

        private ConsentRequestParameters BuildRequest()
        {
            var request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = tagForUnderAgeOfConsent,
            };

            if (debugGeography != DebugGeography.Disabled)
            {
                request.ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = debugGeography,
                    TestDeviceHashedIds = testDeviceHashedIds,
                };
            }

            return request;
        }

        private void RequestConsent()
        {
            CancelRetry();

            // Both callbacks are marshalled: the plugin does not promise which thread UMP calls
            // back on, and Resolve reaches into coroutines and the ad SDK.
            ConsentInformation.Update(BuildRequest(), updateError => MainThreadDispatcher.Run(() =>
            {
                if (updateError != null)
                {
                    // Network failure or misconfiguration. Resolve anyway so the game is not
                    // stuck behind a prompt that will never arrive — CanRequestAds still gates
                    // whether ads actually load — and try again later, because a lookup that
                    // failed once is not a lookup that will fail forever.
                    Debug.LogWarning($"[Consent] Update failed: {updateError.Message}");
                    Resolve();
                    ScheduleRetry();
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError => MainThreadDispatcher.Run(() =>
                {
                    if (formError != null)
                    {
                        Debug.LogWarning($"[Consent] Form failed: {formError.Message}");
                    }

                    Debug.Log($"[Consent] Status={ConsentInformation.ConsentStatus}, " +
                              $"canRequestAds={ConsentInformation.CanRequestAds()}");

                    _retryDelay = FirstRetrySeconds;
                    Resolve();
                }));
            }));
        }

        private void ScheduleRetry()
        {
            CancelRetry();
            _retry = StartCoroutine(RetryAfter(_retryDelay));
            _retryDelay = Mathf.Min(_retryDelay * 2f, MaxRetrySeconds);
        }

        private IEnumerator RetryAfter(float seconds)
        {
            // Realtime: popups pause Time.timeScale, and Splash may be long gone by now.
            yield return new WaitForSecondsRealtime(seconds);
            _retry = null;

            // A retry that would be a no-op is skipped. Ads being allowed is what the retry is
            // for; if a cached status already permits them, there is nothing to chase.
            if (CanRequestAds() && IsRegionKnown()) yield break;

            Debug.Log("[Consent] Retrying consent lookup.");
            RequestConsent();
        }

        private void CancelRetry()
        {
            if (_retry == null) return;
            StopCoroutine(_retry);
            _retry = null;
        }

        /// <summary>
        /// Publishes fresh consent information. The first call also marks the flow resolved and
        /// fires <see cref="OnResolved"/>; every call fires <see cref="OnConsentUpdated"/> and
        /// releases any ad initializer whose turn has come.
        /// </summary>
        private void Resolve()
        {
            if (!IsResolved)
            {
                IsResolved = true;
                OnResolved?.Invoke();
            }

            OnConsentUpdated?.Invoke();
            ReleaseAdWaitersIfAllowed();
        }

        private static void ReleaseAdWaitersIfAllowed()
        {
            if (AdWaiters.Count == 0 || !CanRequestAds()) return;

            // Copy first: an action may itself call WhenAdsAllowed.
            var ready = AdWaiters.ToArray();
            AdWaiters.Clear();
            foreach (var action in ready) action();
        }

        /// <summary>
        /// True when the SDK says ads may be requested. Outside regulated regions this is
        /// true immediately; inside them it only flips once the player answers the form.
        /// </summary>
        public static bool CanRequestAds()
        {
            try
            {
                return ConsentInformation.CanRequestAds();
            }
            catch (Exception e)
            {
                // The UMP native bridge does not exist in the Editor.
                Debug.Log($"[Consent] CanRequestAds unavailable ({e.GetType().Name}); assuming true.");
                return true;
            }
        }

        /// <summary>
        /// Runs <paramref name="action"/> once ads are allowed. Ad controllers use this instead
        /// of initializing the Ads SDK directly.
        ///
        /// "Once" is the operative word. If ads are not allowed yet — the lookup has not
        /// finished, or failed, or the player declined — the action is *kept*, and runs the
        /// moment a later consent update says yes. It used to be dropped, which meant one failed
        /// lookup at launch silenced every ad for the rest of the session.
        ///
        /// If no ConsentController exists (for example entering a gameplay scene directly in
        /// the Editor), the action runs immediately so ads still work while developing.
        /// </summary>
        public static void WhenAdsAllowed(Action action)
        {
            if (action == null) return;

            if (Instance == null)
            {
                Debug.LogWarning("[Consent] No ConsentController in the scene; " +
                                 "initializing ads without waiting for consent.");
                action();
                return;
            }

            if (Instance.IsResolved && CanRequestAds())
            {
                action();
                return;
            }

            if (Instance.IsResolved)
            {
                Debug.Log("[Consent] Ads not allowed yet; the SDK will start if that changes.");
            }

            AdWaiters.Add(action);
        }

        /// <summary>
        /// Runs <paramref name="action"/> once the UMP flow has finished, regardless of what the
        /// player answered. Unlike <see cref="WhenAdsAllowed"/> this does not gate on the answer —
        /// it exists so the analytics prompt can wait for UMP to establish which region the player
        /// is in before deciding whether to ask anything at all.
        ///
        /// Runs immediately when there is no ConsentController (entering a gameplay scene directly
        /// in the Editor), which leaves the region reading at its Unknown default.
        /// </summary>
        public static void WhenResolved(Action action)
        {
            if (action == null) return;

            if (Instance == null)
            {
                action();
                return;
            }

            if (Instance.IsResolved) action();
            else Instance.OnResolved += action;
        }

        /// <summary>
        /// True when the player is somewhere a privacy prompt is legally expected — the EEA, UK,
        /// Switzerland, and the regulated US states. Google decides this, which is why it is read
        /// back off UMP rather than guessed from a locale or a timezone.
        ///
        /// <c>Required</c> means a form is owed and unanswered; <c>Obtained</c> means one was owed
        /// and has been answered. Both mean "this player is in a regulated region".
        /// <c>NotRequired</c> is most of the world.
        ///
        /// Only meaningful after the flow has resolved — see <see cref="WhenResolved"/>. Before
        /// that it reads Unknown and this returns false.
        ///
        /// Note this is Google's determination for *ad* consent. It is the best regional signal
        /// available in the project, but it is not a legal opinion about analytics — see
        /// next_step.md, "Analytics consent".
        /// </summary>
        public static bool IsConsentRequiredRegion()
        {
            try
            {
                var status = ConsentInformation.ConsentStatus;
                return status == ConsentStatus.Required || status == ConsentStatus.Obtained;
            }
            catch (Exception)
            {
                // No UMP native bridge in the Editor.
                return false;
            }
        }

        /// <summary>
        /// True when UMP has actually established where the player is. False means the question is
        /// still open — the update failed (offline, or a missing published consent message) and
        /// <see cref="ConsentStatus"/> is sitting at its <c>Unknown</c> default.
        ///
        /// The distinction matters because <see cref="IsConsentRequiredRegion"/> answers false for
        /// both "definitely outside the EEA" and "no idea", and those must not be treated alike:
        /// silently opting in a player whose region could not be determined is exactly the case
        /// GDPR is about. Callers that are deciding whether to collect data must check this first
        /// and defer rather than guess — the flow resolves anyway so the game is never stuck, and
        /// the next launch gets another chance to determine it.
        /// </summary>
        public static bool IsRegionKnown()
        {
            try
            {
                return ConsentInformation.ConsentStatus != ConsentStatus.Unknown;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// True when the player must be offered a way to change their choice. Show a
        /// "Privacy options" entry in Settings whenever this is true — it is a GDPR
        /// requirement, not a nicety.
        /// </summary>
        public static bool IsPrivacyOptionsRequired()
        {
            try
            {
                return ConsentInformation.PrivacyOptionsRequirementStatus
                       == PrivacyOptionsRequirementStatus.Required;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Reopens the consent form so the player can change their choice. The answer may now
        /// allow ads that were refused at launch, so the waiters are re-evaluated on close.
        /// </summary>
        public static void ShowPrivacyOptions(Action onDismissed = null)
        {
            ConsentForm.ShowPrivacyOptionsForm(error => MainThreadDispatcher.Run(() =>
            {
                if (error != null) Debug.LogWarning($"[Consent] Privacy form failed: {error.Message}");

                OnConsentUpdated?.Invoke();
                ReleaseAdWaitersIfAllowed();
                onDismissed?.Invoke();
            }));
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            CancelRetry();
            Instance = null;
        }
    }
}
