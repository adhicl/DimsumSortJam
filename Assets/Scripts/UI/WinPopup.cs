using Commons;
using Controllers;
using Ricimi;
using UnityEngine;
using Zenject;

namespace UI
{
    public class WinPopup : MonoBehaviour
    {
        private GameSetting _gameSetting;
        
        public void GoToNextScene()
        {
            SoundController.Instance.PlayButtonClickClip();

            _gameSetting = GameController.Instance.GameSetting;
            _gameSetting.currentLevel++;
            _gameSetting.currentLevelData = _gameSetting.allLevelData[_gameSetting.currentLevel];
            
            string newScene = "Home";
            switch (_gameSetting.currentLevel)
            {
                case 0: newScene = "Tutorial1";
                    break;
                case 1: newScene = "Tutorial2"; 
                    break;
                case 2: newScene = "Game";
                    break;
                case 3: newScene = "Tutorial3"; 
                    break;
            }    
            Transition.LoadLevel(newScene, Settings.TransitionTime, Settings.TransitionColor);
        }
    }
}