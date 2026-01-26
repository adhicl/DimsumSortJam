using Controllers;
using UnityEngine;

namespace UI
{
    public class PlayTopBar : MonoBehaviour
    {
        public void PlayButtonSound()
        {
            SoundController.Instance.PlayButtonClickClip();
        }
    }
}