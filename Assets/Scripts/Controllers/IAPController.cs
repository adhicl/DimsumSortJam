using System;
using System.Collections.Generic;
using System.Linq;
using Commons;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Controllers
{
    /// <summary>
    /// Owns the Unity IAP (v5) store connection for the whole app. Lives as a singleton
    /// GameObject in the Loading scene (mirrors RewardedAdController.Instance) and survives
    /// scene loads, so shop buttons in any scene can reach it without Zenject injection.
    ///
    /// The product list itself lives in <see cref="IAPCatalog"/>; this class only connects,
    /// fetches, grants and confirms. See next_step.md for what has to exist in the
    /// Play Console before any of this returns real prices.
    /// </summary>
    public class IAPController : MonoBehaviour
    {
        public static IAPController Instance { get; private set; }

        [Tooltip("The same GameSetting asset the rest of the game uses — rewards are credited here.")]
        [SerializeField] private GameSetting gameSetting;

        /// <summary>Order ids already granted, so a re-delivered purchase is not paid out twice.</summary>
        private const string ProcessedOrdersKey = "iap_processed_orders";
        private const int MaxTrackedOrders = 64;

        private StoreController _store;

        // Ordered oldest-first so trimming drops the orders least likely to be re-delivered.
        private readonly List<string> _processedOrders = new List<string>();

        /// <summary>True once products have been fetched and prices are safe to display.</summary>
        public bool IsReady { get; private set; }

        public event Action OnProductsLoaded;
        public event Action<string> OnPurchaseSucceeded;
        public event Action<string, string> OnPurchaseFailed;
        public event Action<string> OnPurchaseDeferred;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProcessedOrders();
            _ = InitializeAsync();
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            _store = UnityIAPServices.StoreController();

            // Every subscription has to be in place before Connect: an unconfirmed purchase
            // from a previous session is re-delivered as soon as the store comes up.
            _store.OnStoreConnected += OnStoreConnected;
            _store.OnStoreDisconnected += failure =>
                Debug.LogWarning($"[IAP] Store disconnected: {failure.Message}");

            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += failure =>
                Debug.LogWarning($"[IAP] Product fetch failed: {failure.FailureReason}");

            _store.OnPurchasesFetched += OnPurchasesFetched;
            _store.OnPurchasesFetchFailed += failure =>
                Debug.LogWarning($"[IAP] Purchase fetch failed: {failure.FailureReason} - {failure.Message}");

            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnPurchaseFailed += OnStorePurchaseFailed;
            _store.OnPurchaseDeferred += OnStorePurchaseDeferred;

            try
            {
                await _store.Connect();
            }
            catch (Exception e)
            {
                Debug.LogError($"[IAP] Connect failed: {e.Message}");
            }
        }

        private void OnStoreConnected()
        {
            _store.FetchProducts(IAPCatalog.Definitions);

            // Re-applies remove_ads / starter_pack after a reinstall or a PlayerPrefs wipe.
            _store.FetchPurchases();
        }

        private void OnProductsFetched(List<Product> products)
        {
            IsReady = true;
            Debug.Log($"[IAP] {products.Count} products ready.");
            OnProductsLoaded?.Invoke();
        }

        private void OnPurchasesFetched(Orders orders)
        {
            // Confirmed non-consumables never fire OnPurchasePending again, so ownership has
            // to be re-derived here rather than trusted to local save data.
            foreach (var order in orders.ConfirmedOrders)
            {
                string productId = ProductIdOf(order);
                if (productId == null || !IAPCatalog.IsNonConsumable(productId)) continue;

                if (productId == IAPCatalog.RemoveAds || productId == IAPCatalog.StarterPack)
                {
                    if (!gameSetting.removeAds)
                    {
                        gameSetting.removeAds = true;
                        gameSetting.SaveData();
                        Debug.Log($"[IAP] Restored entitlement from {productId}.");
                    }
                }
            }
        }

        /// <summary>
        /// Grant, save, then confirm — in that order. Confirming first would let a crash
        /// between the two steps lose the purchase, because the store stops re-delivering
        /// an order once it has been confirmed.
        /// </summary>
        private void OnPurchasePending(PendingOrder pendingOrder)
        {
            string productId = ProductIdOf(pendingOrder);
            string transactionId = pendingOrder.Info?.TransactionID;

            if (productId == null)
            {
                Debug.LogWarning("[IAP] Pending order with no product; confirming to clear it.");
                _store.ConfirmPurchase(pendingOrder);
                return;
            }

            if (!IAPCatalog.Rewards.TryGetValue(productId, out var reward))
            {
                Debug.LogWarning($"[IAP] Unknown product '{productId}'; confirming without granting.");
                _store.ConfirmPurchase(pendingOrder);
                return;
            }

            // A purchase can be re-delivered on the next launch if the app died before the
            // confirm landed — pay it out once, but still confirm so it stops coming back.
            bool alreadyGranted = !string.IsNullOrEmpty(transactionId)
                                  && _processedOrders.Contains(transactionId);

            if (alreadyGranted)
            {
                Debug.Log($"[IAP] Order {transactionId} already granted; confirming only.");
            }
            else
            {
                // TODO: validate pendingOrder.Info.Receipt with CrossPlatformValidator before
                // granting, once the Google Play tangle is generated. See next_step.md
                // "Known gaps" — until then a tampered receipt would be trusted.
                gameSetting.ApplyReward(reward);
                MarkOrderProcessed(transactionId);
                Debug.Log($"[IAP] Granted '{productId}'.");
                OnPurchaseSucceeded?.Invoke(productId);
            }

            _store.ConfirmPurchase(pendingOrder);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmed:
                    Debug.Log($"[IAP] Confirmed '{ProductIdOf(confirmed)}'.");
                    break;
                case FailedOrder failed:
                    // The reward was already granted and saved; the store just could not
                    // finalize. It will re-deliver, and the ledger stops a double grant.
                    Debug.LogError($"[IAP] Confirmation failed: {failed.FailureReason} - {failed.Details}");
                    break;
            }
        }

        private void OnStorePurchaseFailed(FailedOrder failedOrder)
        {
            string productId = ProductIdOf(failedOrder) ?? "unknown";
            Debug.LogWarning($"[IAP] Purchase failed for '{productId}': " +
                             $"{failedOrder.FailureReason} - {failedOrder.Details}");
            OnPurchaseFailed?.Invoke(productId, failedOrder.FailureReason.ToString());
        }

        private void OnStorePurchaseDeferred(DeferredOrder deferredOrder)
        {
            // Ask-to-Buy / pending Play payments: grant nothing, wait for OnPurchasePending.
            string productId = ProductIdOf(deferredOrder) ?? "unknown";
            Debug.Log($"[IAP] Purchase deferred for '{productId}', awaiting approval.");
            OnPurchaseDeferred?.Invoke(productId);
        }

        // --- Public API for shop UI ---

        /// <summary>Starts the platform purchase dialog for a product id from <see cref="IAPCatalog"/>.</summary>
        public void Buy(string productId)
        {
            if (!IsReady)
            {
                Debug.LogWarning($"[IAP] Not ready yet; ignoring buy '{productId}'.");
                OnPurchaseFailed?.Invoke(productId, "StoreNotReady");
                return;
            }

            var product = _store.GetProductById(productId);
            if (product == null || !product.availableToPurchase)
            {
                Debug.LogWarning($"[IAP] Product '{productId}' is unavailable.");
                OnPurchaseFailed?.Invoke(productId, "ProductUnavailable");
                return;
            }

            _store.PurchaseProduct(product);
        }

        /// <summary>
        /// Re-delivers non-consumables (remove_ads, starter_pack). Wire this to a
        /// "Restore Purchases" button — required by Apple, good practice on Android.
        /// </summary>
        public void RestorePurchases()
        {
            if (_store == null) return;

            _store.RestoreTransactions((success, error) =>
            {
                if (success) Debug.Log("[IAP] Restore finished.");
                else Debug.LogWarning($"[IAP] Restore failed: {error}");
            });
        }

        /// <summary>
        /// Localized price for a product ("Rp 15.000"), or null before products load.
        /// Always show this rather than a hard-coded price — the store sets it per region.
        /// </summary>
        public string GetLocalizedPrice(string productId)
        {
            if (!IsReady) return null;
            return _store.GetProductById(productId)?.metadata?.localizedPriceString;
        }

        public string GetLocalizedTitle(string productId)
        {
            if (!IsReady) return null;
            return _store.GetProductById(productId)?.metadata?.localizedTitle;
        }

        public bool IsAvailable(string productId) =>
            IsReady && _store.GetProductById(productId)?.availableToPurchase == true;

        // --- Helpers ---

        private static string ProductIdOf(Order order) =>
            order?.CartOrdered?.Items()?.FirstOrDefault()?.Product?.definition?.id;

        private void LoadProcessedOrders()
        {
            string stored = PlayerPrefs.GetString(ProcessedOrdersKey, string.Empty);
            if (string.IsNullOrEmpty(stored)) return;

            foreach (var id in stored.Split(';'))
            {
                if (!string.IsNullOrEmpty(id)) _processedOrders.Add(id);
            }
        }

        private void MarkOrderProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;

            _processedOrders.Add(transactionId);

            // Keep the ledger bounded; only recent orders can still be re-delivered.
            if (_processedOrders.Count > MaxTrackedOrders)
            {
                _processedOrders.RemoveRange(0, _processedOrders.Count - MaxTrackedOrders);
            }

            PlayerPrefs.SetString(ProcessedOrdersKey, string.Join(";", _processedOrders));
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
