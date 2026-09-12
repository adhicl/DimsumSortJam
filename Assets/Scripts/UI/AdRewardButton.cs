using System.Collections;
using Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Drop this on any "watch an ad" button. It greys the button out while no rewarded ad is
    /// loaded and lights it back up on its own the moment one lands, so the player is never
    /// left tapping an option that silently does nothing.
    ///
    /// Only handles availability — the button's own onClick still does the work.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AdRewardButton : MonoBehaviour
    {
        [Tooltip("Alpha applied while the option is unavailable. Leave the CanvasGroup empty to dim nothing.")]
        [SerializeField] private float disabledAlpha = 0.45f;

        [Tooltip("Optional — dimmed alongside the button. Found on this object if left empty.")]
        [SerializeField] private CanvasGroup canvasGroup;

        // Polled rather than event-driven because the Ads SDK gives no "a load finished"
        // callback we can subscribe to here. Quarter-second is well under human reaction
        // time and keeps IsReady (a JNI hop on Android) off the per-frame path.
        private const float PollInterval = 0.25f;

        private Button _button;

        /// <summary>
        /// Held down by the owner once the reward has been taken, so the closing animation is
        /// not a window in which the button can be pressed a second time.
        /// </summary>
        public bool Locked { get; set; }

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            Refresh();
            StartCoroutine(PollWhileVisible());
        }

        private IEnumerator PollWhileVisible()
        {
            var wait = new WaitForSeconds(PollInterval);
            while (true)
            {
                yield return wait;
                Refresh();
            }
        }

        public void Refresh()
        {
            // The controller lives in Splash and follows the player everywhere, so it is only
            // null when a scene was played directly in the Editor. ShowAd's callers grant the
            // reward without an ad in that case, so the button must stay live there.
            bool available = !Locked
                             && (RewardedAdController.Instance == null
                                 || RewardedAdController.Instance.IsAvailable);

            if (_button != null) _button.interactable = available;
            if (canvasGroup != null) canvasGroup.alpha = available ? 1f : disabledAlpha;
        }
    }
}
