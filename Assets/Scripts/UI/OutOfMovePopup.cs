using Controllers;
using UnityEngine;
using Zenject;

namespace UI
{
    public class OutOfMovePopup : MonoBehaviour
    {
        public void PlaySoundButton()
        {
            SoundController.Instance.PlayButtonClickClip();            
        }

        public void PlayFinishSoundOverButton()
        {
            SoundController.Instance.PlayFinishOverClip();
        }
    }
}