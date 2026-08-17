using Commons;
using Controllers;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Shown when the clock runs out, before the loss is committed. Two ways to keep playing —
    /// watch a rewarded ad, or pay <see cref="GameSetting.ReviveCost"/> coins — plus Leave,
    /// which gives up.
    ///
    /// Each option greys out rather than going dead when it cannot be used, matching
    /// <see cref="UnlockBasketPopup"/>.
    /// </summary>
    public class OutOfMovePopup : MonoBehaviour
    {
        [Tooltip("The 'pay coins' revive button. Greys out when the player cannot afford it.")]
        [SerializeField] private Button coinButton;
        [SerializeField] private CanvasGroup coinGroup;

        [Tooltip("Availability gate on the REVIVE (ad) button.")]
        [SerializeField] private AdRewardButton watchAdGate;

        [Tooltip("Label on the coin button. Filled in on open from GameSetting.ReviveCost.")]
        [SerializeField] private TextMeshProUGUI costText;

        [Tooltip("Coin balance in the credits row. Kept in step with the wallet.")]
        [SerializeField] private TextMeshProUGUI balanceText;

        [Tooltip("Alpha applied to an option that is currently unavailable.")]
        [SerializeField] private float disabledAlpha = 0.45f;

        // Popup.Close() destroys the popup half a second later, so without this the buttons
        // stay live through the closing animation and a fast double tap pays twice.
        private bool _resolved;

        private void OnEnable()
        {
            if (costText != null) costText.text = GameSetting.ReviveCost.ToString("N0");
            Refresh();
        }

        // No polling needed on the coin side: the balance can only change by buying this very
        // revive, which closes the popup. The ad side polls itself via AdRewardButton.
        private void Refresh()
        {
            GameSetting setting = GameController.Instance != null ? GameController.Instance.GameSetting : null;
            bool canAfford = setting != null && setting.totalGold >= GameSetting.ReviveCost;

            if (coinButton != null) coinButton.interactable = canAfford;
            if (coinGroup != null) coinGroup.alpha = canAfford ? 1f : disabledAlpha;

            // The credits row is decorative in the base prefab; here it is the number the
            // player is about to spend from, so it has to be real.
            if (balanceText != null && setting != null)
                balanceText.text = setting.totalGold.ToString("N0");
        }

        private void RefreshAdGate()
        {
            if (watchAdGate != null) watchAdGate.Refresh();
        }

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
        // stays open so they can still choose another option.
        public void ReviveByAd()
        {
            if (_resolved) return;
            SoundController.Instance.PlayButtonClickClip();

            if (RewardedAdController.Instance != null)
            {
                // If the loaded ad expired since the gate last polled, re-greying the button
                // is better feedback than nothing happening.
                RewardedAdController.Instance.ShowAd(GrantRevive, RefreshAdGate);
            }
            else
            {
                GrantRevive();
            }
        }

        /// <summary>Wired to the coin button. Charges the cost and revives.</summary>
        public void ReviveByCoin()
        {
            if (_resolved) return;
            SoundController.Instance.PlayButtonClickClip();

            GameSetting setting = GameController.Instance != null ? GameController.Instance.GameSetting : null;

            // Check and charge are the same call, so a fast double tap cannot spend twice.
            if (setting == null || !setting.TrySpendGold(GameSetting.ReviveCost))
            {
                Refresh();
                return;
            }

            GrantRevive();
        }

        private void GrantRevive()
        {
            _resolved = true;

            // Dead the options on the way out, so the closing animation is not a window in
            // which a second tap can be registered.
            if (watchAdGate != null) watchAdGate.Locked = true;
            RefreshAdGate();
            if (coinButton != null) coinButton.interactable = false;
            if (coinGroup != null) coinGroup.alpha = disabledAlpha;

            GameController.Instance.ReviveWithTime();
            GetComponent<Popup>().Close();
        }

        // Wired to the "Leave" button and to the corner X. Neither loses the level outright —
        // both hand over to the confirmation popup, which is where the life is actually spent.
        public void LeaveGiveUp()
        {
            if (_resolved) return;
            _resolved = true;

            SoundController.Instance.PlayButtonClickClip();
            GetComponent<Popup>().Close();
            GameController.Instance.ShowLoseConfirm();
        }
    }
}
