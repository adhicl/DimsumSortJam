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
        public GameSetting gameSetting;
        [Inject] GameController gameController;

        [SerializeField] private TextMeshProUGUI textLevel;
        [SerializeField] private TextMeshProUGUI textProgressLevel;
        [SerializeField] private Slider sliderProgress;
        [SerializeField] private TextMeshProUGUI textTimerDown;

        // Kept exactly as authored, deliberately. These read like 0-255 values handed to a
        // constructor that wants 0-1, so they saturate at draw time: the paused clock renders
        // white and the running one cyan, not the grey and green the numbers suggest. Dividing by
        // 255 would match the apparent intent but would change how the game looks, which is an
        // art call rather than a cleanup - see next_step.md.
        private static readonly Color TimerPausedColor = new Color(219f, 219f, 219f, 1f);
        private static readonly Color TimerRunningColor = new Color(0f, 219f, 59f, 1f);
        
        private void Start()
        {
            textLevel.text = $"Lv. {gameSetting.currentLevel + 1}";
        }

        // What the labels currently read. Formatting a number allocates a string, and this used to
        // build two of them on every frame for a clock that ticks once a second and a counter that
        // moves a few times a level - about 120 dead strings a second for the collector to sweep
        // up mid-game. Nothing is rewritten now unless it would say something different.
        private int _shownMinutes = -1;
        private int _shownSeconds = -1;
        private string _shownProgress;
        private int _shownPauseState = -1;

        private void Update()
        {
            int pauseState = gameController.isTimerPause ? 1 : 0;
            if (_shownPauseState != pauseState)
            {
                _shownPauseState = pauseState;
                textTimerDown.color = pauseState == 1 ? TimerPausedColor : TimerRunningColor;
            }

            float remaining = Mathf.Max(0f, gameController.Timer);
            int minutes = Mathf.FloorToInt(remaining / 60f);
            // Floor, not round: rounding turned 59.6s left into "00:60" for a moment.
            int seconds = Mathf.FloorToInt(remaining % 60f);
            if (minutes != _shownMinutes || seconds != _shownSeconds)
            {
                _shownMinutes = minutes;
                _shownSeconds = seconds;
                textTimerDown.text = $"{minutes:D2}:{seconds:D2}";
            }

            string progress = gameController.Progress;
            if (!string.Equals(progress, _shownProgress, StringComparison.Ordinal))
            {
                _shownProgress = progress;
                textProgressLevel.text = progress;
            }

            sliderProgress.value = gameController.ProgressPercentage;
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