using Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Reopens the Google UMP consent form so a player can change their ad-personalization
    /// choice.
    ///
    /// GDPR requires this entry point to exist for players who were shown a consent form —
    /// but only for them. The button hides itself everywhere else (most of the world), so it
    /// does not clutter Settings for players who never saw a prompt.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PrivacyOptionsButton : MonoBehaviour
    {
        [Tooltip("Object to hide/show — defaults to this GameObject. Set to a parent row if the button sits in a layout group.")]
        [SerializeField] private GameObject visibilityTarget;

        [Tooltip("After the ad consent form closes, also re-ask about analytics. This is the " +
                 "player's route to withdrawing analytics consent, so turning it off removes " +
                 "the only way back unless you wire a dedicated button.")]
        [SerializeField] private bool alsoAskAnalyticsConsent = true;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Show);
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            // Requirement status is only meaningful after ConsentController has run its
            // update; before that it reads Unknown, which correctly hides the button.
            // The popup is instantiated fresh on every open, so this re-evaluates each time.
            bool required = ConsentController.IsPrivacyOptionsRequired();
            var target = visibilityTarget != null ? visibilityTarget : gameObject;
            target.SetActive(required);
        }

        /// <summary>
        /// Opens the ad consent form, then the analytics prompt behind it.
        ///
        /// Both privacy choices sit behind this one entry rather than getting a Settings row each:
        /// the button is already visible in exactly the regions where a prompt is owed, and a
        /// player looking for "Privacy options" is looking for all of it. The forms are sequential,
        /// not stacked — the analytics prompt is raised from the UMP form's dismissal callback.
        /// </summary>
        private void Show()
        {
            ConsentController.ShowPrivacyOptions(() =>
            {
                Refresh();
                if (alsoAskAnalyticsConsent) AnalyticsConsentPrompt.Ask();
            });
        }
    }
}
