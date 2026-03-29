using UnityEngine;

namespace Commons
{
    public static class Settings
    {
        public const float THRESHOLD_HEIGHT = 0.7f;
        public const float TRAY_HEIGHT = .03f;

        //
        public const float TransitionTime = 0.5f;
        public static readonly Color TransitionColor = new Color(0.9f, 0.8f, .4f);

        public enum GAME_STATUS
        {
            pause,
            play,
            win,
            lose,
        }
    }
}