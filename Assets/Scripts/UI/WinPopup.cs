using System;
using System.Collections;
using Commons;
using Controllers;
using DG.Tweening;
using Ricimi;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UI
{
    public class WinPopup : MonoBehaviour
    {
        private GameSetting _gameSetting;

        public GameObject coinPrefab;
        [SerializeField] TextMeshProUGUI coinText;
        [SerializeField] TextMeshProUGUI lifeText;
        [SerializeField] Transform buttonDoubleClaim;
        [SerializeField] Transform buttonClaim;
        [SerializeField] Transform targetCoinUI;      // lokasi UI total coin (misalnya Image di Canvas)
        
        public float spawnRadius = 2f;
        public float moveDuration = 0.8f;

        private void Start()
        {
            _gameSetting = GameController.Instance.GameSetting;
            coinText.text = $"{_gameSetting.totalGold:N0}";
            lifeText.text = $"{Settings.GetTimeFormat(_gameSetting.lifeTimer)}";
        }

        public void ButtonClaim()
        {
            SoundController.Instance.PlayButtonClickClip();
            SpawnCoins(buttonClaim, 40);
            StartCoroutine(GoToNextScene());
        }

        public void ButtonDoubleClaim()
        {
            SoundController.Instance.PlayButtonClickClip();
            SpawnCoins(buttonDoubleClaim, 80);
            StartCoroutine(GoToNextScene());
        }

        private IEnumerator GoToNextScene()
        {
            yield return new WaitForSeconds(1.5f);

            _gameSetting.currentLevel++;
            if (_gameSetting.currentLevel >= _gameSetting.maximumLevel) _gameSetting.currentLevel = _gameSetting.maximumLevel - 1;
            
            _gameSetting.currentLevelData = _gameSetting.allLevelData[_gameSetting.currentLevel];
            
            string newScene = Settings.GetNextLevelScene(_gameSetting.currentLevel, "Home");
            Transition.LoadLevel(newScene, Settings.TransitionTime, Settings.TransitionColor);
            
            GetComponent<Popup>().Close();
        }

        private void SpawnCoins(Transform spawnCenter, int coinCount)
        {
            for (int i = 0; i < coinCount; i++)
            {
                // Tentukan posisi random di sekitar spawnCenter
                Vector3 randomPos = spawnCenter.position + Random.insideUnitSphere * spawnRadius;
                randomPos.z = 0; // kalau 2D, pastikan z = 0

                // Buat coin
                GameObject coin = Instantiate(coinPrefab, randomPos, Quaternion.identity);

                // Animasi ke target UI
                coin.transform.DOMove(targetCoinUI.position, moveDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() =>
                    {
                        Destroy(coin); // hapus coin setelah sampai
                        _gameSetting.totalGold++;
                        coinText.text = $"{_gameSetting.totalGold:N0}";
                    });
            }
        }
    }
}