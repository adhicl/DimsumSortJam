using System;
using System.Collections;
using Commons;
using Controllers;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class LoadingScene : MonoBehaviour
    {
        public GameSetting _gameSetting;
        
        [SerializeField] private Slider loadingSlider;
        [SerializeField] private TextMeshProUGUI loadingProgressText;
        [SerializeField] private TextMeshProUGUI loadingText;

        [Tooltip("Longest the loading screen waits for Cloud Save before starting on local data.")]
        [SerializeField] private float cloudSyncTimeout = 8f;

        [Tooltip("Used only for the default avatar a brand new player starts with.")]
        [SerializeField] private AvatarCatalog avatarCatalog;

        public AudioMixer mixer;

        private float _elapsed;
        private bool _transitioning;

        private void Start()
        {
            _gameSetting.LoadData();
            StartCoroutine(DoLoading());
        }

        private float progress = 0f;
        private void Update()
        {
            _elapsed += Time.deltaTime;

            // Hold the bar just short of full until the cloud save has resolved. Which scene
            // to open is decided from currentLevel, so starting before the download lands
            // could drop a returning player back into a level they already finished.
            var target = IsCloudSyncSettled() ? 1f : 0.9f;
            progress = Mathf.Clamp(progress + Time.deltaTime, 0f, target);

            loadingSlider.value = progress;
            loadingProgressText.text = $"{progress * 100f:N0}%";

            if (progress >= 1f && !_transitioning)
            {
                _transitioning = true;

                // Both run only now, after the cloud pull has resolved: refreshing early would
                // regenerate onto lives the download is about to overwrite, and granting early
                // would hand a second free window to someone who claimed today's elsewhere.
                _gameSetting.RefreshLives();
                _gameSetting.TryGrantDailyFreeUnlimitedLives();

                // Seed a name and face for a brand new player. Also after the cloud pull, so a
                // returning player's own profile lands first and this does nothing.
                _gameSetting.EnsureProfile(
                    GameServicesController.Instance != null ? GameServicesController.Instance.PlayerId : null,
                    avatarCatalog != null ? avatarCatalog.DefaultId : null);

                string newScene = "Home";
                if (_gameSetting.currentLevel < 5)
                {
                    newScene = Settings.GetNextLevelScene(_gameSetting.currentLevel, "Home");
                }
                Transition.LoadLevel(newScene, 0f, Settings.TransitionColor);
            }
        }

        /// <summary>
        /// True once Cloud Save has finished its first pull, or once we have waited long
        /// enough that a slow network should not keep the player staring at a loading bar.
        /// Also true when there is no CloudSaveController at all, so playing from a scene
        /// other than Splash still boots.
        /// </summary>
        private bool IsCloudSyncSettled()
        {
            var cloudSave = CloudSaveController.Instance;
            return cloudSave == null || cloudSave.HasSynced || _elapsed >= cloudSyncTimeout;
        }

        private IEnumerator DoLoading()
        {
            mixer.SetFloat("SfxVolume", _gameSetting.soundMute ? -80f : 0f);
            mixer.SetFloat("MusicVolume", _gameSetting.musicMute ? -80f : 0f);
        
            while (true)
            {
                loadingText.text = $"Loading {_gameSetting.currentLevel}";
                yield return new WaitForSeconds(0.2f);
                loadingText.text = $"Loading {_gameSetting.currentLevel}.";
                yield return new WaitForSeconds(0.2f);
                loadingText.text = $"Loading {_gameSetting.currentLevel}..";
                yield return new WaitForSeconds(0.2f);
                loadingText.text = $"Loading {_gameSetting.currentLevel}...";
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
}