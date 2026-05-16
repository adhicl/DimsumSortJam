using System;
using Commons;
using Controllers;
using Ricimi;
using TMPro;
using UnityEngine;
using Zenject;

namespace UI
{
    public class PlayButton : MonoBehaviour
    {
        [Inject] private GameSetting _gameSetting;
        [Inject] SoundController _soundController;

        [SerializeField] private TextMeshProUGUI levelText;

        private void Start()
        {
            levelText.text = $"Level {_gameSetting.currentLevel}";
        }

        public void GoToNextScene()
        {
            SoundController.Instance.PlayButtonClickClip();
            string newScene = Settings.GetNextLevelScene(_gameSetting.currentLevel, "Game");
            Transition.LoadLevel(newScene, Settings.TransitionTime, Settings.TransitionColor);
        }
        
    }
}