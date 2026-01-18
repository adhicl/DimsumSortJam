using UnityEngine;

namespace Controllers
{
    public class SoundController : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;
        public AudioClip[] startDragClip;
        public AudioClip endDragClip;
        public AudioClip[] successClip;

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
    }
}