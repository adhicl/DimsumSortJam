using System;
using UnityEngine;

namespace Commons
{
    [CreateAssetMenu(fileName = "Level", menuName = "LevelData", order = 0)]
    [Serializable]
    public class LevelData : ScriptableObject 
    {
        public int TotalGoal;
        public int TotalVariation;

        /// <summary>
        /// How many of this level's dim sums arrive frozen, and how many sit hidden on the plates.
        /// Counts for the whole level, not per basket - the dealer spreads them at random across
        /// every basket at once, so two runs of the same level ice different pieces.
        ///
        /// Zero means the mechanic simply does not appear, which is what every level below the
        /// introduction point leaves them at. That is also why there is no minimum-level rule in
        /// code any more: the level asset is the only thing that decides.
        ///
        /// Asking for more than the board can hold is not an error - the dealer places as many as
        /// it can and warns. A few dishes have no frozen art and are never picked, and no row is
        /// ever iced solid, so the frozen ceiling is below the raw number of pieces.
        /// </summary>
        public int TotalFrozen;
        public int TotalHidden;

        public DimsumCombination[] currentLevel;
        public DisplayedBasket[] firstDisplayed;
        public int[] currentDropArea;
        
        public RequestCharacter[] requestMissions;
        
    }
}