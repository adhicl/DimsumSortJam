using System;
using System.Collections.Generic;
using System.Globalization;
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

        /// <summary>
        /// Set by the <c>remove_ads</c> (and <c>starter_pack</c>) purchase. Re-applied from the
        /// store on every launch by IAPController, so wiping PlayerPrefs cannot lose it.
        /// </summary>
        public bool removeAds;

        /// <summary>
        /// Credits an IAP reward and persists it. Called by IAPController before the purchase
        /// is confirmed with the store.
        /// </summary>
        public void ApplyReward(IAPCatalog.Reward reward)
        {
            totalGold += reward.gold;
            totalLife += reward.lives;

            totalPowerup1 += reward.powerupEach;
            totalPowerup2 += reward.powerupEach;
            totalPowerup3 += reward.powerupEach;
            totalPowerup4 += reward.powerupEach;

            totalBooster1 += reward.boosterEach;
            totalBooster2 += reward.boosterEach;
            totalBooster3 += reward.boosterEach;

            if (reward.removesAds) removeAds = true;

            SaveData();
        }

        /// <summary>
        /// Raised after every local save. <see cref="Controllers.CloudSaveController"/> listens
        /// so a cloud upload is queued without every save site having to know about it. Static
        /// because this is a ScriptableObject asset — listeners come and go with scene loads
        /// while the asset lives for the whole session.
        /// </summary>
        public static event Action<GameSetting> OnDataSaved;

        /// <summary>Unix ms of the last local save; 0 on a fresh install. Used to order cloud saves.</summary>
        public long SavedAt { get; private set; }

        public void SaveData()
        {
            SavedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PlayerPrefs.SetString("savedAt", SavedAt.ToString(CultureInfo.InvariantCulture));
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
            PlayerPrefs.SetInt("removeAds", removeAds?1:0);
            PlayerPrefs.Save();

            OnDataSaved?.Invoke(this);
        }

        public void LoadData()
        {
            SavedAt = long.TryParse(PlayerPrefs.GetString("savedAt", "0"),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out var savedAt) ? savedAt : 0L;
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
            removeAds = PlayerPrefs.GetInt("removeAds", 0) == 1;
        }

        /// <summary>Copies the persisted fields out for upload to Cloud Save.</summary>
        public SaveSnapshot CreateSnapshot()
        {
            return new SaveSnapshot
            {
                savedAt = SavedAt,
                currentLevel = currentLevel,
                lifeTimer = lifeTimer,
                totalGold = totalGold,
                totalLife = totalLife,
                totalPowerup1 = totalPowerup1,
                totalPowerup2 = totalPowerup2,
                totalPowerup3 = totalPowerup3,
                totalPowerup4 = totalPowerup4,
                totalBooster1 = totalBooster1,
                totalBooster2 = totalBooster2,
                totalBooster3 = totalBooster3,
                soundMute = soundMute,
                musicMute = musicMute,
                removeAds = removeAds
            };
        }

        /// <summary>
        /// Overwrites the in-memory state from a cloud snapshot and writes it straight to
        /// PlayerPrefs, so the download survives a kill before the next gameplay save.
        /// Keeps the snapshot's own <c>savedAt</c> rather than stamping "now" — otherwise a
        /// download would immediately look newer than the record it came from.
        /// </summary>
        public void ApplySnapshot(SaveSnapshot snapshot)
        {
            currentLevel = snapshot.currentLevel;
            lifeTimer = snapshot.lifeTimer;
            totalGold = snapshot.totalGold;
            totalLife = snapshot.totalLife;
            totalPowerup1 = snapshot.totalPowerup1;
            totalPowerup2 = snapshot.totalPowerup2;
            totalPowerup3 = snapshot.totalPowerup3;
            totalPowerup4 = snapshot.totalPowerup4;
            totalBooster1 = snapshot.totalBooster1;
            totalBooster2 = snapshot.totalBooster2;
            totalBooster3 = snapshot.totalBooster3;
            soundMute = snapshot.soundMute;
            musicMute = snapshot.musicMute;
            // removeAds stays whatever the store said this launch — IAPController re-derives
            // ownership from Google Play, which outranks any cached snapshot.
            removeAds |= snapshot.removeAds;

            var restoredAt = SavedAt;
            SaveData();
            SavedAt = snapshot.savedAt;
            PlayerPrefs.SetString("savedAt", SavedAt.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();

            Debug.Log($"[GameSetting] Applied cloud snapshot (local {restoredAt} -> cloud {snapshot.savedAt}).");
        }

        /// <summary>
        /// The saved state as plain data. Serialized to JSON by Cloud Save, so field names are
        /// part of the stored format — renaming one silently drops that value for every
        /// existing player. Add fields, don't rename them.
        /// </summary>
        [Serializable]
        public class SaveSnapshot
        {
            public long savedAt;
            public int currentLevel;
            public float lifeTimer;
            public int totalGold;
            public int totalLife;
            public int totalPowerup1;
            public int totalPowerup2;
            public int totalPowerup3;
            public int totalPowerup4;
            public int totalBooster1;
            public int totalBooster2;
            public int totalBooster3;
            public bool soundMute;
            public bool musicMute;
            public bool removeAds;
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