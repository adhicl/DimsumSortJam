using System;
using TMPro;
using UnityEngine;
using Zenject;

namespace Controllers
{
    public class GameUIController : MonoBehaviour
    {
        [Inject] GameController gameController;
        
        [SerializeField] private TextMeshProUGUI textTimerDown;

        private void Update()
        {
            textTimerDown.text = ReturnTimeString(gameController.Timer);
        }

        private string ReturnTimeString(float secondTime)
        {
            int minutes = Mathf.FloorToInt(secondTime / 60); 
            int seconds = Mathf.RoundToInt(secondTime % 60);
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}