using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Gets Splash on screen before anything heavy loads.
    ///
    /// Unity cannot draw a scene until it has loaded every asset the scene references. Splash used
    /// to carry the boot services and <see cref="LoadingScene"/> directly, which meant every
    /// jajanan sprite (through GameSetting), the avatars, the ads/IAP/UGS controllers and their
    /// Awake calls all had to finish before the first frame, so the player stared at the system
    /// splash for that long.
    ///
    /// Splash now holds only what is drawn. The services live in <c>SplashBoot</c>, which this
    /// loads additively and asynchronously once the first frame has been presented. Until
    /// <see cref="LoadingScene"/> arrives and takes the bar over (<see cref="HandOff"/>), this keeps
    /// the bar and the "Loading..." text moving so the screen never looks frozen.
    ///
    /// <see cref="LoadingScene"/> lives in the other scene and cannot reference this scene's UI,
    /// so it reaches the bar through <see cref="View"/>.
    /// </summary>
    public class SplashBootstrap : MonoBehaviour
    {
        public const string BootSceneName = "SplashBoot";

        /// <summary>The Splash instance currently on screen, or null outside Splash.</summary>
        public static SplashBootstrap View { get; private set; }

        [SerializeField] private Slider loadingSlider;
        [SerializeField] private TextMeshProUGUI loadingProgressText;
        [SerializeField] private TextMeshProUGUI loadingText;

        [Tooltip("Extra wait after the first frame before the boot scene starts loading. Every " +
                 "second here is added to every launch.")]
        [SerializeField] private float bootDelay = 0f;

        [Tooltip("How far the bar creeps on its own while the boot scene loads. LoadingScene " +
                 "continues from wherever it got to.")]
        [SerializeField, Range(0f, 0.5f)] private float placeholderProgress = 0.1f;

        public Slider LoadingSlider => loadingSlider;
        public TextMeshProUGUI LoadingProgressText => loadingProgressText;
        public TextMeshProUGUI LoadingText => loadingText;

        private bool _handedOff;

        private void Awake()
        {
            View = this;
            SetProgress(0f);
        }

        private void OnDestroy()
        {
            if (View == this) View = null;
        }

        private IEnumerator Start()
        {
            StartCoroutine(AnimatePlaceholder());

            // Let the first frame actually reach the screen before asking for anything heavy.
            yield return null;
            yield return new WaitForEndOfFrame();
            if (bootDelay > 0f) yield return new WaitForSecondsRealtime(bootDelay);

            if (SceneManager.GetSceneByName(BootSceneName).isLoaded) yield break;

            var op = SceneManager.LoadSceneAsync(BootSceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[Splash] Could not load '{BootSceneName}'. Is it in the build settings?");
            }
        }

        /// <summary>Called by <see cref="LoadingScene"/> when it takes the bar over.</summary>
        public void HandOff()
        {
            _handedOff = true;
        }

        private IEnumerator AnimatePlaceholder()
        {
            string[] frames = { "Loading", "Loading.", "Loading..", "Loading..." };
            int frame = 0;
            float progress = 0f;
            float nextFrameAt = 0f;

            while (!_handedOff)
            {
                // Eases toward the cap rather than reaching it, so it is still visibly moving
                // when a slow device takes a while to bring the boot scene in.
                progress = Mathf.MoveTowards(progress, placeholderProgress,
                    Time.unscaledDeltaTime * (placeholderProgress - progress + 0.01f));
                SetProgress(progress);

                if (Time.unscaledTime >= nextFrameAt && loadingText != null)
                {
                    loadingText.text = frames[frame];
                    frame = (frame + 1) % frames.Length;
                    nextFrameAt = Time.unscaledTime + 0.2f;
                }

                yield return null;
            }
        }

        private void SetProgress(float value)
        {
            if (loadingSlider != null) loadingSlider.value = value;
            if (loadingProgressText != null) loadingProgressText.text = $"{value * 100f:N0}%";
        }
    }
}
