using UnityEngine;

namespace IClasses
{
    public interface IDragable
    {
        public void OnStartDrag();
        public void OnEndDrag();

        public void DoDropPlaceAt(IDropable dropable, int index, bool isMove);
        public void ResetPreviousDropPlace();
    }
}