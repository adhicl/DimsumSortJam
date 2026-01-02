using Ricimi;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class AlertQuitPopup : MonoBehaviour
    {
        public string scene = "Home";
        public float duration = 1.0f;
        public Color color = Color.black;
        
        public void QuitPopup()
        {
            //added function later
            //reduce one health
            
            Transition.LoadLevel(scene, duration, color);
        }
    }
}