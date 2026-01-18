using System;
using UnityEngine;

namespace Commons
{
    [CreateAssetMenu(fileName = "Level", menuName = "LevelData", order = 0)]
    [Serializable]
    public class LevelData : ScriptableObject 
    {
        public int TotalGoal;
        public DimsumCombination[] currentLevel;
        public DisplayedBasket[] firstDisplayed;
        public int[] currentDropArea;
        
    }
}