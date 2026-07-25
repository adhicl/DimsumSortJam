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
    }
}