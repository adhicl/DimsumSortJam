using UnityEngine;

namespace Controllers
{
    /// <summary>
    /// Makes the ad services exist no matter which scene the game starts from.
    ///
    /// <see cref="ConsentController"/>, <see cref="BannerAdController"/> and
    /// <see cref="RewardedAdController"/> are DontDestroyOnLoad singletons, but they were only
    /// ever *placed* in Splash. Start play on Home, Game or a tutorial — which is how a scene
    /// gets tested — and none of them exist, so the Ads SDK is never initialized: no banner, no
    /// rewarded video, and no error to explain it, because there is nothing running to complain.
    ///
    /// This spawns them from <c>Resources/AdServices</c> when they are missing. It runs
    /// <see cref="RuntimeInitializeLoadType.AfterSceneLoad"/>, *after* the first scene's Awake, so
    /// booting from Splash is completely unaffected: those instances already exist by then and
    /// this does nothing. Only a scene that lacks them gets a spawned copy.
    ///
    /// The prefab is built from the Splash objects rather than configured by hand, so the ad unit
    /// ids, <c>useTestAd</c> and the banner scene list cannot drift apart from the ones that ship.
    /// Rebuild it from Splash if those ever change — see next_step.md, "Ads outside Splash".
    /// </summary>
    public static class AdServicesBootstrap
    {
        /// <summary>Path under a Resources folder, no extension.</summary>
        private const string PrefabPath = "AdServices";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Splash (or anything else that already carries them) wins. Checked per controller
            // rather than on one of them, so a half-populated scene still gets nothing spawned on
            // top of what it has - the controllers' own duplicate guards would destroy the whole
            // spawned object, taking the other two with it.
            if (ConsentController.Instance != null
                || BannerAdController.Instance != null
                || RewardedAdController.Instance != null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[AdServices] No '{PrefabPath}' prefab under a Resources folder; " +
                                 "ads will only work when starting from Splash. Rebuild it from the " +
                                 "Splash objects - see next_step.md, \"Ads outside Splash\".");
                return;
            }

            var instance = Object.Instantiate(prefab);

            // Instantiate appends "(Clone)", and this object shows up in logs and in the hierarchy
            // under DontDestroyOnLoad often enough to be worth naming properly.
            instance.name = prefab.name;

            // The controllers each call DontDestroyOnLoad on their own gameObject in Awake, which
            // has already run by now - so the object is kept alive without anything further here.
            Debug.Log("[AdServices] Ad controllers were not in this scene; spawned them from Resources.");
        }
    }
}
