using UI.Pagination;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Jumping to the shop from anywhere in the Home scene.
    ///
    /// The shop is a page of Home's horizontal pager rather than a popup or its own scene, so
    /// "open the shop" means moving the pager. Everything that used to sell lives points here
    /// now — lives are only sold as part of a bundle.
    /// </summary>
    public static class ShopTab
    {
        /// <summary>Page number of <c>Panel-Shop</c> in Home's PagedRect. PagedRect pages are 1-based.</summary>
        public const int PageNumber = 1;

        /// <summary>
        /// Slides Home to the shop. Returns false when there is no pager in the loaded scene —
        /// the caller is somewhere the shop cannot be reached, and should say so rather than
        /// silently doing nothing.
        /// </summary>
        public static bool Show()
        {
            var pager = Object.FindFirstObjectByType<PagedRect>();
            if (pager == null)
            {
                Debug.LogWarning("[ShopTab] No PagedRect in this scene; cannot open the shop.");
                return false;
            }

            pager.SetCurrentPage(PageNumber);
            return true;
        }
    }
}
