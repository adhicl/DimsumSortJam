using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Sizes a <see cref="GridLayoutGroup"/>'s cells from the width it is actually given, instead
    /// of the fixed cell size the component ships with.
    ///
    /// A fixed cell size only works at one screen shape. The project's CanvasScaler references
    /// 1080x1920 and matches width/height evenly, so a 20:9 phone — most Android hardware now —
    /// reports a canvas only ~966 units wide. A three-column grid of 320-wide cells needs 1020
    /// and spills out of the screen. This divides whatever width the grid really has between the
    /// columns and keeps the cells' shape, so the same grid fits a tall phone and a 4:3 tablet.
    ///
    /// It also keeps the grid's own height honest, so a vertical layout above it can lay the rest
    /// of the page out correctly once the rows are known.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridLayoutGroup))]
    public class ResponsiveGrid : MonoBehaviour
    {
        [Tooltip("Cell height divided by cell width. Cells keep this shape at every screen width.")]
        [SerializeField] private float cellAspect = 400f / 320f;

        private GridLayoutGroup _grid;
        private LayoutElement _element;
        private RectTransform _rect;

        // Last state this actually resized for, so the resize does not run on every layout pass —
        // and, more importantly, does not re-enter when changing the cell size dirties the layout.
        private float _appliedWidth = -1f;
        private int _appliedCells = -1;

        private void OnEnable()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _appliedWidth = -1f;
            Apply();
        }
#endif

        /// <summary>Call after adding or removing tiles at runtime.</summary>
        public void Refresh()
        {
            _appliedWidth = -1f;
            Apply();
        }

        private void Apply()
        {
            if (_grid == null) _grid = GetComponent<GridLayoutGroup>();
            if (_rect == null) _rect = (RectTransform)transform;
            if (_element == null) _element = GetComponent<LayoutElement>();
            if (_grid == null) return;

            float width = _rect.rect.width;
            int cells = CountVisibleCells();
            if (width <= 0f) return;
            if (Mathf.Approximately(width, _appliedWidth) && cells == _appliedCells) return;

            _appliedWidth = width;
            _appliedCells = cells;

            if (_element != null)
            {
                // A GridLayoutGroup advertises a minimum width of columns x cellSize, which a
                // parent layout will honour — so without this the grid pushes itself wider than
                // the screen and the cell size never gets a chance to come down. Declare instead
                // that the width comes from the parent; the cells are then divided out of it.
                _element.minWidth = 0f;
                _element.preferredWidth = 0f;
                _element.flexibleWidth = 1f;
            }

            int columns = Mathf.Max(1, _grid.constraintCount);
            var padding = _grid.padding;

            float usable = width - padding.left - padding.right - _grid.spacing.x * (columns - 1);
            if (usable <= 0f) return;

            float cellWidth = Mathf.Floor(usable / columns);
            _grid.cellSize = new Vector2(cellWidth, Mathf.Round(cellWidth * cellAspect));

            if (_element == null) return;

            int rows = Mathf.CeilToInt(cells / (float)columns);
            float height = rows * _grid.cellSize.y
                           + Mathf.Max(0, rows - 1) * _grid.spacing.y
                           + padding.top + padding.bottom;

            _element.preferredHeight = height;
            _element.minHeight = height;
        }

        private int CountVisibleCells()
        {
            int count = 0;
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf) count++;
            }

            return count;
        }
    }
}
