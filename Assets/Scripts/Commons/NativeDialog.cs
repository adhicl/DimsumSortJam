using System;
using System.Collections.Generic;
using UnityEngine;

namespace Commons
{
    /// <summary>
    /// A native Android two-button dialog, used for the analytics consent prompt.
    ///
    /// Native rather than a Unity prefab on purpose: the prompt has to appear during Splash,
    /// before any game UI canvas is up, and a system dialog is what players expect a privacy
    /// question to look like. Swap it for a styled popup later by reimplementing
    /// <see cref="ShowConfirm"/> — nothing else needs to change.
    ///
    /// On every non-Android platform, and in the Editor, there is no native dialog to show, so
    /// the call completes immediately with <c>fallbackAnswer</c>.
    /// </summary>
    public static class NativeDialog
    {
        /// <summary>
        /// Shows a modal two-button dialog. <paramref name="onResult"/> receives true for the
        /// positive button and always runs on Unity's main thread.
        /// </summary>
        /// <param name="fallbackAnswer">
        /// What to answer where no dialog can be shown (Editor, desktop). The caller decides what
        /// is safe — the consent prompt passes false so a missing dialog never silently opts a
        /// player in.
        /// </param>
        public static void ShowConfirm(string title, string message, string positiveButton,
                                       string negativeButton, Action<bool> onResult,
                                       bool fallbackAnswer = false)
        {
            if (onResult == null) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Must exist before the Java callback fires, and can only be created from the
                // main thread — which is where we are now.
                MainThreadDispatcher.Ensure();

                var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = player.GetStatic<AndroidJavaObject>("currentActivity");

                // AlertDialog must be built and shown on Android's UI thread, which is not
                // Unity's main thread. Nothing Unity-side may be touched inside this runnable.
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        var builder = new AndroidJavaObject("android.app.AlertDialog$Builder", activity);
                        builder.Call<AndroidJavaObject>("setTitle", title);
                        builder.Call<AndroidJavaObject>("setMessage", message);

                        // No dismissing a consent question by tapping outside it.
                        builder.Call<AndroidJavaObject>("setCancelable", false);

                        builder.Call<AndroidJavaObject>("setPositiveButton", positiveButton,
                            new DialogClickListener(() => MainThreadDispatcher.Enqueue(() => onResult(true))));
                        builder.Call<AndroidJavaObject>("setNegativeButton", negativeButton,
                            new DialogClickListener(() => MainThreadDispatcher.Enqueue(() => onResult(false))));

                        builder.Call<AndroidJavaObject>("show");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[NativeDialog] Failed to show: {e.Message}");
                        MainThreadDispatcher.Enqueue(() => onResult(fallbackAnswer));
                    }
                }));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NativeDialog] Unavailable: {e.Message}");
                onResult(fallbackAnswer);
            }
#else
            onResult(fallbackAnswer);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>Bridges Java's DialogInterface.OnClickListener back to a C# delegate.</summary>
        private class DialogClickListener : AndroidJavaProxy
        {
            private readonly Action _onClick;

            public DialogClickListener(Action onClick)
                : base("android.content.DialogInterface$OnClickListener")
            {
                _onClick = onClick;
            }

            // Name and signature must match the Java interface exactly or the proxy never fires.
            public void onClick(AndroidJavaObject dialog, int which)
            {
                try { dialog.Call("dismiss"); }
                catch (Exception) { /* Already gone; the answer below still counts. */ }

                _onClick();
            }
        }
#endif
    }

    /// <summary>
    /// Runs queued work on Unity's main thread. Spawns itself on first use, so nothing has to be
    /// placed in a scene. Only needed because Android hands dialog callbacks back on its own UI
    /// thread, where PlayerPrefs and the Analytics SDK are not safe to touch.
    /// </summary>
    internal class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static readonly Queue<Action> Pending = new Queue<Action>();

        /// <summary>Creates the dispatcher if it does not exist. Main thread only.</summary>
        internal static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject("MainThreadDispatcher") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MainThreadDispatcher>();
        }

        /// <summary>Queues work for the next frame. Safe to call from any thread.</summary>
        internal static void Enqueue(Action action)
        {
            if (action == null) return;
            lock (Pending) { Pending.Enqueue(action); }
        }

        private void Update()
        {
            while (true)
            {
                Action action;
                lock (Pending)
                {
                    if (Pending.Count == 0) return;
                    action = Pending.Dequeue();
                }

                // One failing callback must not stall everything queued behind it.
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
