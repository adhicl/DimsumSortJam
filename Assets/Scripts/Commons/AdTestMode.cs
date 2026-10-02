using System;
using UnityEngine;

namespace Commons
{
    /// <summary>
    /// Decides when ad requests must use Google's sample ad units instead of the live ones.
    ///
    /// An emulator can never show a live ad. The Mobile Ads SDK classifies emulators as test
    /// devices automatically, so a request against a live ad unit comes back empty - and if one
    /// ever did fill, the impression would be invalid traffic against the account. The result is
    /// a build where ads simply never appear, with nothing obviously wrong in the project.
    ///
    /// Rather than leave that to a <c>useTestAd</c> checkbox someone has to remember to flip back
    /// before release, the emulator case is forced here. Real hardware still honours the
    /// checkbox, so live ads can be verified on a device before shipping.
    /// </summary>
    public static class AdTestMode
    {
        private static bool _checked;
        private static bool _isEmulator;

        /// <summary>
        /// True when running on an Android emulator. Cached: it reads several
        /// <c>android.os.Build</c> fields over JNI and the answer cannot change mid-run.
        /// </summary>
        public static bool IsEmulator
        {
            get
            {
                if (_checked) return _isEmulator;
                _checked = true;
                _isEmulator = DetectEmulator();
                return _isEmulator;
            }
        }

        /// <summary>
        /// Whether to request test ads, given what the controller was authored with.
        /// </summary>
        public static bool ShouldUseTestAds(bool authoredUseTestAd)
        {
            return authoredUseTestAd || IsEmulator;
        }

        private static bool DetectEmulator()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var build = new AndroidJavaClass("android.os.Build"))
                {
                    string fingerprint  = build.GetStatic<string>("FINGERPRINT") ?? string.Empty;
                    string model        = build.GetStatic<string>("MODEL") ?? string.Empty;
                    string manufacturer = build.GetStatic<string>("MANUFACTURER") ?? string.Empty;
                    string brand        = build.GetStatic<string>("BRAND") ?? string.Empty;
                    string device       = build.GetStatic<string>("DEVICE") ?? string.Empty;
                    string product      = build.GetStatic<string>("PRODUCT") ?? string.Empty;
                    string hardware     = build.GetStatic<string>("HARDWARE") ?? string.Empty;

                    // The long-standing community check. No single field is reliable on its own:
                    // emulator images vary, and some vendors ship odd values on real hardware.
                    bool emulator =
                        fingerprint.StartsWith("generic") || fingerprint.StartsWith("unknown")
                        || model.Contains("google_sdk") || model.Contains("Emulator")
                        || model.Contains("Android SDK built for")
                        || manufacturer.Contains("Genymotion")
                        || (brand.StartsWith("generic") && device.StartsWith("generic"))
                        || product == "google_sdk" || product.Contains("sdk_gphone")
                        || hardware.Contains("goldfish") || hardware.Contains("ranchu");

                    if (emulator)
                    {
                        Debug.Log($"[Ads] Emulator detected ({manufacturer} {model}, hardware={hardware}); " +
                                  "forcing Google's test ad units. Live ads never fill on an emulator.");
                    }

                    return emulator;
                }
            }
            catch (Exception e)
            {
                // Not being able to tell is not a reason to fail; assume real hardware and let
                // the authored setting stand.
                Debug.LogWarning($"[Ads] Emulator check failed ({e.Message}); assuming a real device.");
                return false;
            }
#else
            return false;
#endif
        }
    }
}
