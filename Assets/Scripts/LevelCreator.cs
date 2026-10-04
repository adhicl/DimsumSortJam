using Commons;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// The level builder, used from <c>LevelCreator.unity</c>. Point it at a <see cref="LevelData"/>,
    /// fill in the recipe (or press Load to start from what the level already has), and Create
    /// rewrites the whole level: rows, baskets, rows per basket, customers, and the frozen /
    /// hidden counts.
    ///
    /// Frozen and hidden are counts, not positions - the game picks which pieces carry them each
    /// time the level loads, so a replay never ices the same ones.
    /// </summary>
    public class LevelCreator : MonoBehaviour
    {
        public LevelData _LevelData;

        public LevelRecipe recipe = new LevelRecipe();

        [Tooltip("0 picks a new layout every press. Any other value always builds the same level.")]
        public int seed;

        /// <summary>Copies the target level's current settings into the recipe.</summary>
        public void OnLoadFromLevel()
        {
            if (_LevelData == null) return;

            var l = _LevelData;
            recipe.totalGoal = l.TotalGoal;
            recipe.totalVariation = l.TotalVariation;
            recipe.totalFrozen = l.TotalFrozen;
            recipe.totalHidden = l.TotalHidden;
            recipe.totalBaskets = l.firstDisplayed != null && l.firstDisplayed.Length > 0 ? l.firstDisplayed.Length : 12;
            recipe.lockedBaskets = Count(l.firstDisplayed, DisplayedBasket.Locked);
            recipe.closedBaskets = Count(l.firstDisplayed, DisplayedBasket.Closed);
            recipe.singleRows = 0;
            recipe.doubleRows = 0;
            if (l.currentLevel != null)
            {
                foreach (var row in l.currentLevel)
                {
                    if (row.FilledCount() == 1) recipe.singleRows++;
                    else if (row.FilledCount() == 2) recipe.doubleRows++;
                }
            }
            recipe.requestCount = l.requestMissions != null ? l.requestMissions.Length : 0;
        }

        public void OnTryCreate()
        {
            if (_LevelData == null)
            {
                Debug.LogError("[LevelCreator] No LevelData assigned.");
                return;
            }

            var rng = seed != 0 ? new System.Random(seed) : new System.Random();
            foreach (var note in LevelGenerator.Generate(recipe, _LevelData, rng))
            {
                Debug.LogWarning($"[LevelCreator] {note}");
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(_LevelData);
#endif
            Debug.Log($"[LevelCreator] Built {_LevelData.name}: {_LevelData.TotalGoal} pieces, " +
                      $"{_LevelData.TotalVariation} types, {_LevelData.currentLevel.Length} rows, " +
                      $"{_LevelData.TotalFrozen} frozen, {_LevelData.TotalHidden} hidden, " +
                      $"{_LevelData.requestMissions.Length} customers.");
        }

        private static int Count(DisplayedBasket[] baskets, DisplayedBasket state)
        {
            int n = 0;
            if (baskets == null) return n;
            foreach (var b in baskets) if (b == state) n++;
            return n;
        }
    }
}
