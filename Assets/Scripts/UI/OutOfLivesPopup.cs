using System;
using Commons;
using Controllers;
using Ricimi;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// The gate shown when the player taps Play with no lives and no unlimited-lives window.
    ///
    /// It names both ways back in — wait for the next life, or buy a bundle — and prints the
    /// actual wait rather than making the player guess. A dead end with no stated way out is
    /// what makes a lives system feel unfair.
    /// </summary>
    public class OutOfLivesPopup : MonoBehaviour
    {
        [SerializeField] private GameSetting gameSetting;

        [Tooltip("Body copy. Rewritten on open with the time until the next free window.")]
        [SerializeField] private TextMeshProUGUI bodyText;

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (bodyText == null || gameSetting == null) return;

            TimeSpan wait = gameSetting.TimeUntilNextLife;

            // Zero would mean a life is already due, which the gate refreshes for before it
            // opens this — but say something sane rather than "next life in now".
            bodyText.text = wait <= TimeSpan.Zero
                ? "You're out of lives!\nA new life is on its way — or get a bundle to play unlimited right now."
                : $"You're out of lives!\nNext life in {Settings.GetWaitFormat(wait)}, or get a bundle to play unlimited right now.";
        }

        /// <summary>Wired to the popup's main button.</summary>
        public void GoToShop()
        {
            PlaySoundButton();

            // Close first: the shop is a page of the scene behind this popup, so leaving the
            // popup up would slide the shop in underneath it.
            var popup = GetComponent<Popup>();
            if (popup != null) popup.Close();

            ShopTab.Show();
        }

        public void PlaySoundButton()
        {
            if (SoundController.Instance != null) SoundController.Instance.PlayButtonClickClip();
        }
    }
}
