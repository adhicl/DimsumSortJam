using System;
using System.Collections.Generic;
using Commons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Controllers
{
    /// <summary>
    /// Starts the whole boot again after it failed, and quits when it cannot.
    ///
    /// Reloading Splash on its own retries nothing. Every service - sign-in, consent, ads, IAP,
    /// Cloud Save - is a <c>DontDestroyOnLoad</c> singleton, so it survives the reload, sees its
    /// own <c>Instance</c> already set and destroys the fresh copy Splash just created. The stuck
    /// ones stay stuck, and the player watches the same loading bar hang twice. So the retry tears
    /// the services down first and only then reloads.
    /// </summary>
    public static class BootRetry
    {
        /// <summary>Splash is build index 0; the name is only used to sanity-check that.</summary>
        private const string SplashSceneName = "Splash";

        /// <summary>
        /// Tears down every boot service and reloads Splash. Returns false if the reload could not
        /// be started, which is the caller's cue to quit instead - see <see cref="QuitApp"/>.
        /// </summary>
        public static bool TryRestart()
        {
            try
            {
                int splashIndex = FindSplashBuildIndex();
                if (splashIndex < 0)
                {
                    Debug.LogError("[BootRetry] Splash is not in the build settings; cannot retry.");
                    return false;
                }

                TearDownServices();
                ResetStatics();

                // Time may have been paused by whatever went wrong on the way in.
                Time.timeScale = 1f;

                SceneManager.LoadScene(splashIndex);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[BootRetry] Restart failed: {e}");
                return false;
            }
        }

        /// <summary>
        /// Closes the game. The last resort when the boot cannot even be retried.
        /// </summary>
        public static void QuitApp()
        {
            Debug.LogWarning("[BootRetry] Quitting.");
#if UNITY_EDITOR
            // Application.Quit does nothing in the Editor, so the Editor's own stop stands in -
            // otherwise the "quit" button would look broken while testing.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static int FindSplashBuildIndex()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (!string.IsNullOrEmpty(path)
                    && System.IO.Path.GetFileNameWithoutExtension(path) == SplashSceneName)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Destroys the persistent service objects so Splash can build them fresh.
        ///
        /// Found by component rather than by name: the objects are called different things
        /// depending on whether they came from the Splash scene or from
        /// <see cref="AdServicesBootstrap"/>, and a name list would quietly miss one.
        /// </summary>
        private static void TearDownServices()
        {
            var doomed = new HashSet<GameObject>();

            Collect<GameServicesController>(doomed);
            Collect<ConsentController>(doomed);
            Collect<BannerAdController>(doomed);
            Collect<RewardedAdController>(doomed);
            Collect<IAPController>(doomed);
            Collect<CloudSaveController>(doomed);
            Collect<MainThreadDispatcher>(doomed);

            foreach (var go in doomed)
            {
                if (go == null) continue;
                UnityEngine.Object.Destroy(go);
            }

            Debug.Log($"[BootRetry] Tore down {doomed.Count} service object(s) before reloading.");
        }

        private static void Collect<T>(HashSet<GameObject> into) where T : Component
        {
            foreach (var c in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include,
                                                                     FindObjectsSortMode.None))
            {
                if (c != null) into.Add(c.gameObject);
            }
        }

        /// <summary>
        /// Clears the static state that outlives those objects. Without this the reload looks
        /// fresh but behaves like the failed run: the ads SDK still believes it is initializing,
        /// consent still holds callbacks belonging to destroyed controllers, and analytics still
        /// believes collection is on.
        /// </summary>
        private static void ResetStatics()
        {
            MobileAdsSdk.ResetForRestart();
            ConsentController.ResetForRestart();
            BannerAdController.ResetForRestart();
            RewardedAdController.ResetForRestart();
            MainThreadDispatcher.ResetForRestart();
            GameAnalytics.IsReady = false;
        }
    }
}
