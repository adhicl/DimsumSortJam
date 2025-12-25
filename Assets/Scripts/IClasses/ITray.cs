using UnityEngine;

namespace IClasses
{
    public interface ITray
    {
        public void SetRendererOrder(int order);
        public Transform[] GetDimsumPositions();
        public void SetDimsums(int[] dimsums);

        public void CleanDimsums();
    }
}