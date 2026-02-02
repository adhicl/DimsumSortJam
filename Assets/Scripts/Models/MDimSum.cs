using Commons;
using Controllers;
using DG.Tweening;
using UnityEngine;
using IClasses;
using Zenject;

namespace Models
{
    public class MDimSum : MonoBehaviour, IDragable
    {
        [Inject] private Camera mainCamera;
        [Inject] SoundController soundController;
        [Inject] GameSetting gameSetting;
        [Inject] GameController gameController;
        [Inject] CommonSetting commonSetting;
        
        [SerializeField] SpriteRenderer _renderer;
        
        public int dimsumType { get; set; }
        
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
            soundController.PlayStartDragClip();
            
            gameController.DoStartTimer();

            _renderer.sortingLayerID = SortingLayer.NameToID("Drag");
            _renderer.material = commonSetting.itemDragGameMaterial;

            _prevDropAt = _dropAt;
            _prevDropAtIndex = _dropAtIndex;
            _dropAt = null;
            _dropAtIndex = -1;
            
            _moved = true;
            _initialPosition = this.transform.position;
            this.transform.position = new Vector3(_initialPosition.x, _initialPosition.y, 120f);
        }

        public void OnEndDrag()
        {
            soundController.PlayStopDragClip();
            
            _renderer.sortingLayerID = SortingLayer.NameToID("Game");
            _renderer.material = commonSetting.itemOnGameMaterial;
            
            _moved = false;
            if (_dropAt != null)
            {
                int indexPos = _dropAt.CheckDropPosition(this.transform);
                // Debug.Log("On end drag "+indexPos);
                if (indexPos >= 0)
                {
                    _prevDropAt.RemoveDimsum(_prevDropAtIndex);
                    DoDropPlaceAt(_dropAt, indexPos, true);
                }
                else
                {
                    ResetPreviousDropPlace();
                }
            }
            else
            {
                ResetPreviousDropPlace();
            }
        }

        public void DoDropPlaceAt(IDropable dropable, int index, bool isMove)
        {
            Vector3 dimsumPosition = dropable.GetDimsumPosition(index);
            dropable.AddDimsum(this, index);
            _dropAt = dropable;
            _dropAtIndex = index;
            if (isMove) this.transform.DOMove(dimsumPosition, 0.2f);
            else this.transform.position = dimsumPosition;
        }
        
        public void ResetPreviousDropPlace()
        {
            _dropAtIndex = -1;
            _dropAt = null;
            ReturnToPreviousDrop();
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
            if (_moved && other.GetComponent<IDropable>() != null)
            {
                _dropAt = null;
            }
        }
        
        #endregion

        void Reset(int dimsumType)
        {
            //Debug.Log($"Reset {dimsumType}");
            this.dimsumType = dimsumType;
            _renderer.sprite = gameSetting.dimsumSprite[dimsumType];
            _renderer.material = commonSetting.itemOnGameMaterial;
        }

        public class Pool : MonoMemoryPool<int, MDimSum>
        {
            protected override void Reinitialize(int dimsumType, MDimSum dimsum)
            {
                dimsum.Reset(dimsumType);
            }
        }
    }
}