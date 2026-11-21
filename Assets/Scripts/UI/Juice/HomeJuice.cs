using System.Collections.Generic;
using UI.Pagination;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Juice
{
    /// <summary>
    /// Home screen movement, attached at runtime by <see cref="HomeScene"/> so the Home scene file
    /// never has to be re-saved: switching tabs bounces the tab button and pops the new page's
    /// contents in - the centre buttons on Home, the rows on Missions, the items in the Shop.
    /// </summary>
    public class HomeJuice : MonoBehaviour
    {
        private PagedRect _pages;

        public static void Install(PagedRect pages)
        {
            if (pages == null || pages.GetComponent<HomeJuice>() != null) return;
            pages.gameObject.AddComponent<HomeJuice>()._pages = pages;
        }

        private void Start()
        {
            if (_pages == null) return;
            _pages.PageChangedEvent.AddListener(OnPageChanged);
            // The page the player lands on gets its entrance too.
            Page current = _pages.Pages.Find(p => p != null && p.PageNumber == _pages.CurrentPage);
            if (current != null) PopPage(current, 0.25f);
        }

        private void OnDestroy()
        {
            if (_pages != null) _pages.PageChangedEvent.RemoveListener(OnPageChanged);
        }

        private void OnPageChanged(Page newPage, Page previousPage)
        {
            if (newPage == null) return;
            BounceTab(newPage.PageNumber);
            PopPage(newPage, 0.05f);
        }

        // Tab buttons are pooled and renamed by PagedRect ("Button - Page N "), so the current
        // one is found by name each time, as MissionTabBadge does.
        private void BounceTab(int pageNumber)
        {
            if (_pages.Pagination == null) return;
            string prefix = $"Button - Page {pageNumber} ";
            foreach (Transform button in _pages.Pagination.transform)
            {
                if (button.gameObject.activeSelf && button.name.StartsWith(prefix))
                    ScaleJuice.For(button).Punch(0.22f, 0.4f);
            }
        }

        private static void PopPage(Page page, float delay)
        {
            var items = new List<Transform>();

            // Missions: the rows.
            var missions = page.GetComponentInChildren<MissionsListController>(true);
            if (missions != null)
            {
                foreach (Transform row in missions.transform) items.Add(row);
                UIJuice.PopIn(items, delay, 0.05f, 6);
                return;
            }

            // Home: the illustration and the play row.
            Transform center = FindDeep(page.transform, "Content-Center");
            if (center != null)
            {
                foreach (Transform piece in center) items.Add(piece);
                UIJuice.PopIn(items, delay, 0.08f);
                return;
            }

            // Shop: the first screenful of whatever the scroll view holds.
            var scroll = page.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null && scroll.content != null)
            {
                foreach (Transform item in scroll.content) items.Add(item);
                UIJuice.PopIn(items, delay, 0.05f, 6);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
