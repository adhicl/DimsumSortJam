using System.Collections;
using Commons;
using Controllers;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Sits on the root of a shop card and takes it off the shelf once the player owns it.
    ///
    /// Only meaningful for the one-time products (<see cref="IAPCatalog.IsNonConsumable"/>) —
    /// a consumable can always be bought again, so a card for one never hides. Without this
    /// the shop would keep showing a dead "Remove Ads" button, priced "—", forever after the
    /// purchase, because an owned non-consumable stops being available to buy.
    ///
    /// Hiding is deliberately one-way: ownership never expires, and a card that deactivates
    /// itself stops receiving events. Every scene load re-runs the check from scratch.
    /// The card sits in a vertical/grid layout, so going inactive also closes the gap.
    /// </summary>
    public class ShopCard : MonoBehaviour
    {
        [Tooltip("Product id, e.g. remove_ads. Must match IAPCatalog and the Play Console.")]
        [SerializeField] private string productId;

        private IAPController _boundTo;

        public string ProductId => productId;

        private void OnEnable()
        {
            // Same lazy bind as IAPBuyButton: the shop tab can be shown before the
            // DontDestroyOnLoad controller from Splash exists, or when a scene is entered
            // directly in the Editor.
            StartCoroutine(BindWhenAvailable());
        }

        private IEnumerator BindWhenAvailable()
        {
            while (IAPController.Instance == null) yield return null;

            _boundTo = IAPController.Instance;
            _boundTo.OnOwnershipChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            StopAllCoroutines();

            if (_boundTo == null) return;
            _boundTo.OnOwnershipChanged -= Refresh;
            _boundTo = null;
        }

        private void Refresh()
        {
            if (_boundTo != null && _boundTo.IsOwned(productId)) gameObject.SetActive(false);
        }
    }
}
