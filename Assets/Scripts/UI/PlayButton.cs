using Commons;
using Controllers;
using Ricimi;
using TMPro;
using UnityEngine;
using Zenject;

namespace UI
{
    public class PlayButton : MonoBehaviour
    {
        public GameSetting _gameSetting;
        [Inject] SoundController _soundController;

        [SerializeField] private TextMeshProUGUI levelText;

        [Tooltip("Shown instead of starting a level when the player has no lives left.")]
        [SerializeField] private GameObject outOfLivesPopup;

        private void Start()
        {
            levelText.text = $"Level {_gameSetting.currentLevel + 1}";
        }

        public void GoToNextScene()
        {
            SoundController.Instance.PlayButtonClickClip();

            // Bring the life state up to date before deciding: a window that lapsed or a life
            // that regenerated while the app was closed both count. The daily grant covers a
            // player who crossed midnight without ever going through the loading screen.
            _gameSetting.RefreshLives();
            _gameSetting.TryGrantDailyFreeUnlimitedLives();

            if (!_gameSetting.CanStartLevel && ShowOutOfLives()) return;

            string newScene = Settings.GetNextLevelScene(_gameSetting.currentLevel, "Game");
            Transition.LoadLevel(newScene, Settings.TransitionTime, Settings.TransitionColor);
        }

        /// <summary>
        /// Opens the gate popup. Returns false if it could not be shown, in which case the
        /// player is let through — a missing prefab should not lock anyone out of the game.
        /// </summary>
        private bool ShowOutOfLives()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (outOfLivesPopup == null || canvas == null)
            {
                Debug.LogWarning("[PlayButton] Out of lives, but no popup to show; starting anyway.");
                return false;
            }

            var popup = Instantiate(outOfLivesPopup, canvas.transform, false);
            popup.SetActive(true);
            popup.GetComponent<Popup>().Open();
            return true;
        }
    }
}
