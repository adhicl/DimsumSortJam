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
using UnityEngine.Serialization;
using Zenject;
using Random = UnityEngine.Random;

namespace Controllers
{
    public class GameController : MonoBehaviour
    {
        [Inject] DimsumSpawner _dimsumSpawner;
        [Inject] private CharacterSpawner _characterSpawner;
        public GameSetting _gameSetting;
        [Inject] BGMController _bgmController;
        [Inject] SoundController _soundController;

        [SerializeField] private MDropArea[] baskets;
        private List<MDropArea> _gameBaskets;
        [SerializeField] private PowerUpAnimationEffect powerUpAnimationEffect;

        private List<RequestCharacter> _requestCharacters = new(); 
        private readonly List<MCharacter> _characterSpawns = new();

        public GameObject successVFXPrefab;
        
        public Settings.GAME_STATUS gameStatus;
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
            gameStatus = Settings.GAME_STATUS.pause;
            _currentTotal = 0;
            
            _gameSetting.currentLevelData = _gameSetting.allLevelData[_gameSetting.currentLevel];
            _totalGoal = _gameSetting.currentLevelData.TotalGoal;

            _requestCharacters = new List<RequestCharacter>();
            for (int i = 0; i < _gameSetting.currentLevelData.requestMissions.Length; i++)
            {
                _requestCharacters.Add(_gameSetting.currentLevelData.requestMissions[i]);
            }
            
            _timer = 5f * 60f;
            isTimerPause = false;
            
            CreateLevel();
        }

        public void ContinueGame()
        {
            gameStatus = Settings.GAME_STATUS.pause;

            foreach (var characterSpawn in _characterSpawns)
            {
                characterSpawn.SetAsFinish();
            }
        }

        private readonly float width_basket = 1.5f;
        private readonly float height_basket = 1.4f;
        
        private readonly float[] position_top = { 2.07f, 2.8f, 3.8f, 4.2f, 4.2f };
        private readonly float[] position_left = { -.75f, -1.5f, -2.25f, -3f };

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
                    _gameBaskets.Add(baskets[basketIndex]);
                    basketIndex++;
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
            CheckShowRequest();
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
        
        private MDimSum[] shuffleDimSums(MDimSum[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1); // Unity's Random.Range is inclusive on min, exclusive on max
                (array[i], array[randomIndex]) = (array[randomIndex], array[i]);
            }

