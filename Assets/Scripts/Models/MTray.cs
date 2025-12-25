using Commons;
using IClasses;
using UnityEngine;
using Zenject;

namespace Models
{
    public class MTray : MonoBehaviour, ITray
    {
        [Inject] GameSetting gameSetting;

        [SerializeField] private SpriteRenderer selfRenderer;
        [SerializeField] SpriteRenderer[] _spriteRenderers;
        private int[] _dimsums;

        public class Pool : MonoMemoryPool<MTray>
        {
        }

        public void SetRendererOrder(int order)
        {
            selfRenderer.sortingOrder = order;
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                _spriteRenderers[i].sortingOrder = order + 1;
            }
        }

        public void SetDimsums(int[] dimsums)
        {
            _dimsums = dimsums;
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (dimsums[i] >= 0)
                {
                    _spriteRenderers[i].sprite = gameSetting.dimsumSprite[_dimsums[i]];
                }
                else
                {
                    _spriteRenderers[i].sprite = null;
                }
            }
        }

        public void CleanDimsums()
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                _spriteRenderers[i].sprite = null;
            }
        }

        public Transform[] GetDimsumPositions()
        {
            Transform[] positions = new Transform[_spriteRenderers.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = _spriteRenderers[i].transform;
            }
            return positions;
        }
    }
}