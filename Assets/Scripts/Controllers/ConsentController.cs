using System;
using System.Collections.Generic;
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

        /// <summary>True once the consent flow has finished, whether or not a form was shown.</summary>
        public bool IsResolved { get; private set; }

        /// <summary>Fires when consent is resolved. Late subscribers are invoked immediately.</summary>
        public event Action OnResolved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            RequestConsent();
        }

        private void RequestConsent()
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

            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null)
                {
                    // Network failure or misconfiguration. Resolve anyway so the game is not
                    // stuck behind a prompt that will never arrive; CanRequestAds still gates
                    // whether ads actually load.
                    Debug.LogWarning($"[Consent] Update failed: {updateError.Message}");
                    Resolve();
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                    {
                        Debug.LogWarning($"[Consent] Form failed: {formError.Message}");
                    }

                    Debug.Log($"[Consent] Status={ConsentInformation.ConsentStatus}, " +
                              $"canRequestAds={ConsentInformation.CanRequestAds()}");
                    Resolve();
                });
            });
        }

        private void Resolve()
        {
            IsResolved = true;
            OnResolved?.Invoke();
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

            if (Instance.IsResolved)
            {
                InvokeIfAllowed(action);
                return;
            }

            Instance.OnResolved += () => InvokeIfAllowed(action);
        }

        private static void InvokeIfAllowed(Action action)
        {
            if (!CanRequestAds())
            {
                Debug.Log("[Consent] Player declined; not requesting ads.");
                return;
            }

            action();
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

        /// <summary>Reopens the consent form so the player can change their choice.</summary>
        public static void ShowPrivacyOptions(Action onDismissed = null)
        {
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                if (error != null) Debug.LogWarning($"[Consent] Privacy form failed: {error.Message}");
                onDismissed?.Invoke();
            });
        }
    }
}
