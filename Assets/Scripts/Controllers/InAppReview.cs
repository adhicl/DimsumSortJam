using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
#endif

namespace Controllers
{
    /// <summary>
    /// Thin wrapper over Google's In-App Review flow (<c>com.google.play.review</c>), which shows
    /// Play's own rating card over the game instead of sending the player out to the store.
    ///
    /// Two things about that API shape everything here:
    ///
    /// <list type="bullet">
    /// <item><b>It can decide to show nothing.</b> Google quotas how often a player sees the card,
    /// and when it declines the flow still reports success. There is no way to tell a rating from
    /// a no-op, so nothing may be paid out for rating and the caller must not promise anything.</item>
    /// <item><b>It is a request, not a command.</b> Which is why the store link stays as the
    /// fallback for an explicit "Rate Us" press — that one is a promise, and has to lead
    /// somewhere the player can actually see.</item>
    /// </list>
    ///
    /// The package's Editor build stubs both calls out and returns success immediately, so
    /// <see cref="IsSupported"/> reports false there: silently "succeeding" with nothing on screen
    /// would make the button look broken every time it is tested in the Editor.
    /// </summary>
    public static class InAppReview
    {
        /// <summary>True where Play can actually show the card: an Android build, not the Editor.</summary>
        public static bool IsSupported
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Asks Play to show its rating card. <paramref name="onComplete"/> gets true when the
        /// flow ran to completion — which, per the note above, does not mean a review was left —
        /// and false when it could not run at all, so the caller can fall back to the store page.
        /// Always called exactly once, and always on the main thread.
        /// </summary>
        public static void Request(Action<bool> onComplete)
        {
            if (onComplete == null) onComplete = _ => { };

            if (!IsSupported)
            {
                onComplete(false);
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var manager = new ReviewManager();
                var request = manager.RequestReviewFlow();
                request.Completed += requestOp =>
                {
                    if (requestOp.Error != ReviewErrorCode.NoError)
                    {
                        Debug.LogWarning("[InAppReview] request failed: " + requestOp.Error);
                        onComplete(false);
                        return;
                    }

                    var launch = manager.LaunchReviewFlow(requestOp.GetResult());
                    launch.Completed += launchOp =>
                    {
                        bool ok = launchOp.Error == ReviewErrorCode.NoError;
                        if (!ok) Debug.LogWarning("[InAppReview] launch failed: " + launchOp.Error);
                        onComplete(ok);
                    };
                };
            }
            catch (Exception e)
            {
                // Play Services missing or too old. Never worth taking the game down for.
                Debug.LogWarning("[InAppReview] unavailable: " + e.Message);
                onComplete(false);
            }
#endif
        }
    }
}
