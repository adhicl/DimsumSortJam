using Controllers;
using Ricimi;
using UnityEngine;

namespace UI
{
    public class OutOfMovePopup : MonoBehaviour
    {
        public void PlaySoundButton()
        {
            SoundController.Instance.PlayButtonClickClip();
        }

        public void PlayFinishSoundOverButton()
        {
            SoundController.Instance.PlayFinishOverClip();
        }

        // Wired to the "REVIVE" (video) button. Watches a rewarded ad and, on reward,
        // adds time and resumes play. If the player declines or no ad is ready the popup
        // stays open so they can still choose to leave.
        public void ReviveByAd()
        {
            SoundController.Instance.PlayButtonClickClip();

            void GrantRevive()
            {
                GameController.Instance.ReviveWithTime();
                GetComponent<Popup>().Close();
            }

            if (RewardedAdController.Instance != null)
            {
                RewardedAdController.Instance.ShowAd(GrantRevive);
            }
            else
            {
                GrantRevive();
            }
        }

        // Wired to the "Leave" button. Commits to the loss.
        public void LeaveGiveUp()
        {
            SoundController.Instance.PlayFinishOverClip();
            GetComponent<Popup>().Close();
            GameController.Instance.ConfirmLose();
        }
    }
}
