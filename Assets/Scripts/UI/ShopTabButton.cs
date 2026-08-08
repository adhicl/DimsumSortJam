using Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Sends a button to the shop tab. Used by the top bar's heart, which used to open a
    /// lives-only popup — lives are sold as part of a bundle now, so it goes to the shelf.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ShopTabButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Open);
        }

        private void Open()
        {
            if (SoundController.Instance != null) SoundController.Instance.PlayButtonClickClip();
            ShopTab.Show();
        }
    }
}
