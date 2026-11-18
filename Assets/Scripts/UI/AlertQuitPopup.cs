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

        // This script is shared by the quit/retry alerts too, and only the failed-request alert
        // offers a revive by ad - so the offer only counts when a button is wired to RevivePopup.
        private readonly RewardOffer _offer = new RewardOffer(GameAnalytics.PlacementContinueGame);
        private bool _offersRevive;

        private void Start()
        {
            foreach (var button in GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (button.onClick.GetPersistentMethodName(i) == nameof(RevivePopup)
                        && button.gameObject.activeInHierarchy)
                    {
                        _offersRevive = true;
                    }
                }
            }
            if (_offersRevive) _offer.Shown();
        }

        private void OnDestroy()
        {
            _offer.Dismissed();
        }

        /// <summary>
        /// Wired to QUIT / RETRY. Leaving the level costs the life the popup warns about -
        /// charged before the scene unloads.
        /// </summary>
        public void QuitPopup()
        {
            if (_quitting) return;
            _quitting = true;
            _offer.Declined(RewardOffer.ChoiceQuit);

            if (GameController.Instance != null) GameController.Instance.QuitLevel();

            Transition.LoadLevel(scene, duration, color);
        }

        public void PlayButtonSound()
        {
            SoundController.Instance.PlayButtonClickClip();
        }
        
        public void RevivePopup()
        {
            _offer.Accepted();

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