using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Commons;
using Controllers;
using DG.Tweening;
using GameObjects;
using IClasses;
using Spawners;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Models
{
    public class MDropArea : MonoBehaviour, IDropable
    {
        [SerializeField] Transform[] tFormationTriple;
        [SerializeField] private Transform trayPlacement;
        [SerializeField] private GameObject gBasketClose;
        [SerializeField] private GameObject gLockedBasket;
        [SerializeField] private GameObject gUnlockPaper;
        [SerializeField] private GameObject gFrontBasket;
        [SerializeField] private GameObject gShadowBasket;
        [SerializeField] private SpriteRenderer sUnlockItem;
        [SerializeField] private Animator animator;
        
        private static readonly int OpenBasket = Animator.StringToHash("OpenBasket");
        private static readonly int Reset = Animator.StringToHash("Reset");

        private int totalItems { get; set; } = 0;
        
        public MDimSum[] mDimSums = new MDimSum[3]
        {
            null, null, null
        };

        public int[] GetDimsumTypes()
        {
            int[] dimsums = new int[3];
            for (int i = 0; i < 3; i++)
            {
                if (mDimSums[i] != null)
                {
                    dimsums[i] = mDimSums[i].dimsumType;
                }
                else
                {
                    dimsums[i] = -1;
                }
            }
            return dimsums;
        }

        /// <summary>
        /// Everything this basket still holds, top row first, for a reshuffle to redeal.
        ///
        /// The ice does not survive the trip, here or in the rows below. A reshuffle is the
        /// game's answer to a board with no move left in it, and frozen pieces are one of the
        /// things that can cause that - so a reshuffle that preserved the ice could hand back the
        /// same dead board five times and then lose the level for the player. It is also what the
        /// animation says happens: the pieces fly up and come back as a fresh deal.
        ///
        /// Hidden rows stay hidden. They cannot deadlock anything, and revealing the whole queue
        /// would make the refresh power-up a way to buy information rather than a way out.
        /// </summary>
        public DimsumCombination[] GetLeftDimsums()
        {
            DimsumCombination dimsumCombination = new DimsumCombination();
            dimsumCombination.dimsum1 = mDimSums[0] == null ? -1 : mDimSums[0].dimsumType;
            dimsumCombination.dimsum2 = mDimSums[1] == null ? -1 : mDimSums[1].dimsumType;
            dimsumCombination.dimsum3 = mDimSums[2] == null ? -1 : mDimSums[2].dimsumType;
            
            
            // Debug.Log("Get left dimsums");
            // Debug.Log(arrayDimsums);
            // Debug.Log($"{dimsumCombination.dimsum1}.{dimsumCombination.dimsum2}.{dimsumCombination.dimsum3} Combinations left: {arrayDimsums.Count}");
            
            if (dimsumCombination.isEmpty())
            {
                return Array.Empty<DimsumCombination>();
            }
            else
            {
                DimsumCombination[] dimsumCombinations = new DimsumCombination[arrayDimsums.Count + 1];
                dimsumCombinations[0] = dimsumCombination;
                int index = 1;
                foreach (var arrayDimsum in arrayDimsums)
                {
                    DimsumCombination thawed = arrayDimsum;
                    thawed.frozenMask = 0;
                    dimsumCombinations[index] = thawed;
                    index++;
                }

                return dimsumCombinations;
            }
        }

        /// <summary>
        /// True when at least one piece on top of this basket is frozen, so the basket cannot be
        /// emptied by dragging. The whole point of the mechanic, and the reason the no-move check
        /// has to ask: a basket pinned by ice is not a basket the player can clear.
        /// </summary>
        public bool HasFrozenDimsum()
        {
            foreach (var dimsum in mDimSums)
            {
                if (dimsum != null && dimsum.IsFrozen) return true;
            }
            return false;
        }

        /// <summary>How many pieces on top can actually be picked up.</summary>
        public int MovableDimsums()
        {
            int total = 0;
            foreach (var dimsum in mDimSums)
            {
                if (dimsum != null && dimsum.dimsumType != -1 && !dimsum.IsFrozen) total++;
            }
            return total;
        }

        /// <summary>
        /// Melts every frozen piece on top of this basket and returns how many gave way, so the
        /// caller can keep counting across baskets and spread the wiggles out. Quiet if there are
        /// none - callers thaw the whole board without checking first.
        /// </summary>
        public int ThawFrozenDimsums(float firstDelay, float stagger, float maxDelay)
        {
            int thawed = 0;
            foreach (var dimsum in mDimSums)
            {
                if (dimsum == null || !dimsum.IsFrozen) continue;

                // Clamped per piece, not per basket. Capping only where a basket starts still let
                // the pieces inside it walk past the cap, so a heavily iced board finished later
                // than the ceiling claimed.
                dimsum.Thaw(Mathf.Min(firstDelay + thawed * stagger, maxDelay));
                thawed++;
            }
            return thawed;
        }

        public MDimSum[] GetAllDimsumTypeInsides()
        {
            Dictionary<int, MDimSum> dimsumMap = new Dictionary<int, MDimSum>();
            
            for (int i = 0; i < 3; i++)
            {
                if (mDimSums[i] != null)
                {
                    if (!dimsumMap.ContainsKey(mDimSums[i].dimsumType))
                    {
                        dimsumMap.Add(mDimSums[i].dimsumType, mDimSums[i]);
                    }
                }
            }

            foreach (var arrayDimsum in arrayDimsums)
            {
                if (!dimsumMap.ContainsKey(arrayDimsum.dimsum1) && arrayDimsum.dimsum1 != -1)
                {
                    MDimSum dimsum = new MDimSum();
                    dimsum.dimsumType = arrayDimsum.dimsum1;
                    dimsumMap.Add(arrayDimsum.dimsum1, dimsum);
                }
                if (!dimsumMap.ContainsKey(arrayDimsum.dimsum2) && arrayDimsum.dimsum2 != -1)
                {
                    MDimSum dimsum = new MDimSum();
                    dimsum.dimsumType = arrayDimsum.dimsum2;
                    dimsumMap.Add(arrayDimsum.dimsum2, dimsum);
                }
                if (!dimsumMap.ContainsKey(arrayDimsum.dimsum3) && arrayDimsum.dimsum3 != -1)
                {
                    MDimSum dimsum = new MDimSum();
                    dimsum.dimsumType = arrayDimsum.dimsum3;
                    dimsumMap.Add(arrayDimsum.dimsum3, dimsum);
                }
            }

            return dimsumMap.Values.ToArray();
        }

        public void AddDimsum(MDimSum dimsum, int indexPosition)
        {
            if (mDimSums[indexPosition] == null)
            {
                mDimSums[indexPosition] = dimsum;
                totalItems++;
                if (totalItems >= 3)
                {
                    CheckComplete();
                }
            }
            else
            {
                Debug.LogError("Duplicate dimsum");
            }
        }

        public void RemoveDimsum(int indexPosition, IDropable previous)
        {
            if (indexPosition >= 0 && indexPosition < mDimSums.Length)
            {
                mDimSums[indexPosition] = null;
                totalItems--;

                if (totalItems <= 0 && (MDropArea) previous != this)
                {
                    CreateDimsumFromTray();

                    // Deferred, not immediate. OnEndDrag calls this before it places the piece in
                    // its new basket, so right now the dragged piece is on no basket at all and the
                    // board reads emptier than it is - which on the move that finishes a level
                    // looked exactly like a stuck board.
                    _gameController.RequestNoMoveCheck();
                }
            }
        }

        public int CheckDropPosition(Transform dropTransform)
        {
            if (_isOpen != DisplayedBasket.Displayed)
            {
                return -1;
            }
            
            //check where to start check empty
            int indexPosition = 0;
            Vector2 pos = dropTransform.position;
            Vector2 selfPosition = this.transform.position;
            float diffPositionX = pos.x - selfPosition.x;
            float diffPositionY = pos.y - selfPosition.y;
            
            //Debug.Log($"CheckDropPosition {diffPositionX}, {diffPositionY}");
            
            if (diffPositionY < Settings.THRESHOLD_HEIGHT)
            {
                if (diffPositionX < 0f)
                {
                    indexPosition = 0;
                }
                else if (diffPositionX >= 0f)
                {
                    indexPosition = 2;
                }
            }
            else
            {
                indexPosition = 1;
            }

            //Debug.Log($"Diff position {diffPositionX},{diffPositionY} => {indexPosition}");
            //PrintDimsums();
            
            //check empty
            bool hasEmpty = false;
            for (int i = indexPosition; i < mDimSums.Length; i++)
            {
                if (mDimSums[i] == null)
                {
                    hasEmpty = true;
                    indexPosition = i;
                    break;
                }
            }

            if (!hasEmpty)
            {
                for (int i = 0; i < indexPosition; i++)
                {
                    if (mDimSums[i] == null)
                    {
                        hasEmpty = true;
                        indexPosition = i;
                        break;
                    }
                }
            }

            if (hasEmpty)
            {
                return indexPosition;
            }
            return -1;
        }

        public Vector3 GetDimsumPosition(int indexPosition)
        {
            return tFormationTriple[indexPosition].transform.position;
        }

        public GameSetting _gameSetting;
        [Inject] SpriteCompleteBasket completeSprite;
        [Inject] DimsumSpawner dimsumSpawner;
        [Inject] private TraySpawner _traySpawner;
        [Inject] private GameController _gameController;
        
        [SerializeField] SpriteRenderer spriteRenderer;
        
        private void CheckComplete()
        {
            if (mDimSums.Length < 3) return;
            
            int checkDimsum = mDimSums[0].dimsumType;
            bool isComplete = true;
            for (int i = 0; i < totalItems; i++)
            {
                if (checkDimsum != mDimSums[i].dimsumType)
                {
                    isComplete = false;
                    break;
                }
            }

            if (isComplete)
            {
                _gameController.CheckClearDimsum(checkDimsum);
                _gameController.DoAddProgress(3);
                _gameController.AddSuccessVFX(this.transform.position + new Vector3(0f, 1f, 0f));
                StartCoroutine(HideAndShowFinishAnimation(checkDimsum));
            }
        }

        /// <summary>
        /// True while the three-in-a-row clear animation is playing. The slots are emptied at the
        /// top of it and only refilled a second later, so a board read taken partway through sees
        /// this basket as empty when it is simply mid-animation.
        /// </summary>
        public bool IsClearing { get; private set; }

        private IEnumerator HideAndShowFinishAnimation(int checkDimsum)
        {
            IsClearing = true;
            gFrontBasket.SetActive(false);
            gShadowBasket.SetActive(false);
            spriteRenderer.enabled = false;
            totalItems = 0;
            for (int i = 0; i < mDimSums.Length; i++)
            {
                if (mDimSums[i] != null)
                {
                    dimsumSpawner.Remove(mDimSums[i]);
                }

                mDimSums[i] = null;
            }
            completeSprite.SetDimsumSprites(checkDimsum, this.transform.position);
            yield return new WaitForSeconds(1f);
            
            // Wrapped so the flag always comes back down. A throw in here left the basket
            // claiming to be mid-clear for the rest of the level, and anything waiting on that -
            // the deferred no-move check waits on it in a loop - would have waited forever.
            try
            {
                gFrontBasket.SetActive(true);
                gShadowBasket.SetActive(true);
                spriteRenderer.enabled = true;
                CreateDimsumFromTray();
            }
            finally
            {
                IsClearing = false;
            }
        }

        private List<DimsumCombination> arrayDimsums = new();
        private List<MTray> trayList = new();

        public void SetDimsums(DimsumCombination[] dimsumArray)
        {
            //Debug.Log($" set dimsum length: {dimsumArray.Length}");
            arrayDimsums = new List<DimsumCombination>();
            foreach (var combination in dimsumArray)
            {
                arrayDimsums.Add(combination);
            }

            CreateDimsum();
            CreateTray();
        }
        
        private void CreateTray()
        {
            //Debug.Log($"Create Tray {arrayDimsums.Count}");
            for (int i = 0; i < arrayDimsums.Count; i++)
            {
                MTray newTray = _traySpawner.Create(trayPlacement);
                Vector3 position = trayPlacement.position + new Vector3(0f, i * Settings.TRAY_HEIGHT, 0f);
                newTray.transform.position = position;
                newTray.transform.localScale = Vector3.one;
                newTray.SetRendererOrder(10 + i);
                trayList.Add(newTray);
                
                if (i >= arrayDimsums.Count - 1) newTray.SetDimsums(arrayDimsums[0]);
            }
        }

        private void CreateDimsum()
        {
            // A basket can be handed an empty deal: a reshuffle on a nearly finished board gives
            // one to every basket that has nothing left in it. Indexing [0] threw here, and because
            // that throw unwound RecreateDropArea before it could clear _rebuildRoutine, the basket
            // stayed "rebuilding" for good and the reshuffle waiting on it never finished.
            if (arrayDimsums.Count == 0) return;

            DimsumCombination firstCombination = arrayDimsums[0];
            arrayDimsums.RemoveAt(0);
            int[] row = firstCombination.ToArray();
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] != -1)
                {
                    MDimSum newDimsum = dimsumSpawner.Create(row[i], firstCombination.IsFrozen(i));
                    newDimsum.DoDropPlaceAt(this, i, false);
                }
                else
                {
                    
                }
            }
        }

        private void CreateDimsumFromTray()
        {
            if (arrayDimsums.Count <= 0) return;

            // No plate to deal from. DrawToTop empties trayList the instant a reshuffle is asked
            // for, but RecreateDropArea only refills it two seconds later - so a basket that
            // completes a match inside that window has rows waiting and nothing to animate them out
            // of, and trayList[^1] threw. The pending rebuild replaces arrayDimsums wholesale, so
            // nothing is lost by leaving these rows alone until it lands.
            if (trayList.Count == 0) return;

            // The front of the stack — the plate the player can actually see, and the only one
            // carrying sprites. The new pieces animate out of it, so its slots are the start
            // positions. (CreateTray fills the LAST entry, which has the highest sorting order.)
            Transform[] trayTransforms = trayList[^1].GetDimsumPositions();

            DimsumCombination firstCombination = arrayDimsums[0];
            arrayDimsums.RemoveAt(0);
            int[] row = firstCombination.ToArray();
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] != -1)
                {
                    // Whatever the plate was showing, the piece that comes up is the real dish:
                    // being dealt IS the reveal, so hiddenMask is deliberately not consulted here.
                    MDimSum newDimsum = dimsumSpawner.Create(row[i], firstCombination.IsFrozen(i));
                    newDimsum.transform.position = trayTransforms[i].position;
                    newDimsum.transform.localScale = Vector3.one * 0.7f;
                    newDimsum.transform.DOScale(Vector3.one, 0.2f);
                    newDimsum.DoDropPlaceAt(this, i, true);
                }
            }

            MTray mTray = trayList[^1];
            mTray.CleanDimsums();
            Sequence sequence = DOTween.Sequence();
            sequence.Append(mTray.GetComponent<SpriteRenderer>().DOFade(0f, .6f));
            sequence.onComplete += () =>
            {
                trayList.Remove(mTray);
                _traySpawner.Remove(mTray);

                // Refill the tray that is now at the FRONT of the stack. CreateTray puts the
                // sprites on the last entry — the highest sorting order — so the front is
                // trayList[^1], not trayList[0]. Filling [0] painted the plate hidden at the
                // back and left the visible one empty, which is what "the item in the plate is
                // not rendered" was: it only showed up on baskets with three or more trays,
                // because with one or two the two indices happen to be the same tray.
                //
                // The count check matters too: arrayDimsums was emptied at the top of this
                // method on the last row, and arrayDimsums[0] then threw inside this callback.
                if (trayList.Count > 0 && arrayDimsums.Count > 0)
                {
                    trayList[^1].SetDimsums(arrayDimsums[0]);
                }
            };
        }

        public bool HasStillTrayLeft()
        {
            return arrayDimsums.Count > 0;
        }

        public int TotalFilledDimsums()
        {
            int total = 0;
            foreach (var mDimSum in mDimSums)
            {
                // Empty slots really are null — RemoveDimsum nulls them, and CreateDimsum skips
                // the -1 entries of a partly filled row. Without this guard an emptied basket
                // with no trays left threw here, and because the throw unwinds through
                // RemoveDimsum it aborted OnEndDrag before DoDropPlaceAt ever ran: the piece
                // being dropped was never placed into the basket.
                if (mDimSum != null && mDimSum.dimsumType != -1) total++;
            }

            return total;
        }

        private void PrintDimsums()
        {
            var text = "";
            var i = 0;
            foreach (var dimSum in mDimSums)
            {
                if (dimSum != null)
                text += i+". "+dimSum.name + "\n";
                i++;
            }
            Debug.Log(text);
        }
        
        [SerializeField] private DisplayedBasket _isOpen;
        [SerializeField] private int dimsumUnlock = -1;

        public DisplayedBasket GetOpenBasket()
        {
            return _isOpen;
        }

        public void SetOpen(DisplayedBasket isOpen)
        {
            if (isOpen == DisplayedBasket.Displayed)
            {
                gBasketClose.SetActive(false);
                gLockedBasket.SetActive(true);
                gUnlockPaper.SetActive(false);
                animator.SetTrigger(OpenBasket);
            }
            else if (isOpen == DisplayedBasket.Locked)
            {
                int iDimsumUnlock = Random.Range(0, _gameSetting.currentLevelData.TotalVariation - 1);
                if (_gameSetting.currentLevel == 3)
                {
                    iDimsumUnlock = _gameController.GetDimsumTypeOnTop();
                }
                
                gLockedBasket.SetActive(true);
                gUnlockPaper.SetActive(true);
                sUnlockItem.sprite = _gameSetting.currentDimsumSprites[iDimsumUnlock];
                dimsumUnlock = iDimsumUnlock;
            }
            else if (isOpen == DisplayedBasket.Closed)
            {
                gBasketClose.SetActive(true);
            }
            _isOpen = isOpen;
        }

        public void CheckUnlockDimsum(int dimsumType)
        {
            //Debug.Log($"Check unlock {dimsumType} -> {dimsumUnlock} can lock?");
            if (dimsumUnlock == dimsumType && _isOpen == DisplayedBasket.Locked)
            {
                SoundController.Instance.PlayBasketOpenClip();
                SetOpen(DisplayedBasket.Displayed);
            }
        }

        private void OnMouseDown()
        {
            // Without this, tapping a closed basket through an open popup would stack a second
            // popup — the unlock offer — on top of the one already on screen.
            if (_gameController != null && !_gameController.AcceptsBoardInput) return;

            // A Closed basket (as opposed to a Locked one, which opens by matching its
            // dimsum) is unlocked through the unlock popup: pay coins or watch a rewarded ad.
            // Either way it becomes a normal empty Displayed basket the player can sort into.
            if (_isOpen == DisplayedBasket.Closed)
            {
                SoundController.Instance.PlayButtonClickClip();

                void UnlockBasket()
                {
                    SoundController.Instance.PlayBasketOpenClip();
                    SetOpen(DisplayedBasket.Displayed);
                }

                _gameController.ShowUnlockBasketPopup(UnlockBasket);
            }
        }
        
        #region animation_effect
        
        public void DrawToTop()
        {
            if (_isOpen == DisplayedBasket.Displayed)
            {
                foreach (var mTray in trayList)
                {
                    _traySpawner.Remove(mTray);
                }
                trayList.Clear();

                gLockedBasket.GetComponent<SpriteRenderer>().color = Color.white;
                gLockedBasket.SetActive(true);
                gUnlockPaper.SetActive(false);
                animator.SetTrigger(Reset);

                this.transform.DOShakeRotation(3f, 15f);
            }
        }

        /// <summary>True from the moment a reshuffle is requested until the basket is rebuilt.</summary>
        public bool IsRebuilding => _rebuildRoutine != null;

        private Coroutine _rebuildRoutine;

        public void BackToBottom(DimsumCombination[] dimsumArray)
        {
            // A basket this level does not use is deactivated but keeps whatever open flag it was
            // serialised with, so it can still look like a rebuild target to a caller. Starting a
            // coroutine on an inactive object throws, and that throw took the whole reshuffle with
            // it. The callers now filter these out; this is the backstop.
            if (!gameObject.activeInHierarchy) return;

            // The rebuild only lands two seconds later, so a second reshuffle arriving before then
            // used to stack another one on top: both fired together, each despawning the pieces the
            // other had just created and each appending a whole new stack to trayList. The visible
            // result was duplicated plates with pieces missing under them.
            if (_rebuildRoutine != null) StopCoroutine(_rebuildRoutine);
            _rebuildRoutine = StartCoroutine(RecreateDropArea(dimsumArray));
        }

        private IEnumerator RecreateDropArea(DimsumCombination[] dimsumArray)
        {
            yield return new WaitForSeconds(2f);

            // Everything below is wrapped so the basket can never be left claiming to be
            // rebuilding. Anything waiting on IsRebuilding - the automatic reshuffle does, in a
            // loop - would otherwise wait for a rebuild that already died, forever.
            try
            {
                for (int i = 0; i < mDimSums.Length; i++)
                {
                    if (mDimSums[i] != null)
                    {
                        dimsumSpawner.Remove(mDimSums[i]);
                    }

                    mDimSums[i] = null;
                }
                totalItems = 0;

                arrayDimsums = new List<DimsumCombination>();
                foreach (var combination in dimsumArray)
                {
                    arrayDimsums.Add(combination);
                }

                CreateDimsum();
                CreateTray();

                gLockedBasket.SetActive(true);
                gUnlockPaper.SetActive(false);
                animator.SetTrigger(OpenBasket);
                this.transform.localEulerAngles = Vector3.zero;
            }
            finally
            {
                _rebuildRoutine = null;
            }
        }
        
        #endregion
    }
}