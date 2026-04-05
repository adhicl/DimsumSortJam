using System;
using System.Collections;
using Commons;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class LoadingScene : MonoBehaviour
    {
        [Inject] private GameSetting _gameSetting;
        
        [SerializeField] private Slider loadingSlider;
        [SerializeField] private TextMeshProUGUI loadingProgressText;
        [SerializeField] private TextMeshProUGUI loadingText;

        private void Start()
        {
            StartCoroutine(DoLoading());
        }

        private float progress = 0f;
        private void Update()
        {
            progress = Mathf.Clamp(progress + Time.deltaTime, 0f, 1f);
            loadingSlider.value = progress;
            loadingProgressText.text = $"{progress * 100f:N0}%";
            if (progress >= 1f)
            {
            
                string newScene = "Home";
                switch (_gameSetting.currentLevel)
                {
                    case 0: newScene = "Tutorial1";
                        break;
                    case 1: newScene = "Tutorial2"; 
                        break;
                    case 2: newScene = "Game";
                        break;
                    case 3: newScene = "Tutorial3"; 
                        break;
                }    
                Transition.LoadLevel(newScene, 0f, Color.yellowNice); 
            }
        }

        private IEnumerator DoLoading()
        {
            while (true)
            {
                loadingText.text = "Loading";
                yield return new WaitForSeconds(0.2f);
                loadingText.text = "Loading.";
                yield return new WaitForSeconds(0.2f);
                loadingText.text = "Loading..";
                yield return new WaitForSeconds(0.2f);
                loadingText.text = "Loading...";
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
}