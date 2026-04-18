using Models;
using UnityEngine;

namespace IClasses
{
    public interface IRequest
    {
        public void SetRequest(MDimSum[] dimsumTypes, Vector2 showAtPosition);
        public void CheckClearRequest(int dimsumType);
        public void SetAsFinish();
    }
}