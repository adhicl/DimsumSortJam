using System;
using Commons;
using TMPro;
using UnityEngine;
using Zenject;

namespace UI
{
    public class HomeScene : MonoBehaviour
    {
        public GameSetting _gameSetting;

        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI lifeText;
        
        #region singleton
        public static HomeScene Instance { get; private set; }

        private void Awake() 
        { 
            // If there is an instance, and it's not me, delete myself.
    
            if (Instance != null && Instance != this) 
            { 
                Destroy(this); 
            } 
            else 
            { 
                Instance = this; 
            } 
        }
        #endregion
        
        public GameSetting GameSetting => _gameSetting;

        private void Start()
        {
            coinText.text = _gameSetting.totalGold.ToString("N0");
        }
    }
}