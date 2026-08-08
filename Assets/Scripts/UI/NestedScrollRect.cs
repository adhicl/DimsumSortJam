using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// A vertical ScrollRect that can live inside a horizontal one without swallowing its swipes.
    ///
    /// Unity's ScrollRect consumes every drag it receives and never passes it up, so the shop
    /// list — which sits inside the Home scene's horizontal PagedRect — would otherwise trap
    /// the player on the shop page: only the tab buttons could get them out. This picks an axis
    /// once per gesture and hands the whole gesture to whichever scroll rect owns that axis.
    /// </summary>
    public class NestedScrollRect : ScrollRect
    {
        private ScrollRect _outer;
        private bool _routeToOuter;

        protected override void Awake()
        {
            base.Awake();

            // Start the search above this object so we never find ourselves.
            if (transform.parent != null)
            {
                _outer = transform.parent.GetComponentInParent<ScrollRect>();
            }
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            // Measured from the press rather than this frame's delta: by the time the drag
            // threshold is crossed the accumulated travel shows the player's intent, while a
            // single frame's delta is often a rounding-sized jitter pointing the wrong way.
            Vector2 travel = eventData.position - eventData.pressPosition;
            _routeToOuter = _outer != null && Mathf.Abs(travel.x) > Mathf.Abs(travel.y);

            if (_routeToOuter) _outer.OnBeginDrag(eventData);
            else base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (_routeToOuter) _outer.OnDrag(eventData);
            else base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (_routeToOuter) _outer.OnEndDrag(eventData);
            else base.OnEndDrag(eventData);

            _routeToOuter = false;
        }
    }
}
