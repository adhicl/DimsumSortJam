using Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Wires an existing "Restore Purchases" button to <see cref="IAPController"/>.
    ///
    /// Restoring re-delivers the non-consumable products (remove_ads, starter_pack) to a
    /// player who reinstalled or switched device. It is a component rather than an inspector
    /// onClick binding because the popup is a prefab and IAPController is a scene singleton —
    /// a prefab cannot hold a reference to it.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RestorePurchasesButton : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Restore);
        }

        private void Restore()
        {
            if (IAPController.Instance == null)
            {
                Debug.LogWarning("[Restore] IAPController not available; play from the Splash scene.");
                return;
            }

            // Restored purchases arrive through the normal OnPurchasePending path, which
            // re-applies entitlements and saves — nothing extra to do here.
            _button.interactable = false;
            IAPController.Instance.RestorePurchases();
            _button.interactable = true;
        }
    }
}
