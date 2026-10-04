using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Populates the Missions tab's scrollable list. Progress is a placeholder field on each
    /// entry for now — wiring it up to real jajanan-match counts is a separate, later task.
    /// </summary>
    public class MissionsListController : MonoBehaviour
    {
        [Serializable]
        public class MissionEntry
        {
            public Sprite icon;

            [Tooltip("Other colours of the same jajanan. They count as this one mission; the row " +
                     "cycles through the icon and these.")]
            public Sprite[] variantIcons;

            public string title;
            public int targetCount = 120;
            public int rewardCoins = 200;

            [Tooltip("Placeholder progress for previewing the list. Real gameplay wiring comes later.")]
            public int currentCount;
        }

        [SerializeField] private MissionItemView itemPrefab;
        [SerializeField] private Transform content;
        [SerializeField] private List<MissionEntry> missions = new List<MissionEntry>();

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (content == null || itemPrefab == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }

            foreach (var entry in missions)
            {
                var view = Instantiate(itemPrefab, content);
                view.Bind(entry.icon, entry.variantIcons, entry.title, entry.currentCount, entry.targetCount,
                    entry.rewardCoins, () => OnClaimClicked(entry));
            }
        }

        private void OnClaimClicked(MissionEntry entry)
        {
            if (entry.currentCount < entry.targetCount) return;

            // TODO: credit entry.rewardCoins through GameSetting once mission progress is wired
            // to real gameplay counts.
            entry.currentCount = 0;
            Refresh();
        }
    }
}
