using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Commons
{
    /// <summary>
    /// Runs work on Unity's main thread from wherever it was raised. Spawns itself on first use,
    /// so nothing has to be placed in a scene.
    ///
    /// Two callers need it. Android hands native dialog callbacks back on its own UI thread. The
    /// Google Mobile Ads plugin raises ad and consent callbacks on whatever thread the SDK
    /// happened to be on — its <c>RaiseAdEventsOnUnityMainThread</c> switch is now
    /// <c>[Obsolete]</c>, so the ad controllers marshal explicitly through
    /// <see cref="Run"/> rather than trusting the flag. PlayerPrefs, coroutines, RectTransforms and
    /// the Analytics SDK are all main-thread-only, and an exception thrown inside a JNI callback
    /// is swallowed rather than logged — which is how a banner can fail to appear with nothing in
    /// logcat to say why.
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static int _mainThreadId;
        private static readonly Queue<Action> Pending = new Queue<Action>();

        /// <summary>
        /// True when called from Unity's main thread. Only meaningful after <see cref="Ensure"/>;
        /// before that, nothing has recorded which thread is main and this answers false, which
        /// makes <see cref="Run"/> fall back to queueing — safe, just one frame later.
        /// </summary>
        public static bool IsMainThread =>
            _instance != null && Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        /// <summary>
        /// Creates the dispatcher if it does not exist. Main thread only — this is also what
        /// records which thread *is* main, so it has to be called from there.
        /// </summary>
        public static void Ensure()
        {
            if (_instance != null) return;

            _mainThreadId = Thread.CurrentThread.ManagedThreadId;

            var go = new GameObject("MainThreadDispatcher") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MainThreadDispatcher>();
        }

        /// <summary>
        /// Runs <paramref name="action"/> on the main thread: immediately if already there, else
        /// on the next frame. The "immediately" case matters for ad callbacks — when the plugin
        /// does deliver on the main thread, this adds no latency and no reordering.
        /// </summary>
        public static void Run(Action action)
        {
            if (action == null) return;

            if (IsMainThread)
            {
                action();
                return;
            }

            Enqueue(action);
        }

        /// <summary>Queues work for the next frame. Safe to call from any thread.</summary>
        public static void Enqueue(Action action)
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
