using UnityEngine;

namespace Controllers
{
    public class BGMController : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;

        public void PlayMusic()
        {
            audioSource.Play();
        }

        public void StopMusic()
        {
            audioSource.Stop();
        }

    }
}