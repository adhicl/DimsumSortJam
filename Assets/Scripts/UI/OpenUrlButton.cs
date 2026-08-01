using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Opens a URL in the device browser when the button is pressed.
    ///
    /// Used for the Privacy Policy and Terms of Use links in Settings — Google Play requires a
    /// reachable privacy policy for any app that handles user data, which this one does via
    /// ads and IAP. It is a component rather than an inspector onClick binding because
    /// UnityEvent cannot call the static Application.OpenURL without a target object.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class OpenUrlButton : MonoBehaviour
    {
        [Tooltip("Absolute URL to open, e.g. https://example.com/privacy-policy.html")]
        [SerializeField] private string url;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Open);
        }

        private void Open()
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning($"[OpenUrl] No URL set on '{name}'.");
                return;
            }

            Application.OpenURL(url);
        }
    }
}
