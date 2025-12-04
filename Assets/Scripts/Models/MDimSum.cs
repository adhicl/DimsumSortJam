using DG.Tweening;
using UnityEngine;
using IClasses;
using Zenject;

namespace Models
{
    public class MDimSum : MonoBehaviour, IDragable, IPoolable<int>
    {
        #region drag

        private bool _moved = false;
        private Vector2 _initialPosition;

        private void OnMouseDown()
        {
            if (mainCamera == null) return;
            OnStartDrag();
        }

        private void OnMouseDrag()
        {
            if (mainCamera == null) return;
            if (!_moved) return;
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            this.transform.position = new Vector3(mousePosition.x, mousePosition.y, -1f);
        }


        private void OnMouseUp()
        {
            if (mainCamera == null) return;
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
                // Debug.Log("On end drag "+indexPos);
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

        private IDropable _dropAt;
        private int _dropAtIndex;
        
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

        private int dimsumType { get; set; }
        [SerializeField] Sprite[] images;
        [SerializeField] SpriteRenderer _renderer;

        [Inject] private Camera mainCamera;
        //
        // [Inject]
        // public void Construct(int dimsum)
        // {
        //     Reset(dimsum);
        // }

        void Reset(int dimsumType)
        {
            Debug.Log($"Reset {dimsumType}");
            this.dimsumType = dimsumType;
            _renderer.sprite = images[dimsumType];
        }

        public class Pool : MonoMemoryPool<int, MDimSum>
        {
            protected override void Reinitialize(int dimsumType, MDimSum dimsum)
            {
                dimsum.Reset(dimsumType);
            }
        }

        public void OnDespawned()
        {
            throw new System.NotImplementedException();
        }

        public void OnSpawned(int dimsum)
        {
            Reset(dimsum);
        }

        public void OnSpawned()
        {
            throw new System.NotImplementedException();
        }
    }
}