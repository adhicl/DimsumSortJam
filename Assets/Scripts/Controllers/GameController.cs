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
using UI;
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
                // Anything covering the game holds the countdown: an open popup (settings,
                // unlock basket, revive) or a rewarded video, which can run for well over
                // half a minute. isTimerPause stays the power-up's own freeze.
                if (!isTimerPause && !Popup.AnyOpen && !RewardedAdController.IsShowingAd)
                {
                    _timer -= Time.deltaTime;
                }
                
                if (_timer <= 0f)
                {
                    ShowOutOfTime();
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

        [Tooltip("Shown when the player taps a closed basket: unlock with coins or a rewarded ad.")]
        public GameObject popupUnlockBasket;

        [Tooltip("Seconds added to the timer when a closed basket is unlocked, by coins or by ad.")]
        [SerializeField] private float basketUnlockTimeBonus = 20f;

        [Tooltip("Shown when the countdown reaches zero: revive by adding time, or give up.")]
        public GameObject popupOutOfTime;

        [Tooltip("Seconds added to the timer by a revive. Matches the '+45s' printed on the popup art.")]
        [SerializeField] private float reviveTimeBonus = 45f;

        [Tooltip("How many reshuffles to try before admitting the board cannot be rescued.")]
        [SerializeField] private int maxReshuffleAttempts = 5;

        // The revive popup to bring back if the player cancels the lose confirmation. Held as a
        // prefab rather than an instance because the popup is destroyed on the way to the confirm.
        private GameObject _reviveSource;

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
        

        // The countdown reached zero. The only way on is more time, so this popup sells time -
        // reshuffling the board would not help a player whose clock has run out.
        public void ShowOutOfTime()
        {
            ShowRevivePopup(popupOutOfTime != null ? popupOutOfTime : popupOutOfMove);
        }

        // Last resort for a dead board: no basket left to unlock and no reshuffle found a
        // solvable arrangement. Offers a revive before the level is actually lost.
        public void ShowOutOfMove()
        {
            ShowRevivePopup(popupOutOfMove);
        }

        private void ShowRevivePopup(GameObject prefab)
        {
            gameStatus = Settings.GAME_STATUS.pause;

            if (prefab == null)
            {
                // Nothing assigned: skip straight to the confirmation rather than stranding
                // the player in a paused level with no popup.
                ShowLoseConfirm();
                return;
            }

            _reviveSource = prefab;
            m_popup = Instantiate(prefab, m_canvas.transform, false);
            m_popup.SetActive(true);
            m_popup.GetComponent<Popup>().Open();
        }

        /// <summary>
        /// Asks the player to confirm giving up. Nothing is spent yet — the life is only
        /// charged by <see cref="CommitLose"/> if they go through with it, and cancelling
        /// brings back the revive popup they came from.
        /// </summary>
        public void ShowLoseConfirm()
        {
            gameStatus = Settings.GAME_STATUS.pause;

            if (popupLose == null)
            {
                // No confirmation available: commit rather than leave the level stuck.
                CommitLose();
                return;
            }

            m_popup = Instantiate(popupLose, m_canvas.transform, false);
            m_popup.SetActive(true);
            m_popup.GetComponent<Popup>().Open();
        }

        /// <summary>Cancelled the confirmation — put the revive popup back.</summary>
        public void ReopenRevivePopup()
        {
            if (_reviveSource == null)
            {
                gameStatus = Settings.GAME_STATUS.play;
                return;
            }
            ShowRevivePopup(_reviveSource);
        }

        /// <summary>
        /// The player confirmed. This is where the level is actually lost: one life, then Home.
        /// </summary>
        public void CommitLose()
        {
            gameStatus = Settings.GAME_STATUS.lose;

            // The one place a life is spent. A running unlimited-lives window makes it free.
            _gameSetting.TrySpendLife();

            _bgmController.StopMusic();
            _soundController.PlayFinishOverClip();
        }

        // Shown when the player taps a Closed basket. Offers two ways to open it — coins or a
        // rewarded ad — and runs <paramref name="onUnlock"/> if the player takes either. The
        // countdown is held while the popup is up so deciding does not cost the player time.
        public void ShowUnlockBasketPopup(Action onUnlock, Action onDecline = null)
        {
            // Both routes out of the popup pay the same time bonus, so it is added here rather
            // than in each button handler - there is no way to unlock without going through this.
            void Unlock()
            {
                _timer += basketUnlockTimeBonus;
                onUnlock?.Invoke();
            }

            // No popup assigned (the tutorial scenes): unlock for free rather than swallow the tap.
            if (popupUnlockBasket == null)
            {
                Unlock();
                return;
            }

            Settings.GAME_STATUS previousStatus = gameStatus;
            gameStatus = Settings.GAME_STATUS.pause;

            m_popup = Instantiate(popupUnlockBasket, m_canvas.transform, false);
            m_popup.SetActive(true);

            var popup = m_popup.GetComponent<Popup>();
            var unlockPopup = m_popup.GetComponent<UnlockBasketPopup>();

            // Restoring on close rather than in each button handler covers every way out of the
            // popup, including the X and the cancel button. When the popup was opened because the
            // board is stuck, walking away is a decision, so the caller gets told instead.
            popup.onClose += () =>
            {
                if (onDecline != null && !unlockPopup.Unlocked)
                {
                    onDecline();
                    return;
                }
                gameStatus = previousStatus;
            };

            unlockPopup.Setup(Unlock);
            popup.Open();
        }

        // Called by the out-of-move popup's REVIVE button after a rewarded ad is watched.
        public void ReviveWithTime()
        {
            _timer += reviveTimeBonus;
            isTimerPause = false;
            gameStatus = Settings.GAME_STATUS.play;
        }

        // Called by a revive popup's Leave button. Does not lose yet: the player still has to
        // confirm, and cancelling brings the revive popup back.
        public void ConfirmLose()
        {
            ShowLoseConfirm();
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
            if (HasAnyMove()) return;
            HandleNoMoves();
        }

        /// <summary>
        /// True while the player still has something to do: either a match is available on top,
        /// or some basket's contents can be moved into space that exists somewhere else.
        /// </summary>
        private bool HasAnyMove()
        {
            MDimSum[] sameDimsumOnTop = GetDimsumsOnTop();
            if (sameDimsumOnTop.Length > 0) return true;    // a match is available

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

            foreach (var basket in _gameBaskets)
            {
                if (!basket.HasStillTrayLeft()) continue;
                if (hasSingleEmptyBasket > 0 && basket.TotalFilledDimsums() == 1) return true;
                if (hasDoubleEmptyBasket > 0 && basket.TotalFilledDimsums() == 2) return true;
                if (hasTripleEmptyBasket > 0 && basket.TotalFilledDimsums() == 3) return true;
            }

            return false;
        }

        /// <summary>True when at least one basket is still closed and could be bought open.</summary>
        private bool HasLockedBasketLeft()
        {
            foreach (var basket in _gameBaskets)
            {
                if (basket.GetOpenBasket() == DisplayedBasket.Closed) return true;
            }
            return false;
        }

        /// <summary>
        /// The board is stuck. Three rescues in order of preference: sell the player an extra
        /// basket, reshuffle what is left into something playable, or — only if neither works —
        /// offer a revive.
        /// </summary>
        private void HandleNoMoves()
        {
            if (HasLockedBasketLeft())
            {
                // Space is the actual problem, so opening a basket is the fix that fits.
                // Declining is a decision to give up, and routes to the same confirmation.
                ShowUnlockBasketPopup(UnlockStuckBasket, ShowLoseConfirm);
                return;
            }

            // No basket left to sell. Reshuffling is free, so try it before charging the player
            // for anything — but a shuffle is random and can land on another dead board, so
            // check the result and try again rather than handing back the same problem.
            for (int attempt = 0; attempt < maxReshuffleAttempts; attempt++)
            {
                PowerUpRefeshItems();
                if (HasAnyMove()) return;
            }

            Debug.LogWarning($"[Game] No move after {maxReshuffleAttempts} reshuffles; offering a revive.");
            ShowOutOfMove();
        }

        // Opens the first still-closed basket. Used when the unlock popup is reached because the
        // board is stuck rather than because the player tapped a particular basket.
        private void UnlockStuckBasket()
        {
            foreach (var basket in _gameBaskets)
            {
                if (basket.GetOpenBasket() != DisplayedBasket.Closed) continue;

                _soundController.PlayBasketOpenClip();
                basket.SetOpen(DisplayedBasket.Displayed);
                return;
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