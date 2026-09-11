using Commons;
using UnityEngine;

namespace Controllers
{
    /// <summary>
    /// Decides whether to ask the player about analytics, and asks.
    ///
    /// The rule: only players in a region that requires a privacy prompt are asked. Everyone else
    /// is opted in without a dialog, which is what the game did before consent existed and what
    /// the rest of the industry does outside the EEA. <see cref="PrivacyOptionsButton"/> already
    /// follows exactly this shape for the ad consent form — it shows itself only where a form was
    /// required — so the two privacy surfaces behave consistently.
    ///
    /// Region comes from UMP via <see cref="ConsentController.IsConsentRequiredRegion"/>, so the
    /// prompt has to wait for that flow to resolve before it can decide anything.
    /// </summary>
    public static class AnalyticsConsentPrompt
    {
        private const string Title = "Help us improve the game?";

        private const string Message =
            "We'd like to collect anonymous gameplay data — levels played, where players get " +
            "stuck, which rewards get used — so we can make the game better.\n\n" +
            "This never includes your name, email, or contact details.\n\n" +
            "You can change this any time from Privacy options in Settings.";

        private const string Allow = "Allow";
        private const string Decline = "No thanks";

        /// <summary>
        /// Asks if the player has not answered and their region requires it. Waits for UMP first.
        /// Safe to call more than once — an answered player is never re-prompted.
        /// </summary>
        public static void AskIfNeeded()
        {
            if (AnalyticsConsent.HasAnswer) return;

            ConsentController.WhenResolved(() =>
            {
                // Re-checked inside the callback: UMP resolving can take seconds, and the answer
                // may have arrived from the Settings route in the meantime.
                if (AnalyticsConsent.HasAnswer) return;

                // The flow resolves even when the UMP update fails — offline, or no consent
                // message published — and leaves the region at Unknown. Storing an answer here
                // would opt the player in permanently on the strength of a lookup that never
                // happened, and an EEA player whose first launch was offline would never be asked
                // again. Collect nothing and try again next launch instead.
                if (!ConsentController.IsRegionKnown())
                {
                    Debug.Log("[Consent] Region undetermined; not collecting analytics this " +
                              "session and leaving the question open.");
                    return;
                }

                if (!ConsentController.IsConsentRequiredRegion())
                {
                    Debug.Log("[Consent] Analytics prompt not required in this region; opting in.");
                    AnalyticsConsent.Set(true);
                    return;
                }

                Ask();
            });
        }

        /// <summary>
        /// Shows the prompt unconditionally. This is the "change your mind" route, reached from
        /// Privacy options in Settings — the GDPR right to withdraw needs a way back in that does
        /// not depend on the player never having answered.
        /// </summary>
        public static void Ask()
        {
            NativeDialog.ShowConfirm(
                Title, Message, Allow, Decline,
                granted => AnalyticsConsent.Set(granted),

                // No dialog means no informed answer, so the safe fallback is "no". In the Editor
                // that would leave analytics off and make every event a silent no-op, which is
                // useless for verifying call sites — so the Editor opts in instead. Device
                // behaviour is the strict one.
#if UNITY_EDITOR
                fallbackAnswer: true
#else
                fallbackAnswer: false
#endif
            );
        }
    }
}
