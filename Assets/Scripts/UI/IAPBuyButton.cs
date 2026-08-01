using System.Collections;
using Commons;
using Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Drop this on a shop button, type the product id, and it handles the rest: shows the
    /// store's localized price, stays disabled until the catalog loads, and starts the
    /// purchase on click.
    ///
    /// Product ids must come from <see cref="IAPCatalog"/> — see next_step.md.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class IAPBuyButton : MonoBehaviour
    {
        [Tooltip("Product id, e.g. coins_500. Must match IAPCatalog and the Play Console.")]
        [SerializeField] private string productId;

        [Tooltip("Optional — filled with the store's localized price once products load.")]
        [SerializeField] private TextMeshProUGUI priceText;

        [Tooltip("Shown while the purchase is in flight.")]
        [SerializeField] private GameObject loadingIndicator;

        [Tooltip("Placeholder shown before the store answers.")]
        [SerializeField] private string pendingPriceLabel = "…";

        private Button _button;
        private IAPController _boundTo;

        public string ProductId => productId;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Buy);

            // Prices are unknown until the store answers; never show a stale placeholder price.
            _button.interactable = false;
            if (priceText != null) priceText.text = pendingPriceLabel;
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
        }

        private void OnEnable()
        {
            // These popups are instantiated on demand and can easily open before the
            // controller exists (or when entering a scene directly in the Editor), so bind
            // lazily rather than assuming Instance is already there.
            StartCoroutine(BindWhenAvailable());
        }

        private IEnumerator BindWhenAvailable()
        {
            while (IAPController.Instance == null) yield return null;

            Bind(IAPController.Instance);
            Refresh();
        }

        private void Bind(IAPController iap)
        {
            if (_boundTo == iap) return;

            Unbind();
            _boundTo = iap;

            iap.OnProductsLoaded += Refresh;
            iap.OnPurchaseSucceeded += OnPurchaseFinished;
            iap.OnPurchaseFailed += OnPurchaseFailed;
        }

        private void Unbind()
        {
            if (_boundTo == null) return;

            _boundTo.OnProductsLoaded -= Refresh;
            _boundTo.OnPurchaseSucceeded -= OnPurchaseFinished;
            _boundTo.OnPurchaseFailed -= OnPurchaseFailed;
            _boundTo = null;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            Unbind();
        }

        private void Refresh()
        {
            var iap = _boundTo;
            if (iap == null || !iap.IsReady)
            {
                _button.interactable = false;
                return;
            }

            // A product missing from the fetch is usually one not yet active in the Play
            // Console. Leave the card visible but dead — hiding it here would be permanent,
            // since a disabled object stops receiving the events that would restore it.
            bool available = iap.IsAvailable(productId);
            _button.interactable = available;

            if (priceText != null)
            {
                priceText.text = available
                    ? iap.GetLocalizedPrice(productId)
                    : "—";
            }
        }

        private void Buy()
        {
            if (_boundTo == null) return;

            _button.interactable = false;
            if (loadingIndicator != null) loadingIndicator.SetActive(true);

            _boundTo.Buy(productId);
        }

        private void OnPurchaseFinished(string purchasedId)
        {
            if (purchasedId != productId) return;
            ResetState();
        }

        private void OnPurchaseFailed(string failedId, string reason)
        {
            if (failedId != productId) return;
            ResetState();
        }

        private void ResetState()
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            Refresh();
        }
    }
}
