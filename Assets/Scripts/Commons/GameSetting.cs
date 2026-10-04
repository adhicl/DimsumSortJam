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

        [Tooltip("The iced-over version of each dish, indexed in step with dimsumSprite. Leave an " +
                 "entry empty when that dish has no frozen art - it simply never freezes.")]
        public Sprite[] dimsumFrozenSprite;

        [Tooltip("Drawn on a plate in place of a hidden dish, until the basket above empties and " +
                 "the row is dealt for real.")]
        public Sprite hiddenDimsumSprite;

        [Tooltip("Level number (as shown to the player) from which a level's dishes are drawn at " +
                 "random from the whole dimsumSprite list. Below it a level only ever uses the " +
                 "first TotalVariation entries, so those levels keep their hand-picked dishes.")]
        public int randomDishPoolFromLevel = 26;

        public int currentLevel = 0;
        public int maximumLevel = 2;
        
        public LevelData currentLevelData;
        public Sprite[] currentDimsumSprites;

        /// <summary>
        /// Frozen art for this level's dishes, in step with <see cref="currentDimsumSprites"/>.
        /// Both are built from one shuffle of the same indices, so type 4 is the same dish in
        /// each; shuffling the two arrays separately would hand a frozen piece a different food
        /// from the one it thaws into.
        /// </summary>
        public Sprite[] currentFrozenDimsumSprites;

        /// <summary>
        /// The frozen face of <paramref name="dimsumType"/>, or null when that dish has no frozen
        /// art. Null is a real answer, not a failure: the level generator asks this before it
        /// freezes anything, and simply leaves dishes it cannot draw as ice alone.
        /// </summary>
        public Sprite GetFrozenSprite(int dimsumType)
        {
            if (currentFrozenDimsumSprites == null) return null;
            if (dimsumType < 0 || dimsumType >= currentFrozenDimsumSprites.Length) return null;
            return currentFrozenDimsumSprites[dimsumType];
        }

        /// <summary>True when this dish can be dealt frozen - i.e. somebody drew ice for it.</summary>
        public bool CanFreeze(int dimsumType) => GetFrozenSprite(dimsumType) != null;

        public LevelData[] allLevelData;
        
        public float lifeTimer;
        public int totalLife;
        public int totalGold;

        /// <summary>The player's display name. Empty until <see cref="EnsureProfile"/> fills it in.</summary>
        public string playerName = string.Empty;

        /// <summary>
        /// Chosen avatar, stored as the sprite's name rather than an index into
        /// <see cref="AvatarCatalog"/> — reordering or inserting an avatar would silently give
        /// every existing player a different face if this were positional.
        /// </summary>
        public string avatarId = string.Empty;

        /// <summary>Longest name the profile popup will accept.</summary>
        public const int MaxPlayerNameLength = 16;

        /// <summary>
        /// Unix ms at which the current unlimited-lives window ends; 0 when there is none.
        /// Bundles sell time rather than a life count, so this is what the top bar counts down.
        /// Wall-clock based, so it keeps running while the app is closed — and, like the rest of
        /// the save, it trusts the device clock (see next_step.md, "Known gaps").
        /// </summary>
        public long unlimitedLivesUntil;

        /// <summary>
        /// Local date ("yyyy-MM-dd") the free daily unlimited-lives window was last handed out.
        /// Empty on a fresh install. Stored as the date rather than a timestamp so "once a day"
        /// means a calendar day the player recognises, not a rolling 24 hours.
        /// </summary>
        public string lastFreeUnlimitedDay = string.Empty;

        /// <summary>
        /// True once the player has been through the rating flow. The prompt never returns after
        /// this, and it is deliberately not cleared by anything: being asked again after you have
        /// already rated is the part players resent.
        /// </summary>
        public bool hasRatedGame;

        /// <summary>
        /// Unix ms before which the rating prompt must not appear; 0 means "any time now". Set to
        /// a week ahead every time the prompt is shown and declined. Wall-clock, like the life
        /// timers, so the wait runs while the app is closed.
        /// </summary>
        public long nextRatePromptAt;

        /// <summary>How long a declined rating prompt stays away.</summary>
        public const int RatePromptSnoozeDays = 7;

        /// <summary>The level the player has to reach before the prompt is allowed at all.</summary>
        public const int RatePromptMinLevel = 5;

        /// <summary>
        /// Unix ms at which the next regenerated life lands; 0 when the bar is full and no
        /// clock is running. Wall-clock like the unlimited window, so lives keep coming back
        /// while the app is closed.
        /// </summary>
        public long nextLifeAt;

        /// <summary>
        /// Lives never go above this. One is spent per failed level, and the counter is handed
        /// back full whenever an unlimited-lives window runs out.
        /// </summary>
        public const int MaxLife = 5;

        /// <summary>Length of the free window every player gets once a day.</summary>
        public const int FreeUnlimitedMinutesPerDay = 15;

        /// <summary>How long one life takes to come back.</summary>
        public const int LifeRegenMinutes = 30;

        private const long LifeRegenPeriodMs = LifeRegenMinutes * 60000L;

        private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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

        /// <summary>How long the unlimited-lives window still has to run; zero when inactive.</summary>
        public TimeSpan UnlimitedLivesRemaining
        {
            get
            {
                long ms = unlimitedLivesUntil - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return ms > 0L ? TimeSpan.FromMilliseconds(ms) : TimeSpan.Zero;
            }
        }

        /// <summary>True while an unlimited-lives window is running.</summary>
        public bool HasUnlimitedLives => UnlimitedLivesRemaining > TimeSpan.Zero;

        /// <summary>
        /// False once every life is spent and no unlimited window is running — the out-of-lives
        /// gate on the Play button. Call <see cref="SettleUnlimitedLives"/> first so a window
        /// that lapsed while the app was closed hands its lives back before this is read.
        /// </summary>
        public bool CanStartLevel => HasUnlimitedLives || totalLife > 0;

        /// <summary>
        /// How long until the next regenerated life; zero when the bar is already full. This is
        /// the free way back in, so the out-of-lives popup prints it rather than making the
        /// player guess.
        /// </summary>
        public TimeSpan TimeUntilNextLife
        {
            get
            {
                if (totalLife >= MaxLife || nextLifeAt == 0L) return TimeSpan.Zero;

                long ms = nextLifeAt - NowMs();
                return ms > 0L ? TimeSpan.FromMilliseconds(ms) : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// How long until the free daily window can be claimed again; zero when it is available
        /// right now.
        /// </summary>
        public TimeSpan TimeUntilNextFreeUnlimited
        {
            get
            {
                var now = DateTime.Now;
                string today = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (lastFreeUnlimitedDay != today) return TimeSpan.Zero;

                return now.Date.AddDays(1) - now;
            }
        }

        /// <summary>
        /// Gives a player without a profile yet a name and a face, so the top bar and the
        /// profile popup are never blank. The name is seeded from the Unity Authentication
        /// player id — the only identifier available before the player has typed anything —
        /// shortened to something a human would accept as a name rather than the raw 28
        /// characters. Does nothing once either field is set, so it never overwrites a choice.
        /// </summary>
        /// <param name="playerId">UGS player id, or null when sign-in has not finished.</param>
        /// <param name="defaultAvatarId">First entry of the avatar catalog.</param>
        public bool EnsureProfile(string playerId, string defaultAvatarId)
        {
            bool changed = false;

            if (string.IsNullOrEmpty(playerName))
            {
                playerName = DefaultNameFor(playerId);
                changed = true;
            }

            if (string.IsNullOrEmpty(avatarId) && !string.IsNullOrEmpty(defaultAvatarId))
            {
                avatarId = defaultAvatarId;
                changed = true;
            }

            if (changed) SaveData();
            return changed;
        }

        /// <summary>"Player-X2JF" from a UGS id, or a plain "Player" before sign-in lands.</summary>
        public static string DefaultNameFor(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return "Player";

            string suffix = playerId.Substring(0, Math.Min(4, playerId.Length)).ToUpperInvariant();
            return "Player-" + suffix;
        }

        /// <summary>
        /// Trims and caps a name typed into the profile popup, falling back to the current one
        /// (and then to a default) so the player can never end up nameless.
        /// </summary>
        public string SanitiseName(string typed, string playerId)
        {
            string clean = (typed ?? string.Empty).Trim();
            if (clean.Length > MaxPlayerNameLength) clean = clean.Substring(0, MaxPlayerNameLength);

            if (clean.Length > 0) return clean;
            return string.IsNullOrEmpty(playerName) ? DefaultNameFor(playerId) : playerName;
        }

        /// <summary>
        /// Brings the life state up to date with the wall clock: closes a lapsed unlimited
        /// window and credits any lives that regenerated. The single entry point, so no caller
        /// has to remember that there are two clocks. Idempotent and cheap — safe to call every
        /// frame, on every boot, and before every read of <see cref="CanStartLevel"/>.
        /// </summary>
        public void RefreshLives()
        {
            bool changed = SettleUnlimitedLives();
            changed |= RegenerateLives();

            if (changed) SaveData();
        }

        /// <summary>
        /// Closes a window that has run out and hands the player a full set of lives, which is
        /// what "unlimited until 8pm, then back to 5" means. Clearing the deadline is the latch
        /// that makes this idempotent. Returns true only on the call that settled a window.
        /// </summary>
        private bool SettleUnlimitedLives()
        {
            if (unlimitedLivesUntil == 0L) return false;
            if (NowMs() < unlimitedLivesUntil) return false;

            unlimitedLivesUntil = 0L;
            totalLife = MaxLife;
            nextLifeAt = 0L;
            return true;
        }

        /// <summary>
        /// Credits lives the regeneration clock has earned. Returns true when anything changed.
        /// </summary>
        private bool RegenerateLives()
        {
            if (totalLife >= MaxLife)
            {
                // Filled by something else — a purchase, or a window ending. Stop the clock so
                // the next life is a full period away rather than arriving instantly.
                if (nextLifeAt == 0L) return false;
                nextLifeAt = 0L;
                return true;
            }

            long now = NowMs();

            if (nextLifeAt == 0L)
            {
                // Short of full with no clock running: a save written before regeneration
                // existed. Start the period from now rather than back-paying for the gap.
                nextLifeAt = now + LifeRegenPeriodMs;
                return true;
            }

            if (now < nextLifeAt) return false;

            // Pay out every period that elapsed, not just one — closing the app for three
            // hours should return six lives' worth, capped at the bar.
            while (now >= nextLifeAt && totalLife < MaxLife)
            {
                totalLife++;
                nextLifeAt += LifeRegenPeriodMs;
            }

            if (totalLife >= MaxLife) nextLifeAt = 0L;
            return true;
        }

        /// <summary>
        /// Charges the player for a failed attempt. Free while an unlimited-lives window is
        /// running — that is the whole point of the window. Returns false when they had none
        /// left to spend, so the caller can decide what to do about it.
        /// </summary>
        public bool TrySpendLife()
        {
            RefreshLives();

            if (HasUnlimitedLives) return true;
            if (totalLife <= 0) return false;

            // Start the clock on the way down from full. Only then: a second loss must not
            // push the pending life further away, or losing twice would cost more than twice.
            if (totalLife >= MaxLife) nextLifeAt = NowMs() + LifeRegenPeriodMs;

            totalLife--;
            SaveData();
            return true;
        }

        /// <summary>Coins charged to unlock a closed basket.</summary>
        public const int BasketUnlockCost = 450;

        /// <summary>Coins charged to revive with extra time after running out.</summary>
        public const int ReviveCost = 600;

        /// <summary>
        /// Whether the rating prompt may appear right now: the player has come far enough to have
        /// an opinion, has not already rated, and is not inside a snooze. The caller still decides
        /// *when* — this only says it is allowed.
        /// </summary>
        public bool CanShowRatePrompt =>
            !hasRatedGame
            && currentLevel >= RatePromptMinLevel
            && NowMs() >= nextRatePromptAt;

        /// <summary>Pushes the prompt a week out. Called every time it is shown.</summary>
        public void SnoozeRatePrompt()
        {
            nextRatePromptAt = NowMs() + RatePromptSnoozeDays * 24L * 60L * 60L * 1000L;
            SaveData();
        }

        /// <summary>Retires the prompt for good. Called when the player goes through the flow.</summary>
        public void MarkRated()
        {
            hasRatedGame = true;
            SaveData();
        }

        /// <summary>Coins charged for one power-up, whichever of the four it is.</summary>
        public const int PowerupCost = 500;

        /// <summary>How many power-ups one purchase or one rewarded ad hands over.</summary>
        public const int PowerupPurchaseAmount = 1;

        /// <summary>
        /// The four power-up counters addressed by number rather than by name, so the top bar and
        /// the buy popup can loop over them instead of repeating themselves four times. Slots are
        /// 1-based to match <c>totalPowerup1..4</c>. An out-of-range slot reads as zero and spends
        /// nothing rather than throwing: a bad index here comes from a misconfigured Inspector
        /// field, and a popup that quietly offers nothing beats one that breaks the level.
        /// </summary>
        public int GetPowerup(int slot)
        {
            switch (slot)
            {
                case 1: return totalPowerup1;
                case 2: return totalPowerup2;
                case 3: return totalPowerup3;
                case 4: return totalPowerup4;
                default: return 0;
            }
        }

        /// <summary>Credits power-ups to one slot and saves. Used by the buy popup.</summary>
        public void AddPowerup(int slot, int amount)
        {
            if (amount <= 0) return;
            switch (slot)
            {
                case 1: totalPowerup1 += amount; break;
                case 2: totalPowerup2 += amount; break;
                case 3: totalPowerup3 += amount; break;
                case 4: totalPowerup4 += amount; break;
                default: return;
            }
            SaveData();
        }

        /// <summary>
        /// Spends one power-up from a slot, or reports that the player has none. Like
        /// <see cref="TrySpendGold"/> the check and the charge are a single call, so a double tap
        /// cannot spend two.
        /// </summary>
        public bool TrySpendPowerup(int slot)
        {
            if (GetPowerup(slot) <= 0) return false;
            switch (slot)
            {
                case 1: totalPowerup1--; break;
                case 2: totalPowerup2--; break;
                case 3: totalPowerup3--; break;
                case 4: totalPowerup4--; break;
                default: return false;
            }
            SaveData();
            return true;
        }

        /// <summary>
        /// Spends coins if the player can afford it, and changes nothing if they cannot. Callers
        /// gate the UI on the same call that performs the purchase, so a double tap cannot
        /// spend twice.
        /// </summary>
        public bool TrySpendGold(int amount)
        {
            if (amount <= 0) return true;
            if (totalGold < amount) return false;

            totalGold -= amount;
            SaveData();
            return true;
        }

        /// <summary>
        /// Hands out the once-a-day free unlimited-lives window. Call it on boot, after Cloud
        /// Save has resolved — running it before the download lands would grant a second window
        /// to a player who already claimed today's on another device.
        /// </summary>
        public bool TryGrantDailyFreeUnlimitedLives()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (lastFreeUnlimitedDay == today) return false;

            lastFreeUnlimitedDay = today;
            GrantUnlimitedLifeMinutes(FreeUnlimitedMinutesPerDay);
            SaveData();

            Debug.Log($"[GameSetting] Daily free unlimited lives: {FreeUnlimitedMinutesPerDay} min.");
            return true;
        }

        /// <summary>
        /// Extends the unlimited-lives window. Stacks from whichever is later, "now" or the
        /// existing deadline: counting from now would burn the time left on a window still
        /// running, and counting from the deadline would silently credit hours that already
        /// elapsed if the last window lapsed days ago.
        /// </summary>
        private void GrantUnlimitedLifeMinutes(int minutes)
        {
            if (minutes <= 0) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long from = Math.Max(now, unlimitedLivesUntil);
            unlimitedLivesUntil = from + minutes * 60000L;
        }

        /// <summary>
        /// Credits an IAP reward and persists it. Called by IAPController before the purchase
        /// is confirmed with the store.
        /// </summary>
        public void ApplyReward(IAPCatalog.Reward reward)
        {
            totalGold += reward.gold;

            // Lives are capped, so the Life Refill tops up to full rather than stockpiling.
            totalLife = Math.Min(totalLife + reward.lives, MaxLife);

            GrantUnlimitedLifeMinutes(reward.unlimitedLifeHours * 60);

            totalPowerup1 += reward.powerupEach;
            totalPowerup2 += reward.powerupEach;
            totalPowerup3 += reward.powerupEach;
            totalPowerup4 += reward.powerupEach;

            // Boosters are not sold: nothing in the catalog grants them while the feature
            // is unimplemented. The totals below are still saved and restored so existing
            // save data survives.

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

        /// <summary>One dish's collected count for the mission tab.</summary>
        [Serializable]
        public class MissionProgress
        {
            /// <summary>The dish's sprite name. Stable across builds, unlike its index.</summary>
            public string id;
            public int count;
        }

        [Serializable]
        private class MissionProgressList
        {
            public List<MissionProgress> items = new List<MissionProgress>();
        }

        /// <summary>
        /// Pieces collected per dish, kept per sprite rather than per mission. Colour and plate
        /// variants are separate dishes in play but share a mission, and keying by sprite keeps
        /// the grouping in one place (the mission list) instead of baked into the save.
        /// Serialized like the other saved fields (totalGold and the rest), so it survives the
        /// Editor's domain reload when Play is started from a scene other than Splash.
        /// </summary>
        [SerializeField] private List<MissionProgress> missionProgress = new List<MissionProgress>();

        public int GetMissionProgress(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            foreach (var p in missionProgress) if (p.id == id) return p.count;
            return 0;
        }

        public void AddMissionProgress(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return;
            foreach (var p in missionProgress)
            {
                if (p.id != id) continue;
                p.count += amount;
                return;
            }
            missionProgress.Add(new MissionProgress { id = id, count = amount });
        }

        public void ResetMissionProgress(string id)
        {
            missionProgress.RemoveAll(p => p.id == id);
        }

        private static List<MissionProgress> CopyMissionProgress(List<MissionProgress> source)
        {
            var copy = new List<MissionProgress>();
            if (source == null) return copy;
            foreach (var p in source)
            {
                if (p != null && !string.IsNullOrEmpty(p.id) && p.count > 0)
                    copy.Add(new MissionProgress { id = p.id, count = p.count });
            }
            return copy;
        }

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
            PlayerPrefs.SetString("unlimitedLivesUntil",
                unlimitedLivesUntil.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetString("lastFreeUnlimitedDay", lastFreeUnlimitedDay ?? string.Empty);
            PlayerPrefs.SetInt("hasRatedGame", hasRatedGame ? 1 : 0);
            PlayerPrefs.SetString("nextRatePromptAt",
                nextRatePromptAt.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetString("nextLifeAt", nextLifeAt.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetString("playerName", playerName ?? string.Empty);
            PlayerPrefs.SetString("avatarId", avatarId ?? string.Empty);
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
            PlayerPrefs.SetString("missionProgress",
                JsonUtility.ToJson(new MissionProgressList { items = CopyMissionProgress(missionProgress) }));
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
            // Clamped, not just defaulted: saves written before lives were capped hold values
            // far above MaxLife, and an uncapped counter makes the whole life economy a no-op.
            totalLife = Math.Min(PlayerPrefs.GetInt("totalLife", MaxLife), MaxLife);
            unlimitedLivesUntil = long.TryParse(PlayerPrefs.GetString("unlimitedLivesUntil", "0"),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out var until) ? until : 0L;
            lastFreeUnlimitedDay = PlayerPrefs.GetString("lastFreeUnlimitedDay", string.Empty);
            hasRatedGame = PlayerPrefs.GetInt("hasRatedGame", 0) == 1;
            nextRatePromptAt = long.TryParse(PlayerPrefs.GetString("nextRatePromptAt", "0"),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out long ratePromptAt) ? ratePromptAt : 0L;
            nextLifeAt = long.TryParse(PlayerPrefs.GetString("nextLifeAt", "0"),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out var nextLife) ? nextLife : 0L;
            playerName = PlayerPrefs.GetString("playerName", string.Empty);
            avatarId = PlayerPrefs.GetString("avatarId", string.Empty);
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

            var savedMissions = PlayerPrefs.GetString("missionProgress", string.Empty);
            missionProgress = string.IsNullOrEmpty(savedMissions)
                ? new List<MissionProgress>()
                : CopyMissionProgress(JsonUtility.FromJson<MissionProgressList>(savedMissions)?.items);
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
                unlimitedLivesUntil = unlimitedLivesUntil,
                lastFreeUnlimitedDay = lastFreeUnlimitedDay,
                hasRatedGame = hasRatedGame,
                nextRatePromptAt = nextRatePromptAt,
                nextLifeAt = nextLifeAt,
                playerName = playerName,
                avatarId = avatarId,
                totalPowerup1 = totalPowerup1,
                totalPowerup2 = totalPowerup2,
                totalPowerup3 = totalPowerup3,
                totalPowerup4 = totalPowerup4,
                totalBooster1 = totalBooster1,
                totalBooster2 = totalBooster2,
                totalBooster3 = totalBooster3,
                soundMute = soundMute,
                musicMute = musicMute,
                removeAds = removeAds,
                missionProgress = CopyMissionProgress(missionProgress)
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

            // Taken wholesale with the gold, since a claim moves both together - merging the
            // larger count per dish would hand back progress already cashed in. Snapshots from
            // before missions existed carry null, and must not wipe what this device collected.
            if (snapshot.missionProgress != null) missionProgress = CopyMissionProgress(snapshot.missionProgress);

            // Only take a profile the cloud actually has. Snapshots written before profiles
            // existed carry nulls, and applying those would wipe a name the player just chose.
            if (!string.IsNullOrEmpty(snapshot.playerName)) playerName = snapshot.playerName;
            if (!string.IsNullOrEmpty(snapshot.avatarId)) avatarId = snapshot.avatarId;
            // removeAds stays whatever the store said this launch — IAPController re-derives
            // ownership from Google Play, which outranks any cached snapshot.
            removeAds |= snapshot.removeAds;

            // Paid time is never taken away by a sync. The rest of the snapshot is applied
            // wholesale, but a player who bought an unlimited-lives window on another device
            // should keep whichever window runs longer.
            unlimitedLivesUntil = Math.Max(unlimitedLivesUntil, snapshot.unlimitedLivesUntil);

            // The regeneration clock travels with the life count it belongs to — taking the
            // downloaded lives but keeping this device's timer would either hand out a free
            // life or restart a wait the player already sat through.
            totalLife = Math.Min(totalLife, MaxLife);
            nextLifeAt = snapshot.nextLifeAt;

            // Rating on one device settles it for the account, and the longer wait wins so
            // switching devices cannot shake a fresh prompt out of a week already served.
            hasRatedGame |= snapshot.hasRatedGame;
            nextRatePromptAt = Math.Max(nextRatePromptAt, snapshot.nextRatePromptAt);

            // ISO dates sort lexically, so the later claim wins — otherwise hopping devices
            // would hand out a second free window on a day already claimed.
            if (string.CompareOrdinal(snapshot.lastFreeUnlimitedDay ?? string.Empty,
                    lastFreeUnlimitedDay ?? string.Empty) > 0)
            {
                lastFreeUnlimitedDay = snapshot.lastFreeUnlimitedDay;
            }

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
            public long unlimitedLivesUntil;
            public string lastFreeUnlimitedDay;
            public bool hasRatedGame;
            public long nextRatePromptAt;
            public long nextLifeAt;
            public string playerName;
            public string avatarId;
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
            public List<MissionProgress> missionProgress;
        }
    }

    [Serializable]
    public struct DimsumCombination
    {
        public int dimsum1, dimsum2, dimsum3;

        /// <summary>
        /// Which of the three slots are frozen, and which are hidden, one bit per slot.
        ///
        /// They live on the row rather than on the piece because that is what survives the
        /// journey: a row is dealt into a basket, then waits on a plate under it - sometimes for
        /// most of a level - and only becomes real <see cref="Models.MDimSum"/> objects when the
        /// basket above it empties. A flag stored on the piece would not exist yet.
        ///
        /// Masks rather than six bools so an existing <c>LevelData</c> asset keeps deserialising:
        /// a new int field simply reads back 0, which is "nothing special about this row" - and
        /// that is exactly what every level below the introduction threshold wants.
        /// </summary>
        public int frozenMask;
        public int hiddenMask;

        public int[] ToArray()
        {
            return new int[] { dimsum1, dimsum2, dimsum3 };
        }

        public DimsumCombination(int dimsum1, int dimsum2, int dimsum3)
        {
            this.dimsum1 = dimsum1;
            this.dimsum2 = dimsum2;
            this.dimsum3 = dimsum3;
            this.frozenMask = 0;
            this.hiddenMask = 0;
        }

        public bool isEmpty()
        {
            return dimsum1 == -1 && dimsum2 == -1 && dimsum3 == -1;
        }

        public bool IsFrozen(int slot) => (frozenMask & (1 << slot)) != 0;
        public bool IsHidden(int slot) => (hiddenMask & (1 << slot)) != 0;

        public void SetFrozen(int slot, bool frozen)
        {
            if (frozen) frozenMask |= 1 << slot;
            else frozenMask &= ~(1 << slot);
        }

        public void SetHidden(int slot, bool hidden)
        {
            if (hidden) hiddenMask |= 1 << slot;
            else hiddenMask &= ~(1 << slot);
        }

        /// <summary>How many of the three slots hold a dim sum. Empty slots are -1.</summary>
        public int FilledCount()
        {
            int total = 0;
            if (dimsum1 != -1) total++;
            if (dimsum2 != -1) total++;
            if (dimsum3 != -1) total++;
            return total;
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