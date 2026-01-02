using System;
using Commons;
using DG.Tweening;
using TMPro;
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
    }
}