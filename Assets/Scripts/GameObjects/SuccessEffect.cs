using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameObjects
{
    public class SuccessEffect : MonoBehaviour
    {
        [SerializeField] GameObject[] successEffects;
        
        private void Start()
        {
            int RandomIndex = Random.Range(0, successEffects.Length);
            successEffects[RandomIndex].SetActive(true); 
            StartCoroutine(RemoveMe());
        }

        private IEnumerator RemoveMe()
        {
            yield return new WaitForSeconds(0.5f);
            Destroy(this.gameObject);
        }
    }
}