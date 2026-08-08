using Commons;
using Controllers;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// The edit-profile popup: pick a name and a face.
    ///
    /// Nothing is written until Save is pressed, so backing out with Cancel or the close button
    /// leaves the profile exactly as it was. Saving goes through <see cref="GameSetting.SaveData"/>,
    /// which is what queues the Cloud Save upload, so the profile travels with the rest of the
    /// save rather than needing its own sync.
    ///
    /// The avatar choices are the grid items already authored in the prefab — this reads them in
    /// order and pairs each with <see cref="AvatarCatalog"/>, so adding an avatar means adding a
    /// toggle to the grid and a sprite to the catalog, with no code change.
    /// </summary>
    public class ProfilePopup : MonoBehaviour
    {
        [SerializeField] private GameSetting gameSetting;
        [SerializeField] private AvatarCatalog avatarCatalog;

        [Header("Fields")]
        [Tooltip("Name entry.")]
        [SerializeField] private TMP_InputField nameField;

        [Tooltip("Large preview of the currently selected avatar.")]
        [SerializeField] private Image avatarPreview;

        [Tooltip("Shows a short form of the Unity Authentication player id.")]
        [SerializeField] private TextMeshProUGUI playerIdLabel;

        [Header("Avatar grid")]
        [Tooltip("Parent of the avatar toggles. Their order must match the catalog's.")]
        [SerializeField] private Transform avatarGrid;

        [Tooltip("Optional 'All: 4/15' counter above the grid.")]
        [SerializeField] private TextMeshProUGUI chosenIndexLabel;

        [SerializeField] private TextMeshProUGUI avatarCountLabel;

        /// <summary>How much of the player id the popup shows. The kit's placeholder is 8 characters.</summary>
        private const int DisplayedIdLength = 8;

        private Toggle[] _toggles;
        private string _pendingAvatarId;

        private void OnEnable()
        {
            Bind();
            Refresh();
        }

        /// <summary>
        /// Hooks every avatar toggle once. Done here rather than in the prefab because the grid
        /// is authored by hand and the count is expected to grow.
        /// </summary>
        private void Bind()
        {
            if (_toggles != null || avatarGrid == null) return;

            _toggles = avatarGrid.GetComponentsInChildren<Toggle>(true);
            for (int i = 0; i < _toggles.Length; i++)
            {
                int index = i;
                _toggles[i].onValueChanged.AddListener(isOn =>
                {
                    if (isOn) Choose(index);
                });
            }
        }

        private void Refresh()
        {
            if (gameSetting == null || avatarCatalog == null) return;

            string playerId = GameServicesController.Instance != null
                ? GameServicesController.Instance.PlayerId
                : null;

            // Sign-in may not have landed the first time this opens; seed whatever we can so the
            // fields are never blank, and let a later open fill in a better default.
            gameSetting.EnsureProfile(playerId, avatarCatalog.DefaultId);

            _pendingAvatarId = gameSetting.avatarId;

            if (nameField != null)
            {
                nameField.characterLimit = GameSetting.MaxPlayerNameLength;
                nameField.text = gameSetting.playerName;
            }

            if (playerIdLabel != null) playerIdLabel.text = ShortId(playerId);
            if (avatarCountLabel != null) avatarCountLabel.text = avatarCatalog.Count.ToString();

            SyncGridToSelection();
            UpdatePreview();
        }

        /// <summary>Turns on the toggle for the saved avatar without treating it as a new choice.</summary>
        private void SyncGridToSelection()
        {
            if (_toggles == null) return;

            int selected = avatarCatalog.IndexOf(_pendingAvatarId);
            if (selected < 0) selected = 0;

            for (int i = 0; i < _toggles.Length; i++)
            {
                _toggles[i].SetIsOnWithoutNotify(i == selected);
            }
        }

        private void Choose(int index)
        {
            _pendingAvatarId = avatarCatalog.IdAt(index);
            UpdatePreview();
            PlayButtonSound();
        }

        private void UpdatePreview()
        {
            if (avatarPreview != null) avatarPreview.sprite = avatarCatalog.Get(_pendingAvatarId);

            if (chosenIndexLabel != null)
            {
                int index = avatarCatalog.IndexOf(_pendingAvatarId);
                chosenIndexLabel.text = (index < 0 ? 1 : index + 1).ToString();
            }
        }

        /// <summary>Wired to the popup's Save button.</summary>
        public void Save()
        {
            PlayButtonSound();
            if (gameSetting == null) return;

            string playerId = GameServicesController.Instance != null
                ? GameServicesController.Instance.PlayerId
                : null;

            gameSetting.playerName = gameSetting.SanitiseName(
                nameField != null ? nameField.text : null, playerId);
            gameSetting.avatarId = _pendingAvatarId;
            gameSetting.SaveData();

            // The Home top bar shows the same name and face, so it has to hear about this.
            if (HomeScene.Instance != null) HomeScene.Instance.RefreshProfile();

            Close();
        }

        /// <summary>Wired to Cancel. Nothing was written, so this only closes.</summary>
        public void Cancel()
        {
            PlayButtonSound();
            Close();
        }

        public void PlayButtonSound()
        {
            if (SoundController.Instance != null) SoundController.Instance.PlayButtonClickClip();
        }

        private void Close()
        {
            var popup = GetComponent<Popup>();
            if (popup != null) popup.Close();
        }

        private static string ShortId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return "—";

            return playerId.Length <= DisplayedIdLength
                ? playerId.ToUpperInvariant()
                : playerId.Substring(0, DisplayedIdLength).ToUpperInvariant();
        }
    }
}
