using DG.Tweening;
using UnityEngine;
using IClasses;
using UnityEngine.Rendering.Universal;
using Zenject;

namespace Models
{
    public class MDimSum : MonoBehaviour, IDragable
    {
        #region drag
        
        [Inject] Camera mainCamera;

        private bool _moved = false;
        private Vector2 _initialPosition;

        private void OnMouseDown()
        {
            OnStartDrag();
        }

        private void OnMouseDrag()
        {
            if (!_moved) return;
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            this.transform.position = new Vector3(mousePosition.x, mousePosition.y, -1f);
        }


        private void OnMouseUp()
        {
            OnEndDrag();
        }

        public void OnStartDrag()
        {
            //Debug.Log("On start drag");
            if (_dropAt != null)
            {
                _dropAt.RemoveDimsum(_dropAtIndex);
                
                _prevDropAt = _dropAt;
                _prevDropAtIndex = _dropAtIndex;
            }
            
            _dropAt = null;
            _prevDropAtIndex = -1;
            
            _moved = true;
            _initialPosition = this.transform.position;
            this.transform.position = new Vector3(_initialPosition.x, _initialPosition.y, 120f);
        }

        public void OnEndDrag()
        {
            _moved = false;
            if (_dropAt != null)
            {
                int indexPos = _dropAt.CheckDropPosition(this.transform);
                Debug.Log("On end drag "+indexPos);
                if (indexPos >= 0)
                {
                    Vector3 dimsumPosition = _dropAt.GetDimsumPosition(indexPos);
                    _dropAt.AddDimsum(this, indexPos);
                    _dropAtIndex = indexPos;
                    this.transform.DOMove(dimsumPosition, 0.2f);
                }
                else
                {
                    _dropAtIndex = -1;
                    _dropAt = null;
                    ReturnToPreviousDrop();
                }
            }
            else
            {
                _dropAtIndex = -1;
                _dropAt = null;
                ReturnToPreviousDrop();
            }
        }

        private void ReturnToPreviousDrop()
        {
            this.transform.DOMove(new Vector3(_initialPosition.x, _initialPosition.y, 0f), 0.2f);
            if (_prevDropAt != null)
            {
                _dropAtIndex = _prevDropAtIndex;
                _dropAt = _prevDropAt;
            }
            _prevDropAtIndex = -1;
            _prevDropAt = null;
        }

        #endregion
        
        #region drop

        [SerializeField] private IDropable _dropAt;
        [SerializeField] private int _dropAtIndex;
        
        private IDropable _prevDropAt;
        private int _prevDropAtIndex;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_moved && other.GetComponent<IDropable>() != null)
            {
                
                _dropAt = other.GetComponent<IDropable>();
            }
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            if (_moved && other.GetComponent<IDropable>() != null)
            {
                _dropAt = other.GetComponent<IDropable>();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<IDropable>() != null)
            {
                _dropAt = null;
            }
        }
        
        #endregion

    }
}