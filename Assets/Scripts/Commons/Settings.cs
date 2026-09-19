using System;
using UnityEngine;

namespace Commons
{
    public static class Settings
    {
        public const float THRESHOLD_HEIGHT = 0.7f;
        public const float TRAY_HEIGHT = .03f;

        public const float TransitionTime = 0.5f;
        public static readonly Color TransitionColor = new Color(0.7f, 0.8f, .95f);

        public const int minLevelPowerup1 = 7;
        public const int minLevelPowerup2 = 2;
        public const int minLevelPowerup3 = 14;
        public const int minLevelPowerup4 = 10;
        
        public const int minLevelBooster1 = 10;
        public const int minLevelBooster2 = 14;
        public const int minLevelBooster3 = 20;

        #region frozen_and_hidden

        // How many frozen and hidden dim sums a level carries is the level asset's business -
        // LevelData.TotalFrozen and TotalHidden. There is deliberately no minimum-level constant
        // here: a rule in code would quietly override whatever a level asset asked for, and
        // leaving the counts at zero already says "not in this level" perfectly well.

        /// <summary>
        /// Most slots in one row that may be frozen. Capped below three so a row can never arrive
        /// completely iced over: a basket holding nothing but frozen pieces cannot be emptied at
        /// all, and if the board runs out of matches while one is sitting there the only way out
        /// is the stuck-board rescue. Leaving one piece movable keeps that a rare accident rather
        /// than something the dealer does on purpose.
        ///
        /// This caps the shape of a row, not the total - a level asking for more frozen pieces
        /// than the rows can carry gets as many as fit, and a warning.
        /// </summary>
        public const int MAX_FROZEN_PER_ROW = 2;

        #endregion

        public const float TIME_CHARACTER_STAY = 120f;
        public static Vector2 START_POSITION_CHAR = new Vector2(-4f, 2.7f);
        public static Vector2 END_POSITION_CHAR = new Vector2(6f, 2.7f);

        public enum GAME_STATUS
        {
            pause,
            play,
            win,
            lose,
        }

        public static string GetNextLevelScene(int currentLevel, string defaultScene)
        {   
            string newScene = defaultScene;
            switch (currentLevel)
            {
                case 0: newScene = "Tutorial1";
                    break;
                case 1: newScene = "Tutorial2"; 
                    break;
                case 2: newScene = "Game";
                    break;
                case 3: newScene = "Tutorial3"; 
                    break;
                case 4: newScene = "Tutorial4"; 
                    break;
                case 6: newScene = "Tutorial7"; 
                    break;
                case 9: newScene = "Tutorial5";
                    break;
                case 13: newScene = "Tutorial6";
                    break;
                default: newScene = defaultScene;
                    break;
            }

            return newScene;
        }

        public static string GetTimeFormat(float timeLeft)
        {
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);

            string formattedTime = $"{minutes:00}:{seconds:00}";
            return formattedTime;
        }

        /// <summary>
        /// The unlimited-lives countdown, always two segments so it fits the top bar pill.
        /// An hour or more shows hours:minutes ("7:59"); under an hour switches to
        /// minutes:seconds, so the free daily window reads "15:00" and ticks down visibly.
        /// </summary>
        public static string GetCountdownFormat(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "0:00";

            if (remaining.TotalHours >= 1d)
                return $"{(int)remaining.TotalHours}:{remaining.Minutes:00}";

            return $"{remaining.Minutes}:{remaining.Seconds:00}";
        }

        /// <summary>
        /// A wait written out for prose — "7h 12m", "42m" — where the bare "7:12" of
        /// <see cref="GetCountdownFormat"/> could be read as minutes and seconds.
        /// </summary>
        public static string GetWaitFormat(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "now";
            if (remaining.TotalMinutes < 1d) return "under a minute";

            // Rounded up, not truncated: a 30-minute wait that reads "29m" the instant it
            // starts looks like the game short-changed the player.
            int totalMinutes = (int)Math.Ceiling(remaining.TotalMinutes);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
        }

        /// <summary>
        /// The shop's short form for a purchasable unlimited-lives window: "2h", "1d".
        /// </summary>
        public static string GetLifeWindowLabel(int hours)
        {
            if (hours >= 24 && hours % 24 == 0) return (hours / 24) + "d";
            return hours + "h";
        }
    }
}