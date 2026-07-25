using System;
using System.Collections;
using Commons;
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
        public AudioMixer mixer;

        private void Start()
        {
            _gameSetting.LoadData();
            StartCoroutine(DoLoading());
        }

        private float progress = 0f;
        private void Update()
        {
            progress = Mathf.Clamp(progress + Time.deltaTime, 0f, 1f);
            loadingSlider.value = progress;
            loadingProgressText.text = $"{progress * 100f:N0}%";
            if (progress >= 1f)
            {
                string newScene = "Home";
                if (_gameSetting.currentLevel < 5)
                {
                    newScene = Settings.GetNextLevelScene(_gameSetting.currentLevel, "Home");
                }
                Transition.LoadLevel(newScene, 0f, Settings.TransitionColor); 
            }
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