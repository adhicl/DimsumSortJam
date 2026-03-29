using System;
using System.Collections;
using Ricimi;
using UnityEngine;
using UnityEngine.Events;

namespace Controllers
{
    public class TutorialController : MonoBehaviour
    {
        private PopupOpener popupOpener;
        private Popup popup;

        private void Start()
        {
            popupOpener = GetComponent<PopupOpener>();

            StartCoroutine(OpenTutorial());
        }

        IEnumerator OpenTutorial()
        {
            yield return new WaitForSeconds(1f);
            
            popupOpener.OpenPopup();
            popup = popupOpener.GetPopup();
            popup.onClose += onClosePopup;
        }

        public UnityEvent OnPopupClosed;
        private void onClosePopup()
        {
            OnPopupClosed?.Invoke();
        }
    }
}