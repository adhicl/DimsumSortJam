using System;
using System.Collections;
using Commons;
using Controllers;
using DG.Tweening;
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
        public GameSetting gameSetting;
        [Inject] GameController gameController;
        
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject requestCanvas;
        [SerializeField] private Slider timerSlider;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private GameObject[] requestItems;
        [SerializeField] private Image[] requestSprites;

        private float timerStay;
        private int[] _dimsumTypes;

        #region pool_zenject
        public class Pool : MonoMemoryPool<MCharacter>
        {
        }
        #endregion

        private void Start()
        {
            requestCanvas.SetActive(false);
        }

        public void SetRequest(MDimSum[] dimsumTypes, Vector2 showAtPosition)
        {
            foreach (var requestItem in requestItems)
            {
                requestItem.SetActive(false);
            }

            _dimsumTypes = new int[dimsumTypes.Length];
            for (int i = 0; i < dimsumTypes.Length; i++)
            {
                requestItems[i].SetActive(true);
                requestSprites[i].sprite = gameSetting.currentDimsumSprites[dimsumTypes[i].dimsumType];
                _dimsumTypes[i] = dimsumTypes[i].dimsumType;
            }
            
            //this.transform.DOMoveX(showAtPosition.x, .5f).SetEase(Ease.OutBack).OnComplete(ShowOrder);

            timerStay = Settings.TIME_CHARACTER_STAY;
            isShowing = true;
            
            timerSlider.value = timerStay / Settings.TIME_CHARACTER_STAY;
            timerText.text = $"{TimeString(timerStay)}";
        }

        public void SetMoveTo(Vector2 position)
        {
            //Debug.Log($"{this.name} move to  {position}");
            this.transform.DOMoveX(position.x, .5f).SetEase(Ease.OutBack).OnComplete(ShowOrder);
        }
        
        private void ShowOrder()
        {
            requestCanvas.SetActive(true);
        } 

        public void CheckClearRequest(int dimsumType)
        {
            for (int i = 0; i < _dimsumTypes.Length; i++)
            {
                if (_dimsumTypes[i] == dimsumType)
                {
                    requestItems[i].SetActive(false);
                    _dimsumTypes[i] = -1;
                }
            }

            int getDimsumClear = 0;
            for (int i = 0; i < _dimsumTypes.Length; i++)
            {
                if (_dimsumTypes[i] == -1) getDimsumClear++;
            }

            if (getDimsumClear == _dimsumTypes.Length)
            {
                SetAsFinish();
            }
        }

        public void SetAsFinish()
        {
            requestCanvas.SetActive(false);
            this.transform.DOMoveX(Settings.END_POSITION_CHAR.x, 1f).SetEase(Ease.InBack).OnComplete(DeleteMe);
            isShowing = false;
            gameController.AddSuccessVFX(this.transform.position + new Vector3(0f, 1f, 0f));
            SoundController.Instance.PlayBikeMoveSoundClips();
        }

        private void DeleteMe()
        {
            gameController.RemoveCharacter(this);
        }

        private bool isShowing = false;

        private void Update()
        {
            if (gameController.gameStatus != Settings.GAME_STATUS.play) return;
            if (!isShowing) return;

            if (!gameController.isTimerPause)
            {
                timerStay -= Time.deltaTime;
            }
            
            if (timerStay <= 0)
            {
                timerStay = 0f;
                gameController.CallGameLose();
            }
            timerSlider.value = timerStay / Settings.TIME_CHARACTER_STAY;
            timerText.text = $"{TimeString(timerStay)}";
        }

        private string TimeString(float timeInSeconds)
        {
            System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(timeInSeconds);
            string formattedTime = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
            return formattedTime;
        }
    }
}