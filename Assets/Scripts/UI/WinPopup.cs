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
    public class WinPopup : MonoBehaviour, UI.Juice.IOwnsRevealAnimation
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

        [Header("Reward")]
        [Tooltip("Coins paid by Continue. Watching the ad pays twice this.")]
        [SerializeField] private int rewardCoins = 40;

        [Header("Reveal - shown one after another")]
        [Tooltip("Slid in from the screen edges as the popup opens.")]
        [SerializeField] private RectTransform trumpetsLeft;
        [SerializeField] private RectTransform trumpetsRight;

        [Tooltip("Title ribbon with its wings and rainbow. Scaled as one, so its own idle animation keeps running.")]
        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform crown;
        [SerializeField] private RectTransform gift;

        [Tooltip("The coin + amount row under the gift. Counts up from 0.")]
        [SerializeField] private RectTransform rewardRow;
        [SerializeField] private TextMeshProUGUI rewardLabel;

        [Tooltip("Wrappers around the two buttons - animated instead of the buttons themselves, " +
                 "whose Animator and ad gate own their scale and alpha.")]
        [SerializeField] private CanvasGroup doubleHolder;
        [SerializeField] private CanvasGroup continueHolder;

        [SerializeField] private ParticleSystem confetti;

        [Tooltip("Seconds between the double-coins button appearing and Continue appearing.")]
        [SerializeField] private float continueDelay = 1.2f;

        [Header("After the claim")]
        [Tooltip("Shows how far this level moved each jajanan in the collection. Skipped when it moved none.")]
        [SerializeField] private CollectionProgressPopup collectionPopup;

        private readonly RewardOffer _offer = new RewardOffer(GameAnalytics.PlacementDoubleReward);

        private Sequence _reveal;
        private Tween _pulse;

        private void Start()
        {
            _gameSetting = GameController.Instance.GameSetting;
            coinText.text = $"{_gameSetting.totalGold:N0}";
            lifeText.text = $"{Settings.GetTimeFormat(_gameSetting.lifeTimer)}";
            PlayReveal();
        }

        private void OnDestroy()
        {
            _reveal?.Kill();
            _pulse?.Kill();
            _offer.Dismissed();
        }

        /// <summary>
        /// Brings the screen in piece by piece: trumpets, title, crown, gift, the coin count,
        /// then the double-coins offer, and only after a beat the plain Continue. Every piece is
        /// optional, so a scene with an older copy of the popup still works.
        /// </summary>
        private void PlayReveal()
        {
            // Hide everything first, in this frame, so nothing flashes at full size before the
            // sequence gets to it.
            Vector2 leftHome = trumpetsLeft != null ? trumpetsLeft.anchoredPosition : Vector2.zero;
            Vector2 rightHome = trumpetsRight != null ? trumpetsRight.anchoredPosition : Vector2.zero;
            Vector2 crownHome = crown != null ? crown.anchoredPosition : Vector2.zero;

            if (trumpetsLeft != null) trumpetsLeft.anchoredPosition = leftHome + new Vector2(-450f, 0f);
            if (trumpetsRight != null) trumpetsRight.anchoredPosition = rightHome + new Vector2(450f, 0f);
            SetScale(header, 0f);
            SetScale(crown, 0f);
            SetScale(gift, 0f);
            SetScale(rewardRow, 0f);
            if (rewardLabel != null) rewardLabel.text = "0";
            Hide(doubleHolder);
            Hide(continueHolder);
            if (confetti != null) confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            _reveal = DOTween.Sequence().SetLink(gameObject);

            // The popup's own open animation takes a third of a second; start just under it.
            float t = 0.15f;
            if (trumpetsLeft != null)
                _reveal.Insert(t, trumpetsLeft.DOAnchorPos(leftHome, 0.45f).SetEase(Ease.OutBack));
            if (trumpetsRight != null)
                _reveal.Insert(t, trumpetsRight.DOAnchorPos(rightHome, 0.45f).SetEase(Ease.OutBack));

            t += 0.15f;
            if (header != null)
            {
                _reveal.Insert(t, header.DOScale(1f, 0.5f).SetEase(Ease.OutBack, 1.8f));
                _reveal.InsertCallback(t, () =>
                {
                    if (confetti != null) confetti.Play(true);
                });
            }

            t += 0.35f;
            if (crown != null)
            {
                _reveal.InsertCallback(t, () => crown.anchoredPosition = crownHome + new Vector2(0f, 220f));
                _reveal.Insert(t, crown.DOScale(1f, 0.25f).SetEase(Ease.OutQuad));
                _reveal.Insert(t, crown.DOAnchorPos(crownHome, 0.45f).SetEase(Ease.OutBounce));
            }

            t += 0.3f;
            if (gift != null)
            {
                _reveal.Insert(t, gift.DOScale(1f, 0.45f).SetEase(Ease.OutBack, 2.2f));
                _reveal.InsertCallback(t, () => SoundController.Instance.PlayBubbleSoundClips());
            }

            t += 0.4f;
            if (rewardRow != null)
            {
                _reveal.Insert(t, rewardRow.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
                if (rewardLabel != null)
                {
                    int shown = 0;
                    _reveal.Insert(t + 0.1f, DOTween.To(() => shown, x =>
                    {
                        shown = x;
                        rewardLabel.text = x.ToString("N0");
                    }, rewardCoins, 0.6f).SetEase(Ease.OutCubic));
                    _reveal.Insert(t + 0.7f, rewardRow.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.6f));
                }
            }

            t += 1.0f;
            if (doubleHolder != null)
            {
                RectTransform rt = (RectTransform)doubleHolder.transform;
                rt.localScale = Vector3.one * 0.4f;
                _reveal.InsertCallback(t, () =>
                {
                    Show(doubleHolder);
                    SoundController.Instance.PlayOpenPopupClip();
                    _offer.Shown();
                });
                _reveal.Insert(t, rt.DOScale(1f, 0.4f).SetEase(Ease.OutBack, 2f));
                _reveal.InsertCallback(t + 0.45f, StartPulse);
            }

            t += continueDelay;
            if (continueHolder != null)
            {
                _reveal.InsertCallback(t, () =>
                {
                    continueHolder.blocksRaycasts = true;
                    continueHolder.interactable = true;
                });
                _reveal.Insert(t, continueHolder.DOFade(1f, 0.4f));
            }
        }

        // A gentle breathing on the offer, so the eye keeps going back to it.
        private void StartPulse()
        {
            if (doubleHolder == null || _claimed) return;
            _pulse = doubleHolder.transform.DOScale(1.06f, 0.55f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        private static void SetScale(Transform target, float scale)
        {
            if (target != null) target.localScale = Vector3.one * scale;
        }

        private static void Hide(CanvasGroup group)
        {
            if (group == null) return;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static void Show(CanvasGroup group)
        {
            if (group == null) return;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        // Set once a claim starts paying out. Each claim spawns coins and loads the next scene,
        // so a second one landing during the coin flight would pay twice and load twice.
        private bool _claimed;

        public void ButtonClaim()
        {
            if (_claimed) return;
            SoundController.Instance.PlayButtonClickClip();
            _offer.Declined(RewardOffer.ChoiceClaimNormal);
            LockClaimButtons();
            SpawnCoins(rewardRow != null ? rewardRow : buttonClaim, rewardCoins);
            StartCoroutine(GoToNextScene());
        }

        public void ButtonDoubleClaim()
        {
            if (_claimed) return;
            SoundController.Instance.PlayButtonClickClip();
            _offer.Accepted();

            // Double bonus is gated behind a rewarded ad. Only pay out x2 once the
            // reward is earned; if the player declines or no ad is ready, stay on the
            // popup so they can still tap Continue.
            void GrantDouble()
            {
                if (_claimed || this == null) return;
                LockClaimButtons();
                StartCoroutine(PayDouble());
            }

            if (RewardedAdController.Instance != null)
            {
                RewardedAdController.Instance.ShowAd(GrantDouble, placement: GameAnalytics.PlacementDoubleReward);
            }
            else
            {
                // No ad controller in this scene: fall back to granting the bonus so
                // the button is never dead.
                GrantDouble();
            }
        }

        // The count doubles in front of the player before the coins fly, so the reward for
        // watching is something they see happen rather than a number they have to trust.
        private IEnumerator PayDouble()
        {
            int total = rewardCoins * 2;
            if (rewardRow != null && rewardLabel != null)
            {
                _reveal?.Complete();
                int shown = rewardCoins;
                yield return DOTween.To(() => shown, x =>
                    {
                        shown = x;
                        rewardLabel.text = x.ToString("N0");
                    }, total, 0.5f)
                    .SetEase(Ease.OutCubic)
                    .SetLink(gameObject)
                    .WaitForCompletion();
                rewardRow.DOPunchScale(Vector3.one * 0.3f, 0.35f, 6, 0.6f).SetLink(gameObject);
                SoundController.Instance.PlaySuccessClip();
            }

            SpawnCoins(rewardRow != null ? rewardRow : buttonDoubleClaim, total);
            StartCoroutine(GoToNextScene());
        }

        private void LockClaimButtons()
        {
            _claimed = true;
            _pulse?.Kill();
            if (doubleHolder != null) doubleHolder.transform.DOScale(1f, 0.15f).SetLink(gameObject);
            foreach (Transform t in new[] { buttonClaim, buttonDoubleClaim })
            {
                if (t == null) continue;
                var button = t.GetComponent<UnityEngine.UI.Button>();
                if (button != null) button.interactable = false;
            }
            var gate = buttonDoubleClaim != null ? buttonDoubleClaim.GetComponent<AdRewardButton>() : null;
            if (gate != null) gate.Locked = true;
        }

        private IEnumerator GoToNextScene()
        {
            yield return new WaitForSeconds(1.5f);

            // Hand over to the collection panel when the level moved any jajanan bar; it loads
            // the next level itself when the player continues. With nothing to show, go on now.
            var gains = GameController.Instance != null ? GameController.Instance.LevelMissionGains : null;
            if (collectionPopup != null && collectionPopup.HasProgress(gains))
            {
                var panel = Instantiate(collectionPopup, transform.parent, false);
                panel.gameObject.SetActive(true);
                GameSetting setting = _gameSetting;
                panel.Setup(gains, () => LoadNextLevel(setting));
                panel.GetComponent<Popup>().Open();
            }
            else
            {
                LoadNextLevel(_gameSetting);
            }

            GetComponent<Popup>().Close();
        }

        /// <summary>Moves the save on one level and loads its scene - or Home, past the tutorials.</summary>
        public static void LoadNextLevel(GameSetting gameSetting)
        {
            gameSetting.currentLevel++;
            if (gameSetting.currentLevel >= gameSetting.maximumLevel) gameSetting.currentLevel = gameSetting.maximumLevel - 1;

            gameSetting.currentLevelData = gameSetting.allLevelData[gameSetting.currentLevel];

            string newScene = "Home";
            if (gameSetting.currentLevel < 5)
            {
                newScene = Settings.GetNextLevelScene(gameSetting.currentLevel, "Home");
            }

            // Only when the run actually ends on Home. Levels below 5 hand off to another
            // tutorial scene, where there is nothing to raise the prompt on.
            if (newScene == "Home") RateUsPopup.ArmAfterLevelWin();

            gameSetting.SaveData();
            Transition.LoadLevel(newScene, Settings.TransitionTime, Settings.TransitionColor);
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