            return array;
        }

        private bool hasStartGame = false;
        public void DoStartTimer()
        {
            if (!hasStartGame)
            {
                hasStartGame = true;
            }
            gameStatus = Settings.GAME_STATUS.play;
        }

        public void DoPauseTimer()
        {
            gameStatus = Settings.GAME_STATUS.pause;
        }

        private void StartShowCharacter()
        {
            RequestCharacter request = _requestCharacters[0];
            
            _soundController.PlayBikeBellSoundClips();
            MDimSum[] dimsumsOnTop;
            if (request.getOnTopOnly)
            {
                dimsumsOnTop = GetDimsumReadyOnTop();
            }
            else
            {
                dimsumsOnTop = GetAvailableDimsums();
            }
                
            dimsumsOnTop = shuffleDimSums(dimsumsOnTop);
            
            MCharacter mCharacter = _characterSpawner.Create();
            mCharacter.SetRequest(dimsumsOnTop.Take(request.totalRequestItems).ToArray(), new Vector2(-1f, 2.7f));
            _characterSpawns.Add(mCharacter);

            SetCharacterPosition();
            
            _requestCharacters.RemoveAt(0);
        }

        private void SetCharacterPosition()
        {
            float positionY = 2.7f;
            if (_characterSpawns.Count == 1)
            {
                _characterSpawns[0].SetMoveTo(new Vector2(-1f, positionY));
            }
            else if (_characterSpawns.Count > 1)
            {
                _characterSpawns[0].SetMoveTo(new Vector2(.5f, positionY));
                _characterSpawns[1].SetMoveTo(new Vector2(-2f, positionY));
            }
        }

        private float position_x = -1f;

        public void RemoveCharacter(MCharacter character)
        {
            _characterSpawns.Remove(character);
            _characterSpawner.Remove(character);

            SetCharacterPosition();
        }

        public void CheckClearDimsum(int dimsumType)
        {
            foreach (var basket in _gameBaskets)
            {
                basket.CheckUnlockDimsum(dimsumType);
            }

            foreach (var characterSpawn in _characterSpawns)
            {
                characterSpawn.CheckClearRequest(dimsumType);
            }
        }

        public void DoAddProgress(int progress)
        {
            DOTween.To(() => _currentTotal, x => _currentTotal = x, _currentTotal + progress, .5f).OnComplete(OnFinishUpdateProgress);
        }

        private void OnFinishUpdateProgress()
        {
            CheckShowRequest();
            if (_currentTotal >= _totalGoal)
            {
                gameStatus = Settings.GAME_STATUS.win;
                StartCoroutine(ShowWin());
            }
        }

        private void Update()
        {
            if (gameStatus == Settings.GAME_STATUS.play)
            {
                if (!isTimerPause)
                {
                    _timer -= Time.deltaTime;
                }
                
                if (_timer <= 0f)
                {
                    ShowOutOfMove();
                }
            }
        }

        private void CheckShowRequest()
        {
            if (_requestCharacters.Count > 0)
            {
                if (_currentTotal >= _requestCharacters[0].requestTimeShow)
                {
                    StartShowCharacter();
                }
            }
        }

        public GameObject popupWin;
        public GameObject popupLose;
        public GameObject popupRequestLose;
        public GameObject popupOutOfMove;

        [Tooltip("Seconds added to the timer when the player revives by watching an ad.")]
        [SerializeField] private float reviveTimeBonus = 60f;

        public bool isTimerPause = false;
        
        [SerializeField] Canvas m_canvas;
        private GameObject m_popup;

        public void CallGameLose()
        {
            gameStatus = Settings.GAME_STATUS.lose;
            
            _bgmController.StopMusic();

            m_popup = Instantiate(popupRequestLose, m_canvas.transform, false);
            m_popup.SetActive(true);
            m_popup.GetComponent<Popup>().Open();
        }
        
        private void ShowLose()
        {
            // The one place a life is actually spent. Every give-up path funnels through here,
            // and a running unlimited-lives window makes it free — see GameSetting.TrySpendLife.
            _gameSetting.TrySpendLife();

            _bgmController.StopMusic();
            _soundController.PlayFinishOverClip();

            m_popup = Instantiate(popupLose, m_canvas.transform, false);
            m_popup.SetActive(true);
            m_popup.GetComponent<Popup>().Open();
        }

        // Shown when the player runs out of moves or time. Offers a revive (watch an ad
        // for extra time) before the game is actually lost. The music keeps playing so a
        // revive resumes seamlessly; giving up routes to ShowLose via ConfirmLose().
        public void ShowOutOfMove()
        {
            gameStatus = Settings.GAME_STATUS.pause;

            if (popupOutOfMove == null)
            {
                // No revive popup assigned: fall back to the normal lose flow.
                ShowLose();
                return;
            }

            m_popup = Instantiate(popupOutOfMove, m_canvas.transform, false);
            m_popup.SetActive(true);
            m_popup.GetComponent<Popup>().Open();
        }

        // Called by the out-of-move popup's REVIVE button after a rewarded ad is watched.
        public void ReviveWithTime()
        {
            _timer += reviveTimeBonus;
            isTimerPause = false;
            gameStatus = Settings.GAME_STATUS.play;
        }

        // Called by the out-of-move popup's Leave button: commit to the loss.
        public void ConfirmLose()
        {
            ShowLose();
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
                    ShowOutOfMove();
                }
            }
        }

        public int GetDimsumTypeOnTop()
        {
            MDimSum[] onTopDimsums = GetDimsumReadyOnTop();
            return onTopDimsums[0].dimsumType;
        }

        private MDimSum[] GetAvailableDimsums()
        {
            Dictionary<int, MDimSum> availableDimsums = new Dictionary<int, MDimSum>();
            foreach (var basket in _gameBaskets)
            {
                MDimSum[] dimsumTypes = basket.GetAllDimsumTypeInsides();
                for (int i = 0; i < dimsumTypes.Length; i++)
                {
                    if (dimsumTypes[i].dimsumType != -1)
                    {
                        if (!availableDimsums.ContainsKey(dimsumTypes[i].dimsumType))
                        {
                            availableDimsums.Add(dimsumTypes[i].dimsumType, dimsumTypes[i]);
                        }
                    }
                }
            }

            return availableDimsums.Values.ToArray();
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

        private int[] leftOverBasketLength;
        private DimsumCombination[] GetLeftOverDimsumCombinations()
        {
            leftOverBasketLength = new int[baskets.Length];
            List<DimsumCombination> combinations = new List<DimsumCombination>();
            for (int i = 0; i < baskets.Length; i++)
            {
                DimsumCombination[] leftDimsums = baskets[i].GetLeftDimsums();
                combinations.AddRange(leftDimsums);
                leftOverBasketLength[i] = leftDimsums.Length;
            }

            return combinations.ToArray();
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
            
            DimsumCombination[] targetDimsums = GetLeftOverDimsumCombinations();
            targetDimsums = ShuffleLevelData(targetDimsums);

            int totalBasketLeft = 0;
            foreach (var basket in _gameBaskets)
            {
                if (basket.GetOpenBasket() == DisplayedBasket.Displayed)
                {
                    totalBasketLeft++;
                }
                basket.DrawToTop();
            }
            
            int dividedBasketLeft = Mathf.FloorToInt((float) targetDimsums.Length /(float) totalBasketLeft);
            int skipIndex = 0;
            for (int i = 0; i < baskets.Length; i++)
            {
                MDropArea basket = baskets[i];
                if (basket.GetOpenBasket() == DisplayedBasket.Displayed)
                {
                    //Debug.Log($"Basket {i}/{totalBasketLeft} Skip {skipIndex} Take {leftOverBasketLength[i]}");
                    basket.BackToBottom(targetDimsums.Skip(skipIndex).Take(leftOverBasketLength[i]).ToArray());
                    
                    skipIndex += leftOverBasketLength[i];
                }
            }
            
            //Debug.Log(targetDimsums.Length);
            powerUpAnimationEffect.DoAnimateRefresh();
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

            isTimerPause = true;
            powerUpAnimationEffect.DoAnimateFreezeTimer();

            StartCoroutine(RestartTimerAgain());
        }

        private IEnumerator RestartTimerAgain()
        {
            yield return new WaitForSeconds(15f);
            isTimerPause = false;
        }
        
    }
}