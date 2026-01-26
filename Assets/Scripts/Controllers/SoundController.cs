using UnityEngine;

namespace Controllers
{
    public class SoundController : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;
        public AudioClip[] startDragClip;
        public AudioClip endDragClip;
        public AudioClip[] successClip;

        public AudioClip buttonClickClip;
        public AudioClip finishSuccessClip;
        public AudioClip finishOverClip;

        #region singleton
        public static SoundController Instance { get; private set; }

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
        
        public void PlayStartDragClip()
        {
            int random = Random.Range(0, startDragClip.Length);
            audioSource.PlayOneShot(startDragClip[random]);
        }

        public void PlayStopDragClip()
        {
            audioSource.PlayOneShot(endDragClip);
        }

        public void PlaySuccessClip()
        {
            audioSource.PlayOneShot(successClip[Random.Range(0, successClip.Length)]);
        }

        public void PlayFinishSuccessClip()
        {
            audioSource.PlayOneShot(finishSuccessClip);
        }

        public void PlayFinishOverClip()
        {
            audioSource.PlayOneShot(finishOverClip);
        }

        public void PlayButtonClickClip()
        {
            audioSource.PlayOneShot(buttonClickClip);
        }
    }
}