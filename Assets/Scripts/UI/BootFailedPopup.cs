using Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Shown when the boot gets stuck on Splash and the game cannot go on.
    ///
    /// Two ways out and no third: retry the whole boot, or quit. There is deliberately no close
    /// button and no tap-outside-to-dismiss - dismissing would leave the player looking at a
    /// loading bar that is never going to finish, which is the situation this exists to end.
    ///
    /// Wording and artwork live on the prefab, not here. This used to push a title and message
    /// into the labels on enable, which meant the prefab showed one thing in the editor and
    /// something else at runtime, and every copy change was a code change.
    /// </summary>
    public class BootFailedPopup : MonoBehaviour
    {
        [Tooltip("Retries the whole boot: tears the services down and reloads Splash.")]
        [SerializeField] private Button retryButton;

        [Tooltip("Closes the game.")]
        [SerializeField] private Button quitButton;

        // The buttons stay live through the closing animation, and a second tap would try to
        // restart a restart.
        private bool _answered;

        private void Awake()
        {
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
        }

        /// <summary>Wired to the retry button. Falls through to quitting when the retry cannot start.</summary>
        public void Retry()
        {
            if (_answered) return;
            _answered = true;

            PlayClick();

            if (BootRetry.TryRestart()) return;

            // Nothing left to try: the player was told to restart, and the game cannot do it
            // itself, so close rather than sit on a dead loading screen.
            Debug.LogError("[BootFailedPopup] Retry could not start; quitting instead.");
            BootRetry.QuitApp();
        }

        /// <summary>Wired to the quit button.</summary>
        public void Quit()
        {
            if (_answered) return;
            _answered = true;

            PlayClick();
            BootRetry.QuitApp();
        }

        private static void PlayClick()
        {
            // The sound controller belongs to the game scenes and does not exist during Splash;
            // a missing click must not swallow the button.
            if (SoundController.Instance != null) SoundController.Instance.PlayButtonClickClip();
        }
    }
}
