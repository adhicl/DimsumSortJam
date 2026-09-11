using System;
using UnityEngine;

namespace Commons
{
    /// <summary>The player's answer to the analytics prompt. Persisted, so it is asked once.</summary>
    public enum AnalyticsConsentState
    {
        /// <summary>Never asked. Nothing is collected while the answer is unknown.</summary>
        Unknown = 0,
        Granted = 1,
        Denied = 2
    }

    /// <summary>
    /// Owns whether the player has agreed to analytics collection. Storage only — the decision to
    /// *ask* lives in <c>AnalyticsConsentPrompt</c>, and acting on the answer lives in
    /// <c>GameServicesController</c>, which is the only thing that touches the Analytics SDK.
    ///
    /// Deliberately defaults to <see cref="AnalyticsConsentState.Unknown"/> rather than Granted:
    /// an unanswered prompt must not collect. A player who is never asked (outside the regions
    /// that require a prompt) is moved to Granted explicitly by the prompt logic, so "we are
    /// collecting" is always a recorded decision rather than the absence of one.
    /// </summary>
    public static class AnalyticsConsent
    {
        private const string Key = "analytics_consent";

        /// <summary>
        /// Fires when the answer changes — never on load, only on an actual change. The argument is
        /// the new state. <c>GameServicesController</c> subscribes to start or purge accordingly.
        /// </summary>
        public static event Action<AnalyticsConsentState> OnChanged;

        public static AnalyticsConsentState State =>
            (AnalyticsConsentState)PlayerPrefs.GetInt(Key, (int)AnalyticsConsentState.Unknown);

        public static bool IsGranted => State == AnalyticsConsentState.Granted;

        /// <summary>False until the player has been asked, or until the prompt decided not to ask.</summary>
        public static bool HasAnswer => State != AnalyticsConsentState.Unknown;

        /// <summary>
        /// Records the player's answer. A no-op when the answer has not actually changed, so
        /// reopening the prompt and tapping the same button does not re-trigger a data purge.
        /// </summary>
        public static void Set(bool granted)
        {
            var next = granted ? AnalyticsConsentState.Granted : AnalyticsConsentState.Denied;
            if (State == next) return;

            PlayerPrefs.SetInt(Key, (int)next);
            PlayerPrefs.Save();

            Debug.Log($"[Consent] Analytics consent set to {next}.");
            OnChanged?.Invoke(next);
        }

        /// <summary>
        /// Forgets the answer so the prompt appears again on the next launch. For testing — there
        /// is no player-facing route to this, because "change your mind" is the prompt itself.
        /// </summary>
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
