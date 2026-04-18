using UnityEngine;

namespace Controllers
{
    public class SoundController : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;
        public AudioClip[] startDragClip;
        public AudioClip endDragClip;
        public AudioClip[] successClip;

        public AudioClip usePowerUpClip;
        public AudioClip popupOpenClip;
        public AudioClip basketOpenClip;
        public AudioClip buttonClickClip;
        public AudioClip bikeBellClip;
        public AudioClip bikeMoveClip;
        public AudioClip finishSuccessClip;
        public AudioClip finishOverClip;

        public AudioClip[] bubbleSoundClips;
        public AudioClip[] whooshSoundClips;

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

        public void PlayPowerUpClip()
        {
            audioSource.PlayOneShot(usePowerUpClip);
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

        public void PlayOpenPopupClip()
        {
            audioSource.PlayOneShot(popupOpenClip);
        }

        public void PlayButtonClickClip()
        {
            audioSource.PlayOneShot(buttonClickClip);
        }

        public void PlayBubbleSoundClips()
        {
            audioSource.PlayOneShot(bubbleSoundClips[Random.Range(0, bubbleSoundClips.Length)]);
        }

        public void PlayWhooshSoundClips()
        {
            audioSource.PlayOneShot(whooshSoundClips[Random.Range(0, whooshSoundClips.Length)]);
        }

        public void PlayBasketOpenClip()
        {
            audioSource.PlayOneShot(basketOpenClip);
        }

        public void PlayBikeBellSoundClips()
        {
            audioSource.PlayOneShot(bikeBellClip);
        }

        public void PlayBikeMoveSoundClips()
        {
            audioSource.PlayOneShot(bikeMoveClip);
        }
    }
}