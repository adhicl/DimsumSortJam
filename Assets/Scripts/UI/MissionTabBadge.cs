using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// The red number on the Missions tab: how many missions are full and waiting to be claimed.
    ///
    /// It lives on the paged scroll rect rather than on the tab button, because that button is
    /// not a fixed object. <c>PagedRect.UpdatePagination</c> frees every tab button and hands them
    /// out again from a pool each time the page changes, so whichever object is "page 3" right now
    /// may have been "page 1" a moment ago. This finds the current one by the name PagedRect gives
    /// it (<c>"Button - Page N "</c>) every frame, and turns off the bubble on every other button.
    ///
    /// The bubble is the kit's own <c>Alert-Bubble</c> child. The button that represents the page
    /// you are on uses a template without one, so the badge hides while the Missions tab is open.
    /// </summary>
    public class MissionTabBadge : MonoBehaviour
    {
        private const string BubbleName = "Alert-Bubble";

        [SerializeField] private MissionsListController missions;

        [Tooltip("The Pagination object whose children are the tab buttons.")]
        [SerializeField] private Transform pagination;

        [Tooltip("Page number of the Missions page in the PagedRect (1-based).")]
        [SerializeField] private int missionsPageNumber = 3;

        [Tooltip("Where the bubble sits, measured in from the tab button's top-right corner.")]
        [SerializeField] private Vector2 cornerOffset = new Vector2(-10f, -10f);

        [Tooltip("Size of the bubble once it has popped in. The kit's bubble is 64px at 1.")]
        [SerializeField] private float bubbleScale = 1.3f;

        private string _buttonPrefix;
        private int _claimable;

        private void Awake()
        {
            // Trailing space included, so page 3 does not also match page 30.
            _buttonPrefix = $"Button - Page {missionsPageNumber} ";
        }

        private void OnEnable()
        {
            MissionsListController.ProgressChanged += Recount;
            Recount();
        }

        private void OnDisable()
        {
            MissionsListController.ProgressChanged -= Recount;
        }

        private void Recount()
        {
            _claimable = missions != null ? missions.ClaimableCount() : 0;
        }

        private void LateUpdate()
        {
            if (pagination == null) return;

            for (int i = 0; i < pagination.childCount; i++)
            {
                Transform button = pagination.GetChild(i);
                Transform bubble = button.Find(BubbleName);
                if (bubble == null) continue;

                bool show = _claimable > 0
                            && button.gameObject.activeSelf
                            && button.name.StartsWith(_buttonPrefix);

                if (show)
                {
                    if (!bubble.gameObject.activeSelf) Place(bubble);
                    var label = bubble.GetComponentInChildren<TextMeshProUGUI>(true);
                    string text = _claimable.ToString();
                    if (label != null && label.text != text) label.text = text;
                }

                if (bubble.gameObject.activeSelf != show)
                {
                    bubble.gameObject.SetActive(show);
                    if (show) PopIn(bubble);
                    else bubble.DOKill();
                }
            }
        }

        /// <summary>
        /// The kit ships the bubble at 1% scale, expecting it to be animated in, so it has to be
        /// grown or it stays an invisible speck.
        /// </summary>
        private void PopIn(Transform bubble)
        {
            bubble.DOKill();
            bubble.localScale = Vector3.zero;
            bubble.DOScale(bubbleScale, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        /// <summary>
        /// Pins the bubble to the button's top-right corner. The tab button lays its children out
        /// in a row, so a bubble left in that layout would push the icon aside instead of sitting
        /// on top of it.
        /// </summary>
        private void Place(Transform bubble)
        {
            var layout = bubble.GetComponent<LayoutElement>();
            if (layout != null) layout.ignoreLayout = true;

            var rect = (RectTransform)bubble;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = cornerOffset;
            rect.SetAsLastSibling();
        }
    }
}
