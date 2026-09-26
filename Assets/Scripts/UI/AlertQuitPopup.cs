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
        
        public void QuitPopup()
        {
            //added function later
            
            //reduce one health
            
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