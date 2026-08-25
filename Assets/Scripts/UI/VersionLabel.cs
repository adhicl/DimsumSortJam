using System.Globalization;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Prints the build's version name and version code, e.g. <c>v1.2 (4)</c>.
    ///
    /// The two numbers answer different questions and both matter on a bug report: the **version
    /// name** (<c>Application.version</c>, "1.2") is what a player would quote, and the **version
    /// code** (<c>bundleVersionCode</c>, 4) is what identifies one specific upload in the Play
    /// Console — two builds can easily share a version name.
    ///
    /// The version code is read back from the **installed package** rather than baked in at build
    /// time, so it cannot drift from the APK the player is actually running.
    /// </summary>
    public class VersionLabel : MonoBehaviour
    {
        [Tooltip("Where to print. Taken from this object if left empty.")]
        [SerializeField] private TextMeshProUGUI label;

        [Tooltip("{0} is the version name (1.2), {1} the version code (4).")]
        [SerializeField] private string format = "v{0} ({1})";

        [Tooltip("Used when the version code cannot be read - anywhere that is not an Android " +
                 "device or the Editor. {0} is the version name.")]
        [SerializeField] private string formatWithoutCode = "v{0}";

        private void Awake()
        {
            if (label == null) label = GetComponent<TextMeshProUGUI>();
        }

        // Written in OnEnable rather than Awake so the label is right again if the splash screen
        // is ever re-shown, and so it survives a domain reload with the object already enabled.
        private void OnEnable()
        {
            if (label == null) return;

            string versionName = Application.version;
            string versionCode = ReadVersionCode();

            label.text = string.IsNullOrEmpty(versionCode)
                ? string.Format(CultureInfo.InvariantCulture, formatWithoutCode, versionName)
                : string.Format(CultureInfo.InvariantCulture, format, versionName, versionCode);
        }

        /// <summary>
        /// The build number, or empty when it cannot be known. Unity exposes it through
        /// <c>PlayerSettings</c>, which is editor-only, so on a device it comes from Android's own
        /// <c>PackageManager</c> — the authoritative copy, and the one the Play Console shows.
        /// </summary>
        private static string ReadVersionCode()
        {
#if UNITY_EDITOR
            return UnityEditor.PlayerSettings.Android.bundleVersionCode.ToString(CultureInfo.InvariantCulture);
#elif UNITY_ANDROID
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var manager = activity.Call<AndroidJavaObject>("getPackageManager"))
                using (var info = manager.Call<AndroidJavaObject>(
                    "getPackageInfo", activity.Call<string>("getPackageName"), 0))
                {
                    // getLongVersionCode() only exists from API 28. The versionCode field is
                    // deprecated there but still populated, and still what the Console displays,
                    // so it is the one that works on every device this ships to.
                    return info.Get<int>("versionCode").ToString(CultureInfo.InvariantCulture);
                }
            }
            catch (System.Exception e)
            {
                // A version label is never worth taking the splash screen down for.
                Debug.LogWarning("[VersionLabel] could not read the version code: " + e.Message);
                return string.Empty;
            }
#else
            return string.Empty;
#endif
        }
    }
}
