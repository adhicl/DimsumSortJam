using System;
using System.Collections.Generic;
using Commons;
using Controllers;
using DG.Tweening;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UI
{
    /// <summary>
    /// Shown after the win screen is claimed: one row per jajanan the level just collected, each
    /// bar filling from where it stood before the level to where it is now. A bar that fills up
    /// can be claimed on the spot for its coins, which fly into the coin pill. Continue at the
    /// bottom is live from the start, so a player in a hurry can skip all of it.
    ///
    /// The progress was already saved when the level was won (<c>GameController.CommitMissionProgress</c>),
    /// so this panel only shows it - leaving early loses nothing. Rows come from the same
    /// <see cref="MissionCatalog"/> as the Missions tab on Home.
    /// </summary>
    public class CollectionProgressPopup : MonoBehaviour, UI.Juice.IOwnsRevealAnimation
    {
        [SerializeField] private MissionCatalog catalog;
        [SerializeField] private MissionItemView rowPrefab;

        [Tooltip("Where the rows go. Has a VerticalLayoutGroup.")]
        [SerializeField] private RectTransform listContent;
        [SerializeField] private ScrollRect scrollRect;

        [SerializeField] private Button continueButton;

        [Tooltip("Title ribbon. Popped in on open, like the win screen's.")]
        [SerializeField] private RectTransform header;

        [Header("Coins")]
        [SerializeField] private GameObject coinPrefab;
        [Tooltip("The coin icon in the credits pill - claimed coins fly here.")]
        [SerializeField] private Transform coinTarget;
        [SerializeField] private TextMeshProUGUI coinText;
        [Tooltip("Most coin sprites one claim sends flying; a bigger reward splits across them.")]
        [SerializeField] private int maxFlyingCoins = 25;
        [SerializeField] private float coinSpread = 1.2f;
        [SerializeField] private float coinFlight = 0.8f;

        [Header("Timing")]
        [SerializeField] private float firstRowDelay = 0.35f;
        [SerializeField] private float rowStagger = 0.12f;
        [SerializeField] private float fillDuration = 0.9f;

        private class Row
        {
            public MissionsListController.MissionEntry Entry;
            public MissionItemView View;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Action _onContinue;
        private GameSetting _setting;
        private int _shownGold;
        private bool _leaving;

        /// <summary>True when the level moved at least one mission, i.e. there is something to show.</summary>
        public bool HasProgress(IReadOnlyDictionary<string, int> gains)
        {
            if (catalog == null || gains == null || gains.Count == 0) return false;
            foreach (var entry in catalog.missions)
            {
                if (MissionCatalog.CountIn(entry, gains) > 0) return true;
            }
            return false;
        }

        /// <summary>Builds the rows and plays them in. Call before <see cref="Popup.Open"/>.</summary>
        public void Setup(IReadOnlyDictionary<string, int> gains, Action onContinue)
        {
            _onContinue = onContinue;
            _setting = GameController.Instance != null ? GameController.Instance.GameSetting : null;
            _shownGold = _setting != null ? _setting.totalGold : 0;
            if (coinText != null) coinText.text = _shownGold.ToString("N0");

            if (continueButton != null) continueButton.onClick.AddListener(Continue);

            if (header != null)
            {
                header.localScale = Vector3.zero;
                header.DOScale(1f, 0.5f).SetEase(Ease.OutBack, 1.8f).SetDelay(0.1f).SetLink(gameObject);
            }

            // Live from the first frame - it is the skip - but it still arrives with a little bounce.
            if (continueButton != null)
            {
                Transform t = continueButton.transform.parent != null ? continueButton.transform.parent : continueButton.transform;
                t.localScale = Vector3.one * 0.6f;
                t.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(0.2f).SetLink(gameObject);
            }

            BuildRows(gains);
        }

        private void BuildRows(IReadOnlyDictionary<string, int> gains)
        {
            if (catalog == null || rowPrefab == null || listContent == null) return;

            for (int i = listContent.childCount - 1; i >= 0; i--) Destroy(listContent.GetChild(i).gameObject);

            float delay = firstRowDelay;
            foreach (var entry in catalog.missions)
            {
                int gained = MissionCatalog.CountIn(entry, gains);
                if (gained <= 0) continue;

                int now = MissionCatalog.CurrentCount(entry, _setting);
                int before = Mathf.Max(0, now - gained);

                var view = Instantiate(rowPrefab, listContent);
                var row = new Row { Entry = entry, View = view };
                _rows.Add(row);

                view.Bind(entry.icon, entry.variantIcons, entry.title, before, entry.targetCount,
                    entry.rewardCoins, () => Claim(row));
                view.LockClaim();

                // Pop the row in, then run its bar up. Each row starts a beat after the last, so
                // the eye can follow them down the list.
                var rt = (RectTransform)view.transform;
                rt.localScale = Vector3.zero;
                var seq = DOTween.Sequence().SetLink(view.gameObject);
                seq.AppendInterval(delay);
                seq.Append(rt.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
                seq.AppendCallback(() => view.ShowCount(before, entry.targetCount));
                seq.AppendCallback(view.LockClaim);
                seq.Append(view.AnimateCount(before, now, entry.targetCount, fillDuration));
                seq.AppendCallback(() =>
                {
                    if (now >= entry.targetCount)
                    {
                        rt.DOPunchScale(Vector3.one * 0.08f, 0.35f, 6, 0.6f).SetLink(view.gameObject);
                        if (SoundController.Instance != null) SoundController.Instance.PlaySuccessClip();
                    }
                });

                delay += rowStagger;
            }

            // Rows that can be claimed are the interesting ones; put the first of them in view.
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        }

        private void Claim(Row row)
        {
            if (_leaving) return;
            int reward = row.Entry.rewardCoins;
            if (!MissionCatalog.TryClaim(row.Entry, _setting)) return;

            if (SoundController.Instance != null) SoundController.Instance.PlayButtonClickClip();
            MissionsListController.RaiseProgressChanged();

            row.View.LockClaim();
            RectTransform from = row.View.RewardButton;
            SpawnCoins(from != null ? from : (Transform)row.View.transform, reward);

            // The claim empties the bar; run it back down so the reset is something seen.
            int target = row.Entry.targetCount;
            row.View.AnimateCount(target, 0, target, 0.5f).SetDelay(0.3f);
        }

        private void SpawnCoins(Transform spawnCenter, int reward)
        {
            if (coinPrefab == null || coinTarget == null)
            {
                AddShownGold(reward);
                return;
            }

            int pieces = Mathf.Clamp(reward, 1, maxFlyingCoins);
            int each = reward / pieces;
            int remainder = reward - each * pieces;

            for (int i = 0; i < pieces; i++)
            {
                int value = each + (i < remainder ? 1 : 0);
                Vector3 scatter = spawnCenter.position + Random.insideUnitSphere * coinSpread;
                scatter.z = 0f;

                GameObject coin = Instantiate(coinPrefab, spawnCenter.position, Quaternion.identity);
                var seq = DOTween.Sequence().SetLink(coin);
                seq.Append(coin.transform.DOMove(scatter, 0.2f));
                seq.AppendInterval(i * 0.02f);
                seq.Append(coin.transform.DOMove(coinTarget.position, coinFlight).SetEase(Ease.InOutQuad));
                seq.AppendCallback(() =>
                {
                    Destroy(coin);
                    AddShownGold(value);
                });
            }
        }

        // Only the display: the gold itself was paid and saved by TryClaim at the tap.
        private void AddShownGold(int amount)
        {
            if (this == null) return;
            _shownGold += amount;
            if (coinText != null) coinText.text = _shownGold.ToString("N0");
            if (coinTarget != null)
            {
                coinTarget.DOKill(true);
                coinTarget.DOPunchScale(Vector3.one * 0.15f, 0.15f, 4, 0.5f).SetLink(coinTarget.gameObject);
            }
        }

        private void Continue()
        {
            if (_leaving) return;
            _leaving = true;
            if (SoundController.Instance != null) SoundController.Instance.PlayButtonClickClip();
            if (continueButton != null) continueButton.interactable = false;
            foreach (var row in _rows) row.View.LockClaim();

            Action next = _onContinue;
            _onContinue = null;
            next?.Invoke();

            var popup = GetComponent<Popup>();
            if (popup != null) popup.Close();
        }
    }
}
