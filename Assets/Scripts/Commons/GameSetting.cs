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
        public int maximumLevel = 2;
        
        public LevelData currentLevelData;
        public Sprite[] currentDimsumSprites;
        public LevelData[] allLevelData;
        
        public float lifeTimer;
        public int totalLife;
        public int totalGold;

        public int totalPowerup1;
        public int totalPowerup2;
        public int totalPowerup3;
        public int totalPowerup4;

        public int totalBooster1;
        public int totalBooster2;
        public int totalBooster3;

        public bool soundMute;
        public bool musicMute;
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