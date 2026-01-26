using Controllers;
using UnityEngine;

namespace UI
{
    public class SettingPopup : MonoBehaviour
    {
        public void PlaySoundButton()
        {
            SoundController.Instance.PlayButtonClickClip();            
        }
    }
}