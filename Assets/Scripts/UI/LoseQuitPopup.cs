using Controllers;
using Ricimi;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// "You will lose your heart and fail the level" — the confirmation between deciding to give
    /// up and actually giving up.
    ///
    /// Reached from either revive popup (out of time, out of moves) and from declining the
    /// unlock-basket offer on a stuck board. The life is spent here and nowhere else, so backing
    /// out costs the player nothing.
    /// </summary>
    public class LoseQuitPopup : MonoBehaviour
    {
        // Whichever button ran first owns the outcome; the popup lingers for half a second while
        // it animates closed, and both buttons stay live through that.
        private bool _resolved;

        public void PlaySoundButton()
        {
            SoundController.Instance.PlayButtonClickClip();
        }

        /// <summary>
        /// Wired to "Leave". Spends the life and lets the button's own SceneTransition carry the
        /// player Home — the life must be charged before the scene unloads.
        /// </summary>
        public void ConfirmLeave()
        {
            if (_resolved) return;
            _resolved = true;

            SoundController.Instance.PlayFinishOverClip();
            GameController.Instance.CommitLose();
        }

        /// <summary>
        /// Wired to the corner X. Backs out and puts the revive popup the player came from back
        /// on screen, so a mis-tap never costs a life.
        /// </summary>
        public void CancelLeave()
        {
            if (_resolved) return;
            _resolved = true;

            var popup = GetComponent<Popup>();
            if (popup != null) popup.Close();

            GameController.Instance.ReopenRevivePopup();
        }
    }
}
