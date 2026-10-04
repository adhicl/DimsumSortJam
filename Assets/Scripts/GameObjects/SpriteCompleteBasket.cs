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
            // dimsumType indexes this level's shuffled dishes, not the master list.
            Sprite[] faces = gameSetting.currentDimsumSprites;
            Sprite face = faces != null && dimsumType >= 0 && dimsumType < faces.Length
                ? faces[dimsumType]
                : null;
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                spriteRenderers[i].sprite = face;
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