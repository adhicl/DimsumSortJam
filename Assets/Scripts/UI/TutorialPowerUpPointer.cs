using Commons;
using Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Keeps the tutorial cover's DownArrow centred over the power-up that tutorial unlocks.
    ///
    /// The arrow cannot simply be placed in the scene and left there. The power-up row is a
    /// <see cref="HorizontalLayoutGroup"/> with <c>childForceExpandWidth</c>, so the four slots are
    /// spread across the toolbar's width — and the toolbar is anchored to both screen edges while
    /// the CanvasScaler runs at <c>match = 0.5</c>, which makes the canvas width in canvas units a
    /// function of the device's aspect ratio. A hand-placed arrow is therefore only ever correct at
    /// one aspect; on anything taller or wider it drifts off its button. That was already true of
    /// the positions this replaces.
    ///
    /// Which slot to point at is derived, not configured: the row is ordered by unlock level (see
    /// <see cref="PlayTopBar"/>), so the slot being unlocked is the one whose level is the level
    /// the player is about to reach. Nothing per-scene has to be kept in step.
    /// </summary>
    public class TutorialPowerUpPointer : MonoBehaviour
    {
        [Tooltip("The arrow to move. Defaults to a child named DownArrow.")]
        [SerializeField] private RectTransform arrow;

        [Tooltip("The row of power-up slots. Defaults to ScrollRect-Toolbar/Viewport/Content.")]
        [SerializeField] private RectTransform row;

        [Tooltip("Read for the player's current level. Found from the scene when left empty.")]
        [SerializeField] private GameSetting gameSetting;

        private RectTransform _target;
        private float _lastTargetX = float.NaN;
        private float _lastRowWidth = float.NaN;

        private void OnEnable()
        {
            Resolve();
            Reposition(force: true);
        }

        private void Resolve()
        {
            if (arrow == null) arrow = transform.Find("DownArrow") as RectTransform;
            if (row == null) row = transform.Find("ScrollRect-Toolbar/Viewport/Content") as RectTransform;
            if (gameSetting == null)
            {
                var game = GameController.Instance;
                if (game != null) gameSetting = game.GameSetting;
            }
            if (gameSetting == null)
            {
                var bar = FindObjectOfType<PlayTopBar>(true);
                if (bar != null) gameSetting = bar.gameSetting;
            }

            _target = FindTargetSlot();
        }

        /// <summary>
        /// The slot for the power-up unlocking at the level the player is about to reach. The row
        /// is in unlock order, so the nth slot is the nth lowest unlock level — no need to know
        /// which power-up is which.
        /// </summary>
        private RectTransform FindTargetSlot()
        {
            if (row == null || gameSetting == null) return null;

            var levels = new[]
            {
                Settings.minLevelPowerup1, Settings.minLevelPowerup2,
                Settings.minLevelPowerup3, Settings.minLevelPowerup4
            };
            System.Array.Sort(levels);

            int unlocking = gameSetting.currentLevel + 1;
            for (int i = 0; i < levels.Length && i < row.childCount; i++)
            {
                if (levels[i] == unlocking) return row.GetChild(i) as RectTransform;
            }

            // No power-up unlocks here. Leave the arrow exactly where it was authored rather than
            // guessing at a slot.
            return null;
        }

        private void LateUpdate()
        {
            Reposition(force: false);
        }

        /// <summary>
        /// Puts the arrow over the target slot. Runs every frame the cover is up, but only touches
        /// the transform when the slot has actually moved — the layout settles a frame or two after
        /// the cover is enabled, and can move again on an orientation change.
        /// </summary>
        private void Reposition(bool force)
        {
            if (arrow == null || _target == null) return;

            float rowWidth = row != null ? row.rect.width : 0f;
            float targetX = _target.TransformPoint(_target.rect.center).x;

            if (!force
                && Mathf.Approximately(targetX, _lastTargetX)
                && Mathf.Approximately(rowWidth, _lastRowWidth))
            {
                return;
            }

            _lastTargetX = targetX;
            _lastRowWidth = rowWidth;

            // Compared centre to centre, and applied as a world delta converted into the arrow's
            // own parent space, so no assumption is made about anchors or pivots.
            float scale = arrow.parent != null ? arrow.parent.lossyScale.x : 1f;
            if (Mathf.Approximately(scale, 0f)) scale = 1f;

            float currentX = arrow.TransformPoint(arrow.rect.center).x;
            float deltaLocal = (targetX - currentX) / scale;

            arrow.anchoredPosition = new Vector2(arrow.anchoredPosition.x + deltaLocal,
                                                 arrow.anchoredPosition.y);
        }
    }
}
