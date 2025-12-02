using UnityEngine;

namespace IClasses
{
    public interface IDragable
    {
        public void OnStartDrag();
        public void OnEndDrag();
    }
}