using System;
using System.Collections.Generic;
using Commons;
using Controllers;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Populates the Missions tab's scrollable list.
    ///
    /// The missions themselves come from <see cref="MissionCatalog"/>, the same asset the
    /// post-win collection panel reads. Progress comes from <see cref="GameSetting.GetMissionProgress"/>,
    /// which counts pieces per dish sprite. A mission's count is the sum over its icon and every
    /// variant icon, so colour and plate versions of one jajanan all fill the same bar. Matches are
    /// banked when a level is won (<c>GameController.CommitMissionProgress</c>), three pieces per match.
    ///
    /// Claiming pays the reward and empties the bar, so a mission can be filled again and again.
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
        }

        /// <summary>Raised after a claim changes what is ready, so the tab badge can recount.</summary>
        public static event Action ProgressChanged;

        [SerializeField] private GameSetting gameSetting;
        [SerializeField] private MissionItemView itemPrefab;
        [SerializeField] private Transform content;

        [Tooltip("The mission list, shared with the collection panel shown after a win.")]
        [SerializeField] private MissionCatalog catalog;

        private IEnumerable<MissionEntry> Missions =>
            catalog != null ? catalog.missions : (IEnumerable<MissionEntry>)Array.Empty<MissionEntry>();

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

            foreach (var entry in Missions)
            {
                var view = Instantiate(itemPrefab, content);
                view.Bind(entry.icon, entry.variantIcons, entry.title, CurrentCount(entry), entry.targetCount,
                    entry.rewardCoins, () => OnClaimClicked(entry));
            }
        }

        /// <summary>
        /// How many missions are full and waiting to be claimed. Reads the save directly, so it
        /// is right even while the Missions page itself is not active.
        /// </summary>
        public int ClaimableCount()
        {
            int ready = 0;
            foreach (var entry in Missions)
            {
                if (CurrentCount(entry) >= entry.targetCount) ready++;
            }
            return ready;
        }

        private int CurrentCount(MissionEntry entry)
        {
            return MissionCatalog.CurrentCount(entry, gameSetting);
        }

        private void OnClaimClicked(MissionEntry entry)
        {
            if (!MissionCatalog.TryClaim(entry, gameSetting)) return;

            if (SoundController.Instance != null) SoundController.Instance.PlayButtonClickClip();

            Refresh();
            RaiseProgressChanged();
        }

        /// <summary>Lets other claim sites (the post-win panel) tell the tab badge to recount.</summary>
        public static void RaiseProgressChanged()
        {
            ProgressChanged?.Invoke();
        }
    }
}
