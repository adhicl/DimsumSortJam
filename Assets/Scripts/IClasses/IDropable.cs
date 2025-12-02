using Models;
using UnityEngine;

namespace IClasses
{
    public interface IDropable
    {
        public void AddDimsum(MDimSum dimsum, int indexPosition);
        public void RemoveDimsum(int indexPosition);
        
        public int CheckDropPosition(Transform dropTransform);
        
        public Vector3 GetDimsumPosition(int indexPosition);
    }
}