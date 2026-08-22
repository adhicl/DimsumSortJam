using Commons;
using DG.Tweening;
using IClasses;
using UnityEngine;
using Zenject;

namespace Models
{
    public class MTray : MonoBehaviour, ITray
    {
        public GameSetting gameSetting;

        [SerializeField] private SpriteRenderer selfRenderer;
        [SerializeField] SpriteRenderer[] _spriteRenderers;
        private int[] _dimsums;

        public class Pool : MonoMemoryPool<MTray>
        {
            protected override void Reinitialize(MTray item)
            {
                item.ResetForSpawn();
            }
        }

        /// <summary>
        /// Puts back everything the tray's last life changed. A tray is faded to nothing on its
        /// way out (see <c>MDropArea.CreateDimsumFromTray</c>) and <see cref="MonoMemoryPool{T}"/>
        /// only deactivates the GameObject — it does not touch the renderer. Without this, a
        /// recycled tray comes back at alpha 0 and the plate is simply invisible, which is why it
        /// looked random: it only bites once the pool starts handing back used trays.
        /// </summary>
        public void ResetForSpawn()
        {
            // A fade still in flight would otherwise empty the tray again after it respawns.
            selfRenderer.DOKill();

            Color colour = selfRenderer.color;
            selfRenderer.color = new Color(colour.r, colour.g, colour.b, 1f);

            CleanDimsums();
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
                    _spriteRenderers[i].sprite = gameSetting.currentDimsumSprites[_dimsums[i]];
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