using UnityEngine;

namespace Commons
{
    public static class Settings
    {
        public const float THRESHOLD_HEIGHT = 0.7f;
        public const float TRAY_HEIGHT = .03f;

        public const float TransitionTime = 0.5f;
        public static readonly Color TransitionColor = new Color(0.7f, 0.8f, .95f);

        public const int minLevelPowerup1 = 2;
        public const int minLevelPowerup2 = 8;
        public const int minLevelPowerup3 = 6;
        public const int minLevelPowerup4 = 12;
        
        public const int minLevelBooster1 = 10;
        public const int minLevelBooster2 = 14;
        public const int minLevelBooster3 = 20;

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
    }
}