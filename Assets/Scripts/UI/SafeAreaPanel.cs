using Controllers;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Insets a full-screen panel so its content stays clear of the notch, punch-hole camera,
    /// status bar and gesture bar. Attach to a RectTransform that stretches over the whole Canvas.
    ///
    /// The device is measured once per app run and the result is cached, so later scene loads
    /// reuse the same values instead of asking the OS again.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaPanel : MonoBehaviour
    {
        [Header("Edges to pad")]
        [Tooltip("Inset the top edge (notch, punch-hole camera, status bar).")]
        [SerializeField] private bool padTop = true;

        [Tooltip("Inset the bottom edge (gesture bar / home indicator).")]
        [SerializeField] private bool padBottom = true;

        [Tooltip("Inset the left edge. Only useful in landscape.")]
        [SerializeField] private bool padLeft;

        [Tooltip("Inset the right edge. Only useful in landscape.")]
        [SerializeField] private bool padRight;

        [Tooltip("Also inset the bottom edge by the height of the banner ad, so the ad cannot " +
                 "cover the content (e.g. the bottom tab bar). Needs padBottom to be meaningful " +
                 "on its own, but works independently of it.")]
        [SerializeField] private bool padBannerAd;

        [Header("Extra padding (canvas units)")]
        [SerializeField] private float extraTopPadding;
        [SerializeField] private float extraBottomPadding;

        [Header("Debug")]
        [Tooltip("Log the detected insets once, so they can be checked on a real device.")]
        [SerializeField] private bool logInsets;

        // Detected once at game start and shared by every panel in every scene.
        private static bool _measured;
        private static float _insetLeft;
        private static float _insetRight;
        private static float _insetTop;
        private static float _insetBottom;
        private static float _screenWidth;
        private static float _screenHeight;

        private void OnEnable()
        {
            Measure();
            Apply();

            if (padBannerAd)
            {
                // The banner is adaptive, so its height only arrives once the ad has loaded.
                BannerAdController.BannerHeightChanged += OnBannerHeightChanged;
            }
        }

        private void OnDisable()
        {
            if (padBannerAd)
            {
                BannerAdController.BannerHeightChanged -= OnBannerHeightChanged;
            }
        }

        private void OnBannerHeightChanged(float heightPixels)
        {
            Apply();
        }

        /// <summary>
        /// Reads Screen.safeArea and Screen.cutouts a single time and caches the four pixel insets.
        /// </summary>
        private void Measure()
        {
            if (_measured)
            {
                return;
            }

            _measured = true;
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;

            if (_screenWidth <= 0f || _screenHeight <= 0f)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            _insetLeft = Mathf.Max(0f, safeArea.xMin);
            _insetBottom = Mathf.Max(0f, safeArea.yMin);
            _insetRight = Mathf.Max(0f, _screenWidth - safeArea.xMax);
            _insetTop = Mathf.Max(0f, _screenHeight - safeArea.yMax);

            MergeCutouts();

            // Never let a bad report swallow the screen.
            _insetLeft = Mathf.Clamp(_insetLeft, 0f, _screenWidth * 0.5f);
            _insetRight = Mathf.Clamp(_insetRight, 0f, _screenWidth * 0.5f);
            _insetTop = Mathf.Clamp(_insetTop, 0f, _screenHeight * 0.5f);
            _insetBottom = Mathf.Clamp(_insetBottom, 0f, _screenHeight * 0.5f);

            if (logInsets)
            {
                Debug.Log($"[SafeAreaPanel] screen {_screenWidth}x{_screenHeight} safeArea {safeArea} " +
                          $"cutouts {Screen.cutouts.Length} -> insets " +
                          $"l:{_insetLeft} r:{_insetRight} t:{_insetTop} b:{_insetBottom}");
            }
        }

        /// <summary>
        /// The safe area already excludes the notch on most devices, but this project sets
        /// androidRenderOutsideSafeArea, so a punch-hole camera can show up only in Screen.cutouts.
        /// Each cutout is folded into the edge it sits closest to, keeping the larger inset.
        /// </summary>
        private static void MergeCutouts()
        {
            // A hole further than this fraction of the screen from every edge is floating in the
            // middle of the display - there is nothing sensible to pad against, so it is ignored.
            const float maxEdgeDistanceRatio = 0.25f;

            Rect[] cutouts = Screen.cutouts;
            for (int i = 0; i < cutouts.Length; i++)
            {
                Rect cutout = cutouts[i];
                if (cutout.width <= 0f || cutout.height <= 0f)
                {
                    continue;
                }

                float toLeft = cutout.xMin;
                float toRight = _screenWidth - cutout.xMax;
                float toBottom = cutout.yMin;
                float toTop = _screenHeight - cutout.yMax;
                float nearest = Mathf.Min(Mathf.Min(toLeft, toRight), Mathf.Min(toBottom, toTop));

                if (nearest.Equals(toTop))
                {
                    if (toTop <= _screenHeight * maxEdgeDistanceRatio)
                    {
                        _insetTop = Mathf.Max(_insetTop, _screenHeight - cutout.yMin);
                    }
                }
                else if (nearest.Equals(toBottom))
                {
                    if (toBottom <= _screenHeight * maxEdgeDistanceRatio)
                    {
                        _insetBottom = Mathf.Max(_insetBottom, cutout.yMax);
                    }
                }
                else if (nearest.Equals(toLeft))
                {
                    if (toLeft <= _screenWidth * maxEdgeDistanceRatio)
                    {
                        _insetLeft = Mathf.Max(_insetLeft, cutout.xMax);
                    }
                }
                else if (toRight <= _screenWidth * maxEdgeDistanceRatio)
                {
                    _insetRight = Mathf.Max(_insetRight, _screenWidth - cutout.xMin);
                }
            }
        }

        /// <summary>
        /// Turns the pixel insets into normalised anchors. Anchors are resolution independent, so
        /// no CanvasScaler maths is needed - this only requires the panel to stretch over the Canvas.
        /// </summary>
        private void Apply()
        {
            if (_screenWidth <= 0f || _screenHeight <= 0f)
            {
                return;
            }

            RectTransform rect = (RectTransform)transform;

            // The banner sits flush with the bottom of the window, on top of the safe-area inset,
            // so the two stack rather than overlap.
            float bottomPixels = padBottom ? _insetBottom : 0f;
            if (padBannerAd)
            {
                bottomPixels += BannerAdController.BannerHeightPixels;
            }

            bottomPixels = Mathf.Clamp(bottomPixels, 0f, _screenHeight * 0.5f);

            float minX = padLeft ? _insetLeft / _screenWidth : 0f;
            float minY = bottomPixels / _screenHeight;
            float maxX = padRight ? 1f - _insetRight / _screenWidth : 1f;
            float maxY = padTop ? 1f - _insetTop / _screenHeight : 1f;

            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = new Vector2(0f, extraBottomPadding);
            rect.offsetMax = new Vector2(0f, -extraTopPadding);
        }
    }
}
