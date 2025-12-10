using System;
using System.Collections;
using Commons;
using IClasses;
using Models;
using UnityEngine;
using Zenject;

namespace GameObjects
{
    public class SpriteCompleteBasket : MonoBehaviour, ICompleteSprite
    {
        private static readonly int Finish = Animator.StringToHash("Finish");
        
        [Inject] GameSetting gameSetting;

        [SerializeField] private GameObject finishObject;
        [SerializeField] SpriteRenderer[] spriteRenderers;
        [SerializeField] Animator animator;

        private void Start()
        {
            finishObject.SetActive(false);
        }

        public void SetDimsumSprites(int dimsumType, Vector3 position)
        {
            this.transform.position = position;
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                spriteRenderers[i].sprite = gameSetting.dimsumSprite[dimsumType];
            }

            StartCoroutine(SequencePlayAnimation());
        }

        private IEnumerator SequencePlayAnimation()
        {
            finishObject.SetActive(true);
            animator.SetTrigger(Finish);
            
            yield return new WaitForSeconds(1f);
            
            finishObject.SetActive(false);
        }
    }
}