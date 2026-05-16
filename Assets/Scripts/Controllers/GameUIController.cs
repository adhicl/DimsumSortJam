using System;
using Commons;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Controllers
{
    public class GameUIController : MonoBehaviour
    {
        [Inject] GameSetting gameSetting;
        [Inject] GameController gameController;

        [SerializeField] private TextMeshProUGUI textLevel;
        [SerializeField] private TextMeshProUGUI textProgressLevel;
        [SerializeField] private Slider sliderProgress;
        [SerializeField] private TextMeshProUGUI textTimerDown;
        
        private void Start()
        {
            textLevel.text = $"Lv. {gameSetting.currentLevel + 1}";
        }

        private void Update()
        {
            if (gameController.isTimerPause)
            {
                textTimerDown.color = new Color(219f, 219, 219f, 1f);
            }
            else
            {
                textTimerDown.color = new Color(0f, 219f, 59f, 1f);
            }
            textTimerDown.text = ReturnTimeString(gameController.Timer);
            textProgressLevel.text = gameController.Progress;
            sliderProgress.value = gameController.ProgressPercentage;
        }

        private string ReturnTimeString(float secondTime)
        {
            int minutes = Mathf.FloorToInt(secondTime / 60); 
            int seconds = Mathf.RoundToInt(secondTime % 60);
            return $"{minutes:D2}:{seconds:D2}";
        }

        [SerializeField] private GameObject tutorialCoverPanel;
        public void ShowTutorialCover()
        {
            tutorialCoverPanel.SetActive(true);
        }
        
        public void CloseTutorialCover()
        {
            tutorialCoverPanel.SetActive(false);
            playTopBar.ShowTutorialPowerUp();
        }
        
        [SerializeField] private PlayTopBar playTopBar;
    }
}