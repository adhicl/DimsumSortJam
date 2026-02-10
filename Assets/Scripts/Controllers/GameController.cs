using System;
using System.Collections.Generic;
using System.Linq;
using Commons;
using DG.Tweening;
using IClasses;
using Models;
using Ricimi;
using Spawners;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Controllers
{
    public class GameController : MonoBehaviour
    {
        [Inject] DimsumSpawner _dimsumSpawner;
        [Inject] GameSetting _gameSetting;
        [Inject] BGMController _bgmController;
        [Inject] SoundController _soundController;

        [SerializeField] private MDropArea[] baskets;
        [SerializeField] private PowerUpAnimationEffect powerUpAnimationEffect;

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
            _timer = 5f * 60f;
            
            CreateLevel();
        }

        private void CreateLevel()
        {
            DimsumCombination[] currentLevel = _gameSetting.currentLevelData.currentLevel;
            DisplayedBasket[] displayedBaskets = _gameSetting.currentLevelData.firstDisplayed;
            _gameSetting.currentDimsumSprites = GetRandomUniqueSprites();
            
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

        private Sprite[] GetRandomUniqueSprites()
        {
            // Pastikan jumlah yang diminta tidak lebih besar dari sumber
            if (_gameSetting.currentLevelData.TotalVariation > _gameSetting.dimsumSprite.Length)
            {
                Debug.LogError("Jumlah elemen yang diminta lebih besar dari array sumber!");
                return null;
            }

            // Shuffle array dengan LINQ dan Random
            Sprite[] shuffled = _gameSetting.dimsumSprite.OrderBy(x => Random.value).ToArray(); // Ambil sejumlah elemen dari hasil shuffle
            return shuffled.Take(_gameSetting.currentLevelData.TotalVariation).ToArray();
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

        public GameObject popupWin;
        public GameObject popupLose; 
        
        [SerializeField] Canvas m_canvas;
        private GameObject m_popup;

        private void ShowLose()
        {
            _bgmController.StopMusic();
            _soundController.PlayFinishOverClip();
            
            m_popup = Instantiate(popupLose, m_canvas.transform, false);
            m_popup.SetActive(true);
            m_popup.GetComponent<Popup>().Open();
        }

        private void ShowWin()
        {
            _bgmController.StopMusic();
            _soundController.PlayFinishSuccessClip();
            
            m_popup = Instantiate(popupWin, m_canvas.transform, false);
            m_popup.SetActive(true);
            m_popup.GetComponent<Popup>().Open();
        }

        public void AddSuccessVFX(Vector2 position)
        {
            _soundController.PlaySuccessClip();   
            GameObject vfx = Instantiate(successVFXPrefab, position, Quaternion.identity);
        }

        private MDimSum[] GetDimsumReadyOnTop()
        {
            Dictionary<int, List<MDimSum>> dimsumMap = new Dictionary<int, List<MDimSum>>();
            foreach (var basket in baskets)
            {
                int[] dimsumTypes = basket.GetDimsumTypes();
                for (int i = 0; i < dimsumTypes.Length; i++)
                {
                    if (dimsumTypes[i] != -1)
                    {
                        if (!dimsumMap.ContainsKey(dimsumTypes[i]))
                        {
                            dimsumMap[dimsumTypes[i]] = new List<MDimSum>();
                        }
                        dimsumMap[dimsumTypes[i]].Add(basket.mDimSums[i]);
                    }
                }
            }

            MDimSum[] targetDimsums = new MDimSum[3];
            foreach (var dimsum in dimsumMap.Keys)
            {
                if (dimsumMap[dimsum].Count >= 3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        targetDimsums[i] = dimsumMap[dimsum][i];
                    }

                    return targetDimsums;
                }
            }

            return targetDimsums;
        }

        private MDimSum[] GetDimsumsOnTop()
        {
            Dictionary<int, List<MDimSum>> dimsumMap = new Dictionary<int, List<MDimSum>>();
            foreach (var basket in baskets)
            {
                int[] dimsumTypes = basket.GetDimsumTypes();
                for (int i = 0; i < dimsumTypes.Length; i++)
                {
                    if (dimsumTypes[i] != -1)
                    {
                        if (!dimsumMap.ContainsKey(dimsumTypes[i]))
                        {
                            dimsumMap[dimsumTypes[i]] = new List<MDimSum>();
                        }
                        dimsumMap[dimsumTypes[i]].Add(basket.mDimSums[i]);
                    }
                }
            }

            MDimSum[] targetDimsums = new MDimSum[3];

            return targetDimsums;
        }

        //do power up magnifier
        public void PowerUpMagnifier()
        {
            _soundController.PlayPowerUpClip();
            MDimSum[] targetDimsums = GetDimsumReadyOnTop();
            powerUpAnimationEffect.DoAnimateMagnifier(targetDimsums);
        }

        //do power up reload
        public void PowerUpRefeshItems()
        {
            _soundController.PlayPowerUpClip();
            MDimSum[] targetDimsums = GetDimsumsOnTop();
            powerUpAnimationEffect.DoAnimateRefresh(targetDimsums);
        }

        //do power up suck package
        public void PowerUpSuckPackage()
        {
            _soundController.PlayPowerUpClip();
            MDimSum[] targetDimsums = GetDimsumReadyOnTop();
            powerUpAnimationEffect.DoAnimateSuckPower(targetDimsums);
        }
        
    }
}