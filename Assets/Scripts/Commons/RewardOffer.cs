namespace Commons
{
    /// <summary>
    /// The offer half of the rewarded-ad funnel for one popup: shown once, then either accepted or
    /// declined once. Each popup that offers an ad owns one of these.
    ///
    /// Accepted wins over declined. A player who taps "watch ad", finds no ad, and then pays coins
    /// instead has still accepted the offer — the failure shows up as reward_ad_show_failed, and
    /// counting a decline as well would make the take rate add up to more than 100%.
    /// </summary>
    public sealed class RewardOffer
    {
        public const string ChoiceCoins = "coins";
        public const string ChoiceClaimNormal = "claim_normal";
        public const string ChoiceLeave = "leave";
        public const string ChoiceQuit = "quit";
        public const string ChoiceClosed = "closed";

        private readonly string _placement;
        private bool _shown;
        private bool _resolved;

        /// <param name="placement">One of the <c>GameAnalytics.Placement*</c> constants.</param>
        public RewardOffer(string placement)
        {
            _placement = GameAnalytics.ToRewardPlacement(placement);
        }

        /// <summary>The popup is on screen. Logged once, however often it is called.</summary>
        public void Shown()
        {
            if (_shown) return;
            _shown = true;

            var ads = Controllers.RewardedAdController.Instance;
            bool ready = ads != null && ads.IsReady;
            FirebaseTracker.RewardOfferShown(_placement, GameAnalytics.CurrentLevelNumber(), ready);
        }

        /// <summary>The player tapped the ad option.</summary>
        public void Accepted()
        {
            if (!_shown || _resolved) return;
            _resolved = true;
            FirebaseTracker.RewardOfferAccepted(_placement, GameAnalytics.CurrentLevelNumber());
        }

        /// <summary>The player chose another route. Ignored once the offer was already accepted or declined.</summary>
        public void Declined(string choice)
        {
            if (!_shown || _resolved) return;
            _resolved = true;
            FirebaseTracker.RewardOfferDeclined(_placement, GameAnalytics.CurrentLevelNumber(), choice);
        }

        /// <summary>Call when the popup goes away; an offer left unanswered counts as closed.</summary>
        public void Dismissed()
        {
            Declined(ChoiceClosed);
        }
    }
}
