using Controllers;
using UnityEngine;

namespace UI
{
    public class LoseQuitPopup : MonoBehaviour
    {
        public void PlaySoundButton()
        {
            SoundController.Instance.PlayButtonClickClip();            
        }
    }
}