using System;
using Commons;
using Controllers;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Offered when the player taps a closed basket. Two ways out: pay
    /// <see cref="GameSetting.BasketUnlockCost"/> coins, or watch a rewarded ad.
    ///
    /// Either option greys out rather than disappearing when it cannot be used, so the player
    /// can always see both routes exist. Spawned by <see cref="GameController.ShowUnlockBasketPopup"/>,
    /// which hands over the callback that actually opens the basket.
    /// </summary>
    public class UnlockBasketPopup : MonoBehaviour
    {
        [Tooltip("The 'watch an ad' button. Its own AdRewardButton greys it while no ad is loaded.")]
        [SerializeField] private Button watchAdButton;

        [Tooltip("Availability gate on the ad button. Found on that button if left empty.")]
        [SerializeField] private AdRewardButton watchAdGate;

        [Tooltip("The 'pay coins' button. Greys out when the player cannot afford the cost.")]
        [SerializeField] private Button coinButton;
        [SerializeField] private CanvasGroup coinGroup;

        [Tooltip("Label on the coin button. Filled in on open from GameSetting.BasketUnlockCost.")]
        [SerializeField] private TextMeshProUGUI costText;

        [Tooltip("Coin balance shown in the popup's credits row. Kept in step with the wallet.")]
        [SerializeField] private TextMeshProUGUI balanceText;

        [Tooltip("Alpha applied to an option that is currently unavailable.")]
        [SerializeField] private float disabledAlpha = 0.45f;

        private Action _onUnlock;

        // Popup.Close() only destroys the popup half a second later, so without this the
        // buttons stay live through the closing animation and a fast double tap pays twice.
        private bool _resolved;

        /// <summary>
        /// True once the player has taken one of the two unlock routes. Lets the caller tell a
        /// deliberate dismissal apart from a successful unlock on close.
        /// </summary>
        public bool Unlocked => _resolved;

        /// <summary>
        /// Hands the popup the action that opens the basket. Called before <see cref="Popup.Open"/>.
        /// </summary>
        public void Setup(Action onUnlock)
        {
            _onUnlock = onUnlock;
        }

        private void Awake()
        {
            // Wired here rather than as prefab UnityEvents: both buttons are already serialized
            // for the enable/disable logic, so there is nothing to gain from a second binding
            // that can silently drift out of sync.
            if (watchAdButton != null)
            {
                watchAdButton.onClick.AddListener(UnlockByAd);
                if (watchAdGate == null) watchAdGate = watchAdButton.GetComponent<AdRewardButton>();
            }
            if (coinButton != null) coinButton.onClick.AddListener(UnlockByCoin);
        }

        private void OnEnable()
        {
            if (costText != null) costText.text = GameSetting.BasketUnlockCost.ToString("N0");
            Refresh();
        }

        // The coin side needs no polling: the balance can only change by buying this very
        // unlock, which closes the popup. The ad side polls itself via AdRewardButton.
        private void Refresh()
        {
            GameSetting setting = GameController.Instance != null ? GameController.Instance.GameSetting : null;
            bool canAfford = setting != null && setting.totalGold >= GameSetting.BasketUnlockCost;

            SetOption(coinButton, coinGroup, canAfford);

            // The credits row is decorative in the parent prefab; here it is the number the
            // player is about to spend from, so it has to be real.
            if (balanceText != null && setting != null)
                balanceText.text = setting.totalGold.ToString("N0");
        }

        private void SetOption(Button button, CanvasGroup group, bool available)
        {
            if (button != null) button.interactable = available;
            if (group != null) group.alpha = available ? 1f : disabledAlpha;
        }

        /// <summary>Wired to the ad button. Unlocks once the reward is earned.</summary>
        public void UnlockByAd()
        {
            if (_resolved) return;
            SoundController.Instance.PlayButtonClickClip();

            if (RewardedAdController.Instance != null)
            {
                // onUnavailable matters here: if the loaded ad expired since the gate last
                // polled, re-greying the button is better feedback than nothing happening.
                RewardedAdController.Instance.ShowAd(Grant, RefreshAdGate, GameAnalytics.PlacementUnlockBasket);
            }
            else
            {
                Grant();
            }
        }

        /// <summary>Wired to the coin button. Charges the cost and unlocks.</summary>
        public void UnlockByCoin()
        {
            if (_resolved) return;
            SoundController.Instance.PlayButtonClickClip();

            GameSetting setting = GameController.Instance != null ? GameController.Instance.GameSetting : null;

            // Check and charge are the same call, so a fast double tap cannot spend twice.
            if (setting == null || !setting.TrySpendGold(GameSetting.BasketUnlockCost))
            {
                Refresh();
                return;
            }

            Grant();
        }

        /// <summary>Wired to the cancel button alongside Popup.Close.</summary>
        public void PlaySoundButton()
        {
            SoundController.Instance.PlayButtonClickClip();
        }

        private void RefreshAdGate()
        {
            if (watchAdGate != null) watchAdGate.Refresh();
        }

        private void Grant()
        {
            _resolved = true;

            // Dead the buttons on the way out, so the closing animation is not a window
            // in which a second tap can be registered.
            if (watchAdGate != null) watchAdGate.Locked = true;
            RefreshAdGate();
            SetOption(coinButton, coinGroup, false);

            Action onUnlock = _onUnlock;
            _onUnlock = null;
            if (onUnlock != null) onUnlock();

            Close();
        }

        private void Close()
        {
            var popup = GetComponent<Popup>();
            if (popup != null) popup.Close();
        }
    }
}
