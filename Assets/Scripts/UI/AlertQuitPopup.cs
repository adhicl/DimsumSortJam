using Commons;
using Controllers;
using Ricimi;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class AlertQuitPopup : MonoBehaviour
    {
        public string scene = "Home";
        public float duration = 1.0f;
        public Color color = Color.black;
        
        // The popup lingers while it animates closed; a second tap must not charge or load twice.
        private bool _quitting;

        /// <summary>
        /// Wired to QUIT / RETRY. Leaving the level costs the life the popup warns about -
        /// charged before the scene unloads.
        /// </summary>
        public void QuitPopup()
        {
            if (_quitting) return;
            _quitting = true;

            if (GameController.Instance != null) GameController.Instance.QuitLevel();

            Transition.LoadLevel(scene, duration, color);
        }

        public void PlayButtonSound()
        {
            SoundController.Instance.PlayButtonClickClip();
        }
        
        public void RevivePopup()
        {
            // Watch a rewarded ad to revive; only continue once the reward is earned.
            void GrantRevive()
            {
                GameController.Instance.ContinueGame();
                this.GetComponent<Popup>().Close();
            }

            if (RewardedAdController.Instance != null)
            {
                RewardedAdController.Instance.ShowAd(GrantRevive, placement: GameAnalytics.PlacementContinueGame);
            }
            else
            {
                GrantRevive();
            }
        }

    }
}