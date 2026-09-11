using Commons;
using Controllers;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Offered when the player taps a power-up they have none of. Two ways out: pay
    /// <see cref="GameSetting.PowerupCost"/> coins, or watch a rewarded ad — the same pair
    /// <see cref="UnlockBasketPopup"/> offers, so the two read as one shop.
    ///
    /// It sells the one power-up the player reached for rather than putting a shelf in front of
    /// them, so the popup is told its slot by <see cref="GameController.ShowBuyPowerupPopup"/>
    /// and dresses itself from <see cref="entries"/>. Buying does not fire the power-up: the
    /// popup closes and the button lights up with its new count, so the player spends it when
    /// they mean to.
    /// </summary>
    public class BuyPowerupPopup : MonoBehaviour
    {
        /// <summary>
        /// The art and words for one power-up. Filled in on the prefab, one entry per slot, so
        /// adding a fifth power-up is an Inspector edit rather than a code change.
        /// </summary>
        [System.Serializable]
        public class PowerupEntry
        {
            [Tooltip("Which power-up this describes: 1 package, 2 magnifier, 3 shuffle, 4 timer.")]
            public int slot = 1;

            [Tooltip("Shown on the popup title bar. Keep it to two or three words.")]
            public string title = "POWER-UP";

            [Tooltip("What it does, in one short sentence.")]
            [TextArea]
            public string description = string.Empty;

            [Tooltip("The same icon the top bar button uses, so the popup is obviously about that button.")]
            public Sprite icon;
        }

        [Tooltip("One entry per power-up slot. Matched on PowerupEntry.slot, not on order.")]
        [SerializeField] private PowerupEntry[] entries;

        [Tooltip("Title bar label.")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("The one-line explanation of what the power-up does.")]
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Tooltip("Big power-up icon in the body of the popup.")]
        [SerializeField] private Image iconImage;

        [Tooltip("The 'watch an ad' button. Its own AdRewardButton greys it while no ad is loaded.")]
        [SerializeField] private Button watchAdButton;

        [Tooltip("Availability gate on the ad button. Found on that button if left empty.")]
        [SerializeField] private AdRewardButton watchAdGate;

        [Tooltip("The 'pay coins' button. Greys out when the player cannot afford the cost.")]
        [SerializeField] private Button coinButton;
        [SerializeField] private CanvasGroup coinGroup;

        [Tooltip("Label on the coin button. Filled in on open from GameSetting.PowerupCost.")]
        [SerializeField] private TextMeshProUGUI costText;

        [Tooltip("Coin balance shown in the popup's credits row. Kept in step with the wallet.")]
        [SerializeField] private TextMeshProUGUI balanceText;

        [Tooltip("Alpha applied to an option that is currently unavailable.")]
        [SerializeField] private float disabledAlpha = 0.45f;

        private int _slot = 1;

        // Popup.Close() only destroys the popup half a second later, so without this the buttons
        // stay live through the closing animation and a fast double tap pays twice.
        private bool _resolved;

        /// <summary>True once the player has bought, by either route.</summary>
        public bool Bought => _resolved;

        /// <summary>
        /// Tells the popup which power-up it is selling. Called before <see cref="Popup.Open"/>.
        /// </summary>
        public void Setup(int slot)
        {
            _slot = slot;
            Dress();
        }

        private void Awake()
        {
            // Wired here rather than as prefab UnityEvents: both buttons are already serialized
            // for the enable/disable logic, so there is nothing to gain from a second binding
            // that can silently drift out of sync.
            if (watchAdButton != null)
            {
                watchAdButton.onClick.AddListener(BuyByAd);
                if (watchAdGate == null) watchAdGate = watchAdButton.GetComponent<AdRewardButton>();
            }
            if (coinButton != null) coinButton.onClick.AddListener(BuyByCoin);
        }

        private void OnEnable()
        {
            if (costText != null) costText.text = GameSetting.PowerupCost.ToString("N0");
            Dress();
            Refresh();
        }

        /// <summary>Puts the current slot's icon, name and explanation on screen.</summary>
        private void Dress()
        {
            PowerupEntry entry = FindEntry(_slot);
            if (entry == null) return;

            if (titleText != null) titleText.text = entry.title;
            if (descriptionText != null) descriptionText.text = entry.description;

            // A slot with no icon assigned would otherwise show whatever the prefab was saved
            // with, which is a different power-up's art - worse than showing none.
            if (iconImage != null)
            {
                iconImage.sprite = entry.icon;
                iconImage.enabled = entry.icon != null;
            }
        }

        private PowerupEntry FindEntry(int slot)
        {
            if (entries == null) return null;
            foreach (PowerupEntry entry in entries)
            {
                if (entry != null && entry.slot == slot) return entry;
            }
            return null;
        }

        // The coin side needs no polling: the balance can only change by making this very
        // purchase, which closes the popup. The ad side polls itself via AdRewardButton.
        private void Refresh()
        {
            GameSetting setting = GameController.Instance != null ? GameController.Instance.GameSetting : null;
            bool canAfford = setting != null && setting.totalGold >= GameSetting.PowerupCost;

            SetOption(coinButton, coinGroup, canAfford);

            if (balanceText != null && setting != null)
                balanceText.text = setting.totalGold.ToString("N0");
        }

        private void SetOption(Button button, CanvasGroup group, bool available)
        {
            if (button != null) button.interactable = available;
            if (group != null) group.alpha = available ? 1f : disabledAlpha;
        }

        /// <summary>Wired to the ad button. Grants the power-up once the reward is earned.</summary>
        public void BuyByAd()
        {
            if (_resolved) return;
            SoundController.Instance.PlayButtonClickClip();

            if (RewardedAdController.Instance != null)
            {
                // onUnavailable matters here: if the loaded ad expired since the gate last
                // polled, re-greying the button is better feedback than nothing happening.
                RewardedAdController.Instance.ShowAd(Grant, RefreshAdGate, GameAnalytics.PlacementBuyPowerUp);
            }
            else
            {
                Grant();
            }
        }

        /// <summary>Wired to the coin button. Charges the cost and grants the power-up.</summary>
        public void BuyByCoin()
        {
            if (_resolved) return;
            SoundController.Instance.PlayButtonClickClip();

            GameSetting setting = GameController.Instance != null ? GameController.Instance.GameSetting : null;

            // Check and charge are the same call, so a fast double tap cannot spend twice.
            if (setting == null || !setting.TrySpendGold(GameSetting.PowerupCost))
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

            GameSetting setting = GameController.Instance != null ? GameController.Instance.GameSetting : null;

            if (setting != null) setting.AddPowerup(_slot, GameSetting.PowerupPurchaseAmount);

            // Dead the buttons on the way out, so the closing animation is not a window in which
            // a second tap can be registered.
            if (watchAdGate != null) watchAdGate.Locked = true;
            RefreshAdGate();
            SetOption(coinButton, coinGroup, false);

            var popup = GetComponent<Popup>();

            // Celebrate on the top bar, but only once the popup is out of the way - Popup fires
            // onClose after the closing animation, not at the tap. Flying the icons at the moment
            // the power-up is credited would send them behind the popup the player is still
            // looking at. The slot is copied out because this component is destroyed along with
            // the popup before the callback runs.
            int slot = _slot;
            if (popup == null) return;

            popup.onClose += () => PlayTopBar.RequestPunch(slot);
            popup.Close();
        }
    }
}
