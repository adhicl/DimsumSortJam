using System.Collections;
using Commons;
using GameObjects;
using IClasses;
using Spawners;
using UnityEngine;
using Zenject;

namespace Models
{
    public class MDropArea : MonoBehaviour, IDropable
    {
        [SerializeField] Transform[] tFormationTriple;

        private int totalItems { get; set; } = 0;

        private MDimSum[] mDimSums = new MDimSum[3]
        {
            null, null, null
        };

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

        public void RemoveDimsum(int indexPosition)
        {
            if (indexPosition >= 0 && indexPosition < mDimSums.Length)
            {
                mDimSums[indexPosition] = null;
                totalItems--;
            }
        }

        public int CheckDropPosition(Transform dropTransform)
        {
            //check where to start check empty
            int indexPosition = 0;
            Vector2 pos = dropTransform.position;
            Vector2 selfPosition = this.transform.position;
            float diffPositionX = pos.x - selfPosition.x;
            float diffPositionY = pos.y - selfPosition.y;
            
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

        [Inject] SpriteCompleteBasket completeSprite;
        [Inject] DimsumSpawner dimsumSpawner;
        [SerializeField] SpriteRenderer spriteRenderer;
        
        private void CheckComplete()
        {
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
                StartCoroutine(HideAndShowFinishAnimation(checkDimsum));
            }
        }

        private IEnumerator HideAndShowFinishAnimation(int checkDimsum)
        {
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
            spriteRenderer.enabled = true;
        }

        // private void ClearDimsums()
        // {
        //     totalItems = 0;
        //     for (int i = 0; i < mDimSums.Length; i++)
        //     {
        //         if (mDimSums[i] != null)
        //         {
        //             dimsumSpawner.Remove(mDimSums[i]);
        //         }
        //
        //         mDimSums[i] = null;
        //     }
        // }

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
    }
}