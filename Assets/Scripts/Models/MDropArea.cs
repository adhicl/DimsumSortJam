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
                    dimsumCombinations[index] = arrayDimsum;
                    index++;
                }

                return dimsumCombinations;
            }
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
                    _gameController.CheckIsGameNoMove();
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

        [Inject] private GameSetting _gameSetting;
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

        private IEnumerator HideAndShowFinishAnimation(int checkDimsum)
        {
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
            
            gFrontBasket.SetActive(true);
            gShadowBasket.SetActive(true);
            spriteRenderer.enabled = true;
            CreateDimsumFromTray();
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
                
                if (i >= arrayDimsums.Count - 1) newTray.SetDimsums(arrayDimsums[0].ToArray());
            }
        }

        private void CreateDimsum()
        {
            DimsumCombination firstCombination = arrayDimsums[0];
            arrayDimsums.RemoveAt(0);
            int[] row = firstCombination.ToArray();
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] != -1)
                {
                    MDimSum newDimsum = dimsumSpawner.Create(row[i]);
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

            Transform[] trayTransforms = trayList[0].GetDimsumPositions();
            
            DimsumCombination firstCombination = arrayDimsums[0];
            arrayDimsums.RemoveAt(0);
            int[] row = firstCombination.ToArray();
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] != -1)
                {
                    MDimSum newDimsum = dimsumSpawner.Create(row[i]);
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

                if (trayList.Count > 0)
                {
                    trayList[0].SetDimsums(arrayDimsums[0].ToArray());
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
                if (mDimSum.dimsumType != -1) total++;
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
            if (_isOpen == DisplayedBasket.Closed)
            {
                SoundController.Instance.PlayBasketOpenClip();
                //SetOpen(DisplayedBasket.Displayed);
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

        public void BackToBottom(DimsumCombination[] dimsumArray)
        {
            StartCoroutine(RecreateDropArea(dimsumArray));
        }

        private IEnumerator RecreateDropArea(DimsumCombination[] dimsumArray)
        {
            yield return new WaitForSeconds(2f);
            
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
            // gLockedBasket.SetActive(false);
            // gUnlockPaper.SetActive(true);
            this.transform.localEulerAngles = Vector3.zero;
        }
        
        #endregion
    }
}