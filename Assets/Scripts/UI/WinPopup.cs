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

            // Double bonus is gated behind a rewarded ad. Only pay out x2 once the
            // reward is earned; if the player declines or no ad is ready, stay on the
            // popup so they can still tap the normal x1 Claim button.
            void GrantDouble()
            {
                SpawnCoins(buttonDoubleClaim, 80);
                StartCoroutine(GoToNextScene());
            }

            if (RewardedAdController.Instance != null)
            {
                RewardedAdController.Instance.ShowAd(GrantDouble);
            }
            else
            {
                // No ad controller in this scene: fall back to granting the bonus so
                // the button is never dead.
                GrantDouble();
            }
        }

        private IEnumerator GoToNextScene()
        {
            yield return new WaitForSeconds(1.5f);

            _gameSetting.currentLevel++;
            if (_gameSetting.currentLevel >= _gameSetting.maximumLevel) _gameSetting.currentLevel = _gameSetting.maximumLevel - 1;
            
            _gameSetting.currentLevelData = _gameSetting.allLevelData[_gameSetting.currentLevel];

            string newScene = "Home";
            if (_gameSetting.currentLevel < 5)
            {
                newScene = Settings.GetNextLevelScene(_gameSetting.currentLevel, "Home");
            }
            
            _gameSetting.SaveData();
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
                GameObject coin = Instantiate(coinPrefab, spawnCenter.position, Quaternion.identity);

                Sequence createSequence = DOTween.Sequence();
                createSequence.Append(coin.transform.DOMove(randomPos, 0.2f));
                createSequence.Append(coin.transform.DOMove(targetCoinUI.position, moveDuration).SetEase(Ease.InOutQuad));
                createSequence.AppendCallback(() =>
                {
                    Destroy(coin); // hapus coin setelah sampai
                    _gameSetting.totalGold++;
                    coinText.text = $"{_gameSetting.totalGold:N0}";
                });
            }
        }
    }
}