using System;
using System.Collections.Generic;
using UnityEngine;

namespace Commons
{
    [CreateAssetMenu(fileName = "Setting", menuName = "GameSetting", order = 0)]
    [Serializable]
    public class GameSetting : ScriptableObject
    {
        //public GameObject dimsumPrefab;
        //public GameObject trayPrefab;

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

        public void SaveData()
        {
            PlayerPrefs.SetInt("currentLevel", currentLevel);
            PlayerPrefs.SetFloat("lifeTimer", lifeTimer);
            PlayerPrefs.SetInt("totalGold", totalGold);
            PlayerPrefs.SetInt("totalLife", totalLife);
            PlayerPrefs.SetInt("totalPowerup1", totalPowerup1);
            PlayerPrefs.SetInt("totalPowerup2", totalPowerup2);
            PlayerPrefs.SetInt("totalPowerup3", totalPowerup3);
            PlayerPrefs.SetInt("totalPowerup4", totalPowerup4);
            PlayerPrefs.SetInt("totalBooster1", totalBooster1);
            PlayerPrefs.SetInt("totalBooster2", totalBooster2);
            PlayerPrefs.SetInt("totalBooster3", totalBooster3);
            PlayerPrefs.SetInt("soundMute", soundMute?1:0);
            PlayerPrefs.SetInt("musicMute", musicMute?1:0);
        }

        public void LoadData()
        {
            currentLevel = PlayerPrefs.GetInt("currentLevel", 0);
            lifeTimer = PlayerPrefs.GetFloat("lifeTimer", 15f);
            totalGold = PlayerPrefs.GetInt("totalGold", 100);
            totalLife = PlayerPrefs.GetInt("totalLife", 99);
            totalPowerup1 = PlayerPrefs.GetInt("totalPowerup1", 3);
            totalPowerup2 = PlayerPrefs.GetInt("totalPowerup2", 3);
            totalPowerup3 = PlayerPrefs.GetInt("totalPowerup3", 3);
            totalPowerup4 = PlayerPrefs.GetInt("totalPowerup4", 3);
            totalBooster1 = PlayerPrefs.GetInt("totalBooster1", 3);
            totalBooster2 = PlayerPrefs.GetInt("totalBooster2", 3);
            totalBooster3 = PlayerPrefs.GetInt("totalBooster3", 3);
            soundMute = PlayerPrefs.GetInt("soundMute", 0) == 1;
            musicMute = PlayerPrefs.GetInt("musicMute", 0) == 1;
        }
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

        public bool isEmpty()
        {
            return dimsum1 == -1 && dimsum2 == -1 && dimsum3 == -1;
        }
    }

    [Serializable]
    public struct RequestCharacter
    {
        public int totalRequestItems;
        public float requestTimeShow;
        public bool getOnTopOnly;
    }

    public enum DisplayedBasket
    {
        Displayed,
        Closed,
        Locked
    };
}