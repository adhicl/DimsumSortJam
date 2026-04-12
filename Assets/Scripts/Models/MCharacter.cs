using System;
using Commons;
using Controllers;
using IClasses;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Models
{
    public class MCharacter : MonoBehaviour, IRequest
    {
        [Inject] SoundController soundController;
        [Inject] GameSetting gameSetting;
        [Inject] GameController gameController;
        
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject requestCanvas;
        [SerializeField] private Slider timerSlider;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private GameObject[] requestItems;
        [SerializeField] private Image[] requestSprites;

        private void Start()
        {
            requestCanvas.SetActive(false);
        }

        public void SetRequest(int[] dimsumTypes)
        {
            foreach (var requestItem in requestItems)
            {
                requestItem.SetActive(false);
            }

            for (int i = 0; i < dimsumTypes.Length; i++)
            {
                requestItems[i].SetActive(true);
                requestSprites[i].sprite = gameSetting.currentDimsumSprites[dimsumTypes[i]];
            }
            
        }

        public void CheckClearRequest()
        {
            throw new NotImplementedException();
        }
    }
}