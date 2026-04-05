using Commons;
using Models;
using UnityEngine;

namespace IClasses
{
    public interface IDropable
    {
        public void SetDimsums(DimsumCombination[] dimsumArray);
        
        public void AddDimsum(MDimSum dimsum, int indexPosition);
        public void RemoveDimsum(int indexPosition, IDropable previous);
        
        public int CheckDropPosition(Transform dropTransform);
        
        public Vector3 GetDimsumPosition(int indexPosition);
    }
}