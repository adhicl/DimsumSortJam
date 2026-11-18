using System.Collections.Generic;
using Commons;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// The jajanan-collection missions, as one asset shared by the Missions tab on Home
    /// (<see cref="MissionsListController"/>) and the collection panel after a win
    /// (<see cref="CollectionProgressPopup"/>), so the two can never disagree on targets or rewards.
    ///
    /// Progress itself lives in the save (<see cref="GameSetting.GetMissionProgress"/>), counted per
    /// dish sprite; a mission's count is the sum over its icon and every variant icon.
    /// </summary>
    [CreateAssetMenu(menuName = "Jajanan/Mission Catalog", fileName = "MissionCatalog")]
    public class MissionCatalog : ScriptableObject
    {
        public List<MissionsListController.MissionEntry> missions = new List<MissionsListController.MissionEntry>();

        /// <summary>The icon and every distinct variant icon - each one a counted dish.</summary>
        public static IEnumerable<Sprite> Sprites(MissionsListController.MissionEntry entry)
        {
            if (entry.icon != null) yield return entry.icon;
            if (entry.variantIcons == null) yield break;
            foreach (var variant in entry.variantIcons)
            {
                if (variant != null && variant != entry.icon) yield return variant;
            }
        }

        public static int CurrentCount(MissionsListController.MissionEntry entry, GameSetting setting)
        {
            if (setting == null) return 0;
            int total = 0;
            foreach (var sprite in Sprites(entry)) total += setting.GetMissionProgress(sprite.name);
            return total;
        }

        /// <summary>How much of a pieces-per-sprite tally belongs to this mission.</summary>
        public static int CountIn(MissionsListController.MissionEntry entry, IReadOnlyDictionary<string, int> perSprite)
        {
            if (perSprite == null) return 0;
            int total = 0;
            foreach (var sprite in Sprites(entry))
            {
                if (perSprite.TryGetValue(sprite.name, out int count)) total += count;
            }
            return total;
        }

        /// <summary>
        /// Pays the reward, empties the bar and saves, all at once - split up, a kill between them
        /// could pay twice or wipe the bar without paying. False if the mission is not full.
        /// </summary>
        public static bool TryClaim(MissionsListController.MissionEntry entry, GameSetting setting)
        {
            if (setting == null || CurrentCount(entry, setting) < entry.targetCount) return false;

            setting.totalGold += entry.rewardCoins;
            foreach (var sprite in Sprites(entry)) setting.ResetMissionProgress(sprite.name);
            setting.SaveData();
            return true;
        }
    }
}
