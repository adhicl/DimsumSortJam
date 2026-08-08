using UnityEngine;

namespace Commons
{
    /// <summary>
    /// The avatars a player can pick from, in the order the profile popup shows them.
    ///
    /// Both the popup's grid and the Home top bar resolve a saved choice through here, so there
    /// is one list to edit rather than two places that can drift apart. Lookups are by sprite
    /// name (see <see cref="GameSetting.avatarId"/>) so reordering the list never changes an
    /// existing player's face.
    /// </summary>
    [CreateAssetMenu(fileName = "AvatarCatalog", menuName = "AvatarCatalog", order = 1)]
    public class AvatarCatalog : ScriptableObject
    {
        [Tooltip("Every pickable avatar, in the order the profile popup lists them.")]
        [SerializeField] private Sprite[] avatars;

        public int Count => avatars == null ? 0 : avatars.Length;

        /// <summary>The avatar a player gets before they have chosen one.</summary>
        public string DefaultId => Count > 0 ? avatars[0].name : string.Empty;

        public Sprite At(int index) =>
            avatars != null && index >= 0 && index < avatars.Length ? avatars[index] : null;

        public string IdAt(int index)
        {
            var sprite = At(index);
            return sprite == null ? string.Empty : sprite.name;
        }

        /// <summary>
        /// The sprite for a saved id, falling back to the first avatar. A missing id means the
        /// art was renamed or removed after the player picked it — showing the default beats
        /// showing an empty square.
        /// </summary>
        public Sprite Get(string avatarId)
        {
            if (avatars == null || avatars.Length == 0) return null;
            if (string.IsNullOrEmpty(avatarId)) return avatars[0];

            foreach (var sprite in avatars)
            {
                if (sprite != null && sprite.name == avatarId) return sprite;
            }

            return avatars[0];
        }

        public int IndexOf(string avatarId)
        {
            if (avatars == null) return -1;

            for (int i = 0; i < avatars.Length; i++)
            {
                if (avatars[i] != null && avatars[i].name == avatarId) return i;
            }

            return -1;
        }
    }
}
