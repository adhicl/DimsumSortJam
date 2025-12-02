using System;
using IClasses;
using UnityEngine;

namespace Models
{
    public class MDropArea : MonoBehaviour, IDropable
    {
        [SerializeField] Transform[] tFormationTriple;

        private int totalItems { get; set; }
        private MDimSum[] mDimSums;

        private void Start()
        {
            mDimSums = new MDimSum[3]
            {
                null, null, null
            };
            totalItems = 0;
        }

        public void AddDimsum(MDimSum dimsum, int indexPosition)
        {
            if (mDimSums[indexPosition] == null)
            {
                mDimSums[indexPosition] = dimsum;
                totalItems++;
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
            
            if (diffPositionY < 0)
            {
                if (diffPositionX < (-1 * Commons.Settings.THRESHOLD_WIDTH))
                {
                    indexPosition = 0;
                }
                else if (diffPositionX > Commons.Settings.THRESHOLD_WIDTH)
                {
                    indexPosition = 2;
                }
            }
            else
            {
                indexPosition = 1;
            }
            
            //check empty
            bool hasEmpty = false;
            for (int i = indexPosition; i < mDimSums.Length; i++)
            {
                Debug.Log($"is Empty {i} = "+mDimSums[i]);
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
    }
}