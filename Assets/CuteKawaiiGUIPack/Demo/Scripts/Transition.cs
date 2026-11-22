// Copyright (C) 2024 ricimi. All rights reserved.
// This code can only be used under the standard Unity Asset Store EULA,
// a copy of which is available at https://unity.com/legal/as-terms.
//
// Modified for DimsumSortJam: the next scene now loads asynchronously, in the background, while
// the screen fades out, instead of a synchronous LoadScene that froze the game for the whole load.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Ricimi
{
    /// <summary>
    /// Fades to a colour, swaps scenes, fades back.
    ///
    /// The scene starts loading the moment the fade begins, so most of the load happens behind the
    /// fade instead of after it. Activation waits until the screen is fully covered, so the player
    /// never sees the old scene being torn down. Only activation itself (the new scene's Awake and
    /// Start) still runs on the main thread.
    ///
    /// A transition already in flight ignores further requests, so a double tap on Play cannot
    /// start two loads.
    /// </summary>
    public class Transition : MonoBehaviour
    {
        private static bool s_busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_busy = false;

        public static void LoadLevel(string level, float duration, Color color)
        {
            if (s_busy)
            {
                Debug.LogWarning($"[Transition] Already transitioning; ignoring request for '{level}'.");
                return;
            }
            s_busy = true;

            // An overlay canvas of its own that survives the scene change, so the fade can finish
            // drawing over the new scene.
            var go = new GameObject("TransitionCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            go.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(go);

            var transition = go.AddComponent<Transition>();
            transition.StartCoroutine(transition.Run(level, duration, color));
        }

        private IEnumerator Run(string level, float duration, Color fadeColor)
        {
            // Full-screen blocker. It swallows taps for the whole transition even when invisible
            // (duration 0), so nothing in the outgoing scene can be pressed mid-load.
            var overlay = new GameObject("Fade");
            overlay.transform.SetParent(transform, false);
            var rect = overlay.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = overlay.AddComponent<Image>();
            image.color = fadeColor;
            image.raycastTarget = true;
            image.canvasRenderer.SetAlpha(0f);

            var op = SceneManager.LoadSceneAsync(level, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[Transition] Could not load scene '{level}'. Is it in the build settings?");
                Finish();
                yield break;
            }
            op.allowSceneActivation = false;

            float half = duration / 2f;

            // Fade out while the load runs in the background. Unscaled time, so a paused game
            // (timeScale 0) cannot stall the transition.
            yield return Fade(image, 0f, 1f, half);

            // The screen is covered now (or the caller asked for no fade), so smoothness no longer
            // matters: let the loader take as much of each frame as it wants.
            var previousPriority = Application.backgroundLoadingPriority;
            Application.backgroundLoadingPriority = ThreadPriority.High;

            // Async loads park at 0.9 until activation is allowed.
            while (op.progress < 0.9f) yield return null;

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            Application.backgroundLoadingPriority = previousPriority;

            yield return Fade(image, 1f, 0f, half);

            Finish();
        }

        private static IEnumerator Fade(Image image, float from, float to, float time)
        {
            if (time <= 0f)
            {
                // No fade requested. Leave the overlay invisible either way: a zero-length "fade"
                // would otherwise flash solid colour for exactly as long as the load takes.
                image.canvasRenderer.SetAlpha(0f);
                yield break;
            }

            float t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                image.canvasRenderer.SetAlpha(Mathf.Lerp(from, to, t / time));
                yield return null;
            }
            image.canvasRenderer.SetAlpha(to);
        }

        private void Finish()
        {
            s_busy = false;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // Covers being torn down by anything other than Finish, so a lost transition can never
            // lock every future scene change out.
            s_busy = false;
        }
    }
}
