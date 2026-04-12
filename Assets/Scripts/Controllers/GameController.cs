using System;
using System.Collections;
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
        private List<MDropArea> _gameBaskets;
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

        #region singleton
        public static GameController Instance { get; private set; }

        private void Awake() 
        { 
            // If there is an instance, and it's not me, delete myself.
    
            if (Instance != null && Instance != this) 
            { 
                Destroy(this); 
            } 
            else 
            { 
                Instance = this; 
            } 
        }
        #endregion
        
        public GameSetting GameSetting => _gameSetting;
        
        private void Start()
        {
            ResetGame();
        }

        private void ResetGame()
        {
            _gameStatus = Settings.GAME_STATUS.pause;
            _currentTotal = 0;
            
            _gameSetting.currentLevelData = _gameSetting.allLevelData[_gameSetting.currentLevel];
            _totalGoal = _gameSetting.currentLevelData.TotalGoal;
            
            _timer = 5f * 60f;
            
            CreateLevel();
        }

        private float width_basket = 1.5f;
        private float height_basket = 1.4f;
        
        private float[] position_top = { 2.07f, 2.8f, 3.8f, 4.2f, 4.2f };
        private float[] position_left = { -.75f, -1.5f, -2.25f, -3f };

        private void SetUpBaskets()
        {
            _gameBaskets = new List<MDropArea>();
            int totalBasket = _gameSetting.currentLevelData.currentDropArea.Length;
            
            int totalRow = Mathf.CeilToInt((float) totalBasket / 3f);
            float first_position_top = position_top[totalRow - 1];
            
            int basketIndex = 0;
            for (int i = 0; i < totalRow; i++)
            {
                int totalColumn = (totalBasket - basketIndex) >= 3 ? 3 : (totalBasket - basketIndex);
                float first_position_left = position_left[totalColumn];

                for (int j = 0; j < totalColumn; j++)
                {
                    baskets[basketIndex].gameObject.SetActive(true);
                    baskets[basketIndex].transform.localPosition = new Vector3(first_position_left + (j * width_basket), first_position_top - (i * height_basket), 0f);
                    basketIndex++;
                    _gameBaskets.Add(baskets[basketIndex]);
                }
            }

            for (int i = basketIndex; i < baskets.Length; i++)
            {
                baskets[i].gameObject.SetActive(false);
            }
        }
        
        private void CreateLevel()
        {
            int totalBasket = _gameSetting.currentLevelData.currentDropArea.Length;
            
            SetUpBaskets();
            
            DimsumCombination[] currentLevelData = _gameSetting.currentLevelData.currentLevel;
            DisplayedBasket[] displayedBaskets = _gameSetting.currentLevelData.firstDisplayed;
            _gameSetting.currentDimsumSprites = GetRandomUniqueSprites();
            
            int index = 0;

            if (_gameSetting.currentLevel == 0 || _gameSetting.currentLevel == 1)
            {
                for (int b = 0; b < totalBasket; b++)
                {
                    baskets[b].SetOpen(displayedBaskets[b]);
                    
                    if (displayedBaskets[b] == DisplayedBasket.Displayed)
                    {
                        int totalTray = _gameSetting.currentLevelData.currentDropArea[b];
                        var randomPick = currentLevelData.Skip(index).Take(totalTray).ToArray();
                        index += totalTray;
                        baskets[b].SetDimsums(randomPick);
                    }
                }
            }
            else
            {
                currentLevelData = ShuffleLevelData(currentLevelData);
                for (int b = 0; b < totalBasket; b++)
                {
                    baskets[b].SetOpen(displayedBaskets[b]);
                    if (displayedBaskets[b] == DisplayedBasket.Displayed)
                    {
                        int totalTray = _gameSetting.currentLevelData.currentDropArea[b];
                        var randomPick = currentLevelData.Skip(index).Take(totalTray).ToArray();
                        index += totalTray;
                        baskets[b].SetDimsums(randomPick);
                    }
                }
            }
            
            CheckIsGameNoMove();
        }

        private Sprite[] GetRandomUniqueSprites()
        {
            // Pastikan jumlah yang diminta tidak lebih besar dari sumber
            if (_gameSetting.currentLevelData.TotalVariation > _gameSetting.dimsumSprite.Length)
            {
                Debug.LogError("Jumlah elemen yang diminta lebih besar dari array sumber!");
                return null;
            }
            
            Sprite[] shuffled = _gameSetting.dimsumSprite.Take(_gameSetting.currentLevelData.TotalVariation).ToArray();
            shuffled = shuffled.OrderBy(x => Random.value).ToArray();
            
            return shuffled;
        }
        
        private DimsumCombination[] ShuffleLevelData(DimsumCombination[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1); // Unity's Random.Range is inclusive on min, exclusive on max
                (array[i], array[randomIndex]) = (array[randomIndex], array[i]);
            }

            return array;
        }

        public void DoStartTimer()
        {
            _gameStatus = Settings.GAME_STATUS.play;
        }

        public void DoPauseTimer()
        {
            _gameStatus = Settings.GAME_STATUS.pause;
        }

        public void CheckClearDimsum(int dimsumType)
        {
            foreach (var basket in _gameBaskets)
            {
                basket.CheckUnlockDimsum(dimsumType);
            }
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
                StartCoroutine(ShowWin());
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

        private IEnumerator ShowWin()
        {
            _bgmController.StopMusic();
            _soundController.PlayFinishSuccessClip();

            yield return new WaitForSeconds(0.2f);
            
            m_popup = Instantiate(popupWin, m_canvas.transform, false);
            m_popup.SetActive(true);
            m_popup.GetComponent<Popup>().Open();
        }

        public void AddSuccessVFX(Vector2 position)
        {
            _soundController.PlaySuccessClip();   
            GameObject vfx = Instantiate(successVFXPrefab, position, Quaternion.identity);
        }

        public void CheckIsGameNoMove()
        {
            MDimSum[] sameDimsumOnTop = GetDimsumsOnTop();
            if (sameDimsumOnTop.Length == 0)        //no matched dimsum
            {
                int hasSingleEmptyBasket = 0;
                int hasDoubleEmptyBasket = 0;
                int hasTripleEmptyBasket = 0;
                foreach (var basket in _gameBaskets)
                {
                    int totalEmpty = 3 - basket.TotalFilledDimsums();
                    if (totalEmpty == 1) hasSingleEmptyBasket++;
                    else if (totalEmpty == 2) hasDoubleEmptyBasket++;
                    else if (totalEmpty == 3) hasTripleEmptyBasket++;
                }

                bool stillHasMove = false;
                foreach (var basket in _gameBaskets)
                {
                    if (basket.HasStillTrayLeft())
                    {
                        if (hasSingleEmptyBasket > 0 && basket.TotalFilledDimsums() == 1) stillHasMove = true;
                        if (hasDoubleEmptyBasket > 0 && basket.TotalFilledDimsums() == 2) stillHasMove = true;
                        if (hasTripleEmptyBasket > 0 && basket.TotalFilledDimsums() == 3) stillHasMove = true;
                    }
                }

                if (!stillHasMove)
                {
                    ShowLose();
                }
            }
        }

        public int GetDimsumTypeOnTop()
        {
            MDimSum[] onTopDimsums = GetDimsumReadyOnTop();
            return onTopDimsums[0].dimsumType;
        }

        private MDimSum[] GetDimsumReadyOnTop()
        {
            Dictionary<int, List<MDimSum>> dimsumMap = new Dictionary<int, List<MDimSum>>();
            foreach (var basket in _gameBaskets)
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

            return Array.Empty<MDimSum>();
        }

        private MDimSum[] GetDimsumsOnTop()
        {
            Dictionary<int, List<MDimSum>> dimsumMap = new Dictionary<int, List<MDimSum>>();
            foreach (var basket in _gameBaskets)
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
        
        //do power up timer
        public void PowerUpTimerDown()
        {
            _soundController.PlayPowerUpClip();
            // MDimSum[] targetDimsums = GetDimsumReadyOnTop();
            //
            // powerUpAnimationEffect.DoAnimateSuckPower(targetDimsums);
        }
        
    }
}