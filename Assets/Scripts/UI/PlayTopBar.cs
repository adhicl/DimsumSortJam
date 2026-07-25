using System;
using Commons;
using Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class PlayTopBar : MonoBehaviour
    {
        public GameSetting gameSetting;

        [SerializeField] private Button powerUpBtn1;
        [SerializeField] private Button powerUpBtn2;
        [SerializeField] private Button powerUpBtn3;
        [SerializeField] private Button powerUpBtn4;
        
        [SerializeField] private GameObject powerupActive1;
        [SerializeField] private GameObject powerupActive2;
        [SerializeField] private GameObject powerupActive3;
        [SerializeField] private GameObject powerupActive4;
        
        [SerializeField] private GameObject powerupInactive1;
        [SerializeField] private GameObject powerupInactive2;
        [SerializeField] private GameObject powerupInactive3;
        [SerializeField] private GameObject powerupInactive4;
        
        [SerializeField] private TextMeshProUGUI totalPowerup1Text;
        [SerializeField] private TextMeshProUGUI totalPowerup2Text;
        [SerializeField] private TextMeshProUGUI totalPowerup3Text;
        [SerializeField] private TextMeshProUGUI totalPowerup4Text;
        
        [SerializeField] private TextMeshProUGUI levelPowerup1Text;
        [SerializeField] private TextMeshProUGUI levelPowerup2Text;
        [SerializeField] private TextMeshProUGUI levelPowerup3Text;
        [SerializeField] private TextMeshProUGUI levelPowerup4Text;
        
        [SerializeField] private Button addPowerup1Btn;
        [SerializeField] private Button addPowerup2Btn;
        [SerializeField] private Button addPowerup3Btn;
        [SerializeField] private Button addPowerup4Btn;
        
        public void PlayButtonSound()
        {
            SoundController.Instance.PlayButtonClickClip();
        }

        private void Start()
        {
            powerupActive1.SetActive(gameSetting.currentLevel >= Settings.minLevelPowerup1);
            powerupInactive1.SetActive(gameSetting.currentLevel < Settings.minLevelPowerup1);
            powerUpBtn1.interactable = gameSetting.currentLevel >= Settings.minLevelPowerup1;
            levelPowerup1Text.text = $"Lv. {Settings.minLevelPowerup1}";
            
            powerupActive2.SetActive(gameSetting.currentLevel >= Settings.minLevelPowerup2);
            powerupInactive2.SetActive(gameSetting.currentLevel < Settings.minLevelPowerup2);
            powerUpBtn2.interactable = gameSetting.currentLevel >= Settings.minLevelPowerup2;
            levelPowerup2Text.text = $"Lv. {Settings.minLevelPowerup2}";
            
            powerupActive3.SetActive(gameSetting.currentLevel >= Settings.minLevelPowerup3);
            powerupInactive3.SetActive(gameSetting.currentLevel < Settings.minLevelPowerup3);
            powerUpBtn3.interactable = gameSetting.currentLevel >= Settings.minLevelPowerup3;
            levelPowerup3Text.text = $"Lv. {Settings.minLevelPowerup3}";
            
            powerupActive4.SetActive(gameSetting.currentLevel >= Settings.minLevelPowerup4);
            powerupInactive4.SetActive(gameSetting.currentLevel < Settings.minLevelPowerup4);
            powerUpBtn4.interactable = gameSetting.currentLevel >= Settings.minLevelPowerup4;
            levelPowerup4Text.text = $"Lv. {Settings.minLevelPowerup4}";

            if (gameSetting.currentLevel != 0) SetUpPowerUpButtons();
        }

        public void ShowTutorialPowerUp()
        {
            if (gameSetting.currentLevel == Settings.minLevelPowerup1 - 1)
            {
                powerupActive1.SetActive(true);
                powerupInactive1.SetActive(false);
            }
            else if (gameSetting.currentLevel == Settings.minLevelPowerup2 - 1)
            {
                powerupActive2.SetActive(true);
                powerupInactive2.SetActive(false);
            }
            else if (gameSetting.currentLevel == Settings.minLevelPowerup3 - 1)
            {
                powerupActive3.SetActive(true);
                powerupInactive3.SetActive(false);
            }
            else if (gameSetting.currentLevel == Settings.minLevelPowerup4 - 1)
            {
                powerupActive4.SetActive(true);
                powerupInactive4.SetActive(false);
            }

            SetUpPowerUpButtons();
        }

        private void SetUpPowerUpButtons()
        {
            totalPowerup1Text.text = $"{gameSetting.totalPowerup1:N0}";
            totalPowerup2Text.text = $"{gameSetting.totalPowerup2:N0}";
            totalPowerup3Text.text = $"{gameSetting.totalPowerup3:N0}";
            totalPowerup4Text.text = $"{gameSetting.totalPowerup4:N0}";
            
            addPowerup1Btn.gameObject.SetActive(gameSetting.totalPowerup1 <= 0);
            addPowerup2Btn.gameObject.SetActive(gameSetting.totalPowerup2 <= 0);
            addPowerup3Btn.gameObject.SetActive(gameSetting.totalPowerup3 <= 0);
            addPowerup4Btn.gameObject.SetActive(gameSetting.totalPowerup4 <= 0);
        }
    }
}