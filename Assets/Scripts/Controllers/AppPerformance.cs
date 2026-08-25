using UnityEngine;

namespace Controllers
{
    /// <summary>
    /// Frame pacing, applied once at startup.
    ///
    /// Left alone, the game renders at whatever the panel refreshes at — 90 or 120Hz on a lot of
    /// current Android phones. For a board that spends most of its time completely still, that is
    /// up to twice the GPU work and battery for no visible gain, and it is a common reason a
    /// casual title runs hot.
    ///
    /// <c>targetFrameRate</c> only takes effect while VSync is off; with <c>vSyncCount</c> at 1
    /// Unity paces to the display instead and ignores it. The Android compositor still presents on
    /// its own cadence, so turning VSync off here does not tear the way it would on a desktop.
    ///
    /// No scene wiring: a <see cref="RuntimeInitializeOnLoadMethodAttribute"/> runs before the
    /// first scene loads, so this cannot be forgotten in a scene that was set up before it existed.
    /// </summary>
    public static class AppPerformance
    {
        /// <summary>Frames per second to aim for. 60 is smooth for a sort puzzle.</summary>
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // Only on the platform this ships to. On desktop the Editor's own pacing is more
            // useful than a cap, and quality levels there are set up differently.
#if UNITY_ANDROID && !UNITY_EDITOR
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;

            // Nothing in the game rotates, and letting the screen turn off mid-level is the
            // player's business, so neither is touched here — this is only about frame pacing.
#endif
        }
    }
}
