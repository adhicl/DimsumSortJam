using System;
using UnityEngine;

namespace Commons
{
    [Serializable]
    public class GameSetting
    {
        public GameObject dimsumPrefab;

        public int[][] currentLevel = new int[][]
        {
            new int[] { 0, -1, 0 }, new int[] { 1, -1, -1 }, new int[] { 2, -1, -1 }, new int[] { 3, -1, 0 },
            new int[] { 1, 1, 0 }, new int[] { 2, -1, 4 }, new int[] { 5, 10, -1 }, new int[] { 7, 0, 7 }
        };

        public Sprite[] dimsumSprite;
    }
}