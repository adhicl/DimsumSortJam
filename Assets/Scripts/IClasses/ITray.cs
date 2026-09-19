using Commons;
using UnityEngine;

namespace IClasses
{
    public interface ITray
    {
        public void SetRendererOrder(int order);
        public Transform[] GetDimsumPositions();
        public void SetDimsums(DimsumCombination combination);

        public void CleanDimsums();
    }
}