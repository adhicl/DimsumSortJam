using System;
using System.Collections.Generic;
using System.Linq;
using Commons;
using DG.Tweening;
using IClasses;
using Models;
using Spawners;
using UnityEngine;
using Zenject;
using Zenject.SpaceFighter;
using Random = System.Random;

namespace Controllers
{
    public class GameController : MonoBehaviour
    {
        [Inject] DimsumSpawner _dimsumSpawner;
        [Inject] GameSetting _gameSetting;
        [Inject] SoundController _soundController;

        [SerializeField] private MDropArea[] baskets;

        public GameObject successVFXPrefab;
        
        private Settings.GAME_STATUS _gameStatus;
        private float _timer = 0f;
        private int _totalGoal = 0;
        private int _currentTotal = 0;
        
        public float Timer
        {
            get => _timer;
            set => _timer = value;
        }

        public string Progress => $"{_currentTotal:D2}/{_totalGoal:D2}";
        public float ProgressPercentage => (float)_currentTotal / _totalGoal;

        private void Start()
        {
            ResetGame();
        }

        private void ResetGame()
        {
            _gameStatus = Settings.GAME_STATUS.pause;
            _currentTotal = 0;
            _totalGoal = _gameSetting.currentLevelData.TotalGoal;
            _timer = 5 * 60f;
            
            CreateLevel();
        }

        private void CreateLevel()
        {
            DimsumCombination[] currentLevel = _gameSetting.currentLevelData.currentLevel;

            DisplayedBasket[] displayedBaskets = _gameSetting.currentLevelData.firstDisplayed;
            int index = 0;
            for (int b = 0; b < baskets.Length; b++)
            {
                baskets[b].SetOpen(displayedBaskets[b], 0);
                if (displayedBaskets[b] == DisplayedBasket.Displayed)
                {
                    int totalTray = _gameSetting.currentLevelData.currentDropArea[b];
                    var randomPick = currentLevel.Skip(index).Take(totalTray).ToArray();
                    index += totalTray;
                    baskets[b].SetDimsums(randomPick);
                }
            }
        }

        public void DoStartTimer()
        {
            _gameStatus = Settings.GAME_STATUS.play;
        }

        public void DoPauseTimer()
        {
            _gameStatus = Settings.GAME_STATUS.pause;
        }

        public void DoAddProgress(int progress)
        {
            DOTween.To(() => _currentTotal, x => _currentTotal = x, _currentTotal + progress, .5f).OnComplete(OnFinishUpdateProgress);
        }

        private void OnFinishUpdateProgress()
        {
            if (_currentTotal >= _totalGoal)
            {
                _gameStatus = Settings.GAME_STATUS.win;
                ShowWin();
            }
        }

        private void Update()
        {
            if (_gameStatus == Settings.GAME_STATUS.play)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _gameStatus = Settings.GAME_STATUS.lose;
                    ShowLose();
                }
            }
        }

        private void ShowLose()
        {
            
        }

        private void ShowWin()
        {
            
        }

        public void AddSuccessVFX(Vector2 position)
        {
            _soundController.PlaySuccessClip();   
            GameObject vfx = Instantiate(successVFXPrefab, position, Quaternion.identity);
        }

    }
}