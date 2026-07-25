using System;
using System.Collections;
using Commons;
using DG.Tweening;
using IClasses;
using Models;
using UnityEngine;
using Zenject;

namespace GameObjects
{
    public class SpriteCompleteBasket : MonoBehaviour, ICompleteSprite
    {
        private static readonly int Finish = Animator.StringToHash("Finish");
        
        public GameSetting gameSetting;

        [SerializeField] private Transform dropFinish;
        [SerializeField] private GameObject finishObject;
        [SerializeField] SpriteRenderer[] spriteRenderers;
        [SerializeField] Animator animator;

        private void Start()
        {
            //finishObject.SetActive(false);
        }

        public void SetDimsumSprites(int dimsumType, Vector3 position)
        {
            this.transform.position = position;
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                spriteRenderers[i].sprite = gameSetting.dimsumSprite[dimsumType];
            }

            animator.SetTrigger(Finish);
            //StartCoroutine(SequencePlayAnimation());
        }

        private IEnumerator SequencePlayAnimation()
        {
            //finishObject.SetActive(true);
            animator.SetTrigger(Finish);

            yield return new WaitForSeconds(0.5f);

            //this.transform.DOMove(dropFinish.transform.position, 0.5f);
            
            finishObject.SetActive(false);
        }
    }
}