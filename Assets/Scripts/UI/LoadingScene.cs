using System;
using System.Collections;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    public class LoadingScene : MonoBehaviour
    {
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
                Transition.LoadLevel("Tutorial1", 0f, Color.yellowNice); 
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