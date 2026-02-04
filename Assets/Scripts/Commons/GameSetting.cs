using System;
using System.Collections.Generic;
using UnityEngine;

namespace Commons
{
    [Serializable]
    public class GameSetting
    {
        public GameObject dimsumPrefab;
        public GameObject trayPrefab;

        public Sprite[] dimsumSprite;

        public int currentLevel = 0;
        public LevelData currentLevelData;
        public Sprite[] currentDimsumSprites;
        public LevelData[] allLevelData;
    }

    [Serializable]
    public struct DimsumCombination
    {
        public int dimsum1, dimsum2, dimsum3;

        public int[] ToArray()
        {
            return new int[] { dimsum1, dimsum2, dimsum3 };
        }

        public DimsumCombination(int dimsum1, int dimsum2, int dimsum3)
        {
            this.dimsum1 = dimsum1;
            this.dimsum2 = dimsum2;
            this.dimsum3 = dimsum3;
        }
    }

    public enum DisplayedBasket
    {
        Displayed,
        Closed,
        Locked
    };
}