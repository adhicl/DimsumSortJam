using Commons;
using Controllers;
using DG.Tweening;
using UnityEngine;
using IClasses;
using Zenject;

namespace Models
{
    public class MDimSum : MonoBehaviour, IDragable, IAnimationEffect
    {
        [Inject] private Camera mainCamera;
        [Inject] SoundController soundController;
        public GameSetting gameSetting;
        [Inject] GameController gameController;
        
        [SerializeField] SpriteRenderer _renderer;
        
        public int dimsumType { get; set; }
        
        #region drag

        [Header("Drag feel")]
        [Tooltip("How much bigger the piece gets while it is held, so it reads from under a fingertip. Feedback only — the drop test reads the transform, not the sprite bounds.")]
        [SerializeField] private float dragScale = 1.5f;

        private bool _moved = false;
        private Vector2 _initialPosition;

        // Held so grabbing and dropping can each cancel the other's scale tween. Starting a second
        // DOScale does not replace the first, it just adds one: re-grabbing a piece inside the
        // drop's 0.15s shrink leaves both writing localScale every frame. The later tween happens
        // to win today, so this is insurance rather than a fix for a visible bug, and it keeps
        // tweens from piling up on a fast player. Only the scale tween is tracked, so cancelling
        // it never touches the DOMove that settles a piece into its slot.
        private Tween _scaleTween;

        private void ScaleTo(float target, float duration)
        {
            if (_scaleTween != null && _scaleTween.IsActive()) _scaleTween.Kill();
            _scaleTween = transform.DOScale(Vector3.one * target, duration);
        }

        private void OnMouseDown()
        {
            if (mainCamera == null) return;
            // A popup covering the board does not stop Unity delivering this, and OnStartDrag
            // would call DoStartTimer and resume the level underneath the popup.
            if (gameController != null && !gameController.AcceptsBoardInput) return;
            OnStartDrag();
        }

        private void OnMouseDrag()
        {
            if (mainCamera == null) return;
            if (!_moved) return;
            // A popup opening mid-drag freezes the piece where it is rather than letting the
            // player keep sliding it around over the popup. Releasing still resolves the drag.
            if (gameController != null && !gameController.AcceptsBoardInput) return;
            // The piece tracks the touch point exactly. It was briefly floated above the finger
            // so a fingertip could not cover it, but every offset big enough to help felt like the
            // piece had come unstuck from the thumb, and it moved the collider too — which is what
            // picks the basket and the slot, so the piece landed higher than it looked. Growing it
            // does the same job without separating what the player sees from what they hit.
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            this.transform.position = new Vector3(mousePosition.x, mousePosition.y, -1f);
        }


        private void OnMouseUp()
        {
            if (mainCamera == null) return;
            // Unity still sends this to whatever received OnMouseDown, including a press this
            // script ignored. Ending a drag that never started would play the drop sound and
            // tween the piece to a stale _initialPosition, so only finish a real one.
            if (!_moved) return;
            OnEndDrag();
        }

        public void OnStartDrag()
        {
            soundController.PlayStartDragClip();
            
            gameController.DoStartTimer();

            _renderer.sortingLayerID = SortingLayer.NameToID("Drag");

            _prevDropAt = _dropAt;
            _prevDropAtIndex = _dropAtIndex;
            _dropAt = null;
            _dropAtIndex = -1;
            
            _moved = true;
            ScaleTo(dragScale, 0.12f);

            _initialPosition = this.transform.position;
            this.transform.position = new Vector3(_initialPosition.x, _initialPosition.y, 120f);
        }

        public void OnEndDrag()
        {
            soundController.PlayStopDragClip();
            
            _renderer.sortingLayerID = SortingLayer.NameToID("Game");
            
            _moved = false;
            // Back to board size. The DOMove that settles the piece into its slot runs alongside
            // this one; a position tween and a scale tween touch different properties, so those
            // two really are independent.
            ScaleTo(1f, 0.15f);

            if (_dropAt != null)
            {
                int indexPos = _dropAt.CheckDropPosition(this.transform);
                // Debug.Log("On end drag "+indexPos);
                if (indexPos >= 0)
                {
                    _prevDropAt.RemoveDimsum(_prevDropAtIndex, _dropAt);
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

        public void RemoveFromDropPlace()
        {
            if (_dropAt != null) _dropAt.RemoveDimsum(_dropAtIndex, null);
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
        
        #region pool_zenject

        void Reset(int dimsumType)
        {
            //Debug.Log($"Reset {dimsumType}");
            this.dimsumType = dimsumType;

            // The pool only reactivates the GameObject, so anything the previous life changed is
            // still set. A DOMove left over from the last drag keeps running and walks the piece
            // off the slot it was just placed in, and one despawned during a power-up comes back
            // on the Effect layer at 0.7 scale. Callers set position and start their own tweens
            // after this returns, so killing here is safe.
            transform.DOKill();
            transform.localScale = Vector3.one;
            // A piece despawned mid-drag (a power-up clearing the board) never reaches OnEndDrag,
            // so it would come back out of the pool still believing it was held — drawn a lift
            // above the finger and at drag size.
            _moved = false;
            _scaleTween = null;
            BackToBottom();

            if (gameSetting.currentDimsumSprites.Length > dimsumType)
            {
                _renderer.sprite = gameSetting.currentDimsumSprites[dimsumType];
            }
            else
            {
                // Silently keeping the previous sprite shows the wrong food, and a never-used
                // pool object has none at all and renders nothing. Both are level-data faults
                // worth seeing rather than a piece that quietly goes missing.
                Debug.LogError("[Dimsum] type " + dimsumType + " has no sprite: this level loaded only "
                               + gameSetting.currentDimsumSprites.Length + ".");
            }
        }

        public class Pool : MonoMemoryPool<int, MDimSum>
        {
            protected override void Reinitialize(int dimsumType, MDimSum dimsum)
            {
                dimsum.Reset(dimsumType);
            }
        }

        #endregion
        
        #region animation_effect
        
        public void DrawToTop()
        {
            this._renderer.sortingLayerID = SortingLayer.NameToID("Effect");
            this._renderer.sortingOrder = 21;
        }

        public void BackToBottom()
        {
            this._renderer.sortingLayerID = SortingLayer.NameToID("Game");
            this._renderer.sortingOrder = 10;
        }
        
        #endregion
    }
}