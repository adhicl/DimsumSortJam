using System;
using Unity.Services.Analytics;
using UnityEngine;

namespace Commons
{
    /// <summary>
    /// Every custom Analytics event the game sends, in one place.
    ///
    /// Call sites use these named methods rather than building a <see cref="CustomEvent"/>
    /// themselves, so the event names and parameter keys exist exactly once. They have to match
    /// the schemas registered in the UGS dashboard (Analytics > Event Manager) character for
    /// character — an event whose name or parameters are not declared there fails validation and
    /// never reaches a report, which looks identical to "analytics is broken" from the dashboard.
    /// next_step.md 3a is the console procedure; "Analytics events" there is the code-side map.
    ///
    /// Nothing here throws. <c>AnalyticsService.Instance</c> throws
    /// <see cref="Unity.Services.Core.ServicesInitializationException"/> until UGS finishes
    /// initializing, and gameplay can reach a level before that lands on a cold start, so every
    /// call is gated on <see cref="IsReady"/> and wrapped. A dropped event must never cost a
    /// player their level.
    /// </summary>
    public static class GameAnalytics
    {
        /// <summary>
        /// Set by <c>GameServicesController</c> once UGS is initialized and data collection has
        /// actually started. Until then every Record call below is a no-op: the SDK itself also
        /// silently discards events while inactive, but reaching it would throw first.
        /// </summary>
        public static bool IsReady { get; set; }

        /// <summary>Reason strings shared by the fail / revive / offer events so they group in the dashboard.</summary>
        public const string ReasonOutOfTime = "outOfTime";
        public const string ReasonOutOfMoves = "outOfMoves";

        /// <summary>Where a rewarded ad was offered. One string per button in the game.</summary>
        public const string PlacementContinueGame = "continueGame";
        public const string PlacementReviveOutOfMove = "reviveOutOfMove";
        public const string PlacementUnlockBasket = "unlockBasket";
        public const string PlacementBuyPowerUp = "buyPowerUp";
        public const string PlacementDoubleReward = "doubleReward";

        /// <summary>A level was dealt and handed to the player.</summary>
        public static void LevelStarted(int levelIndex)
        {
            Record("levelStarted", e =>
            {
                e["levelIndex"] = levelIndex;
            });
        }

        /// <summary>The goal was met. <paramref name="durationSeconds"/> is wall-clock time on the level, revives included.</summary>
        public static void LevelCompleted(int levelIndex, int durationSeconds, int reviveCount)
        {
            Record("levelCompleted", e =>
            {
                e["levelIndex"] = levelIndex;
                e["durationSeconds"] = durationSeconds;
                e["reviveCount"] = reviveCount;
            });
        }

        /// <summary>
        /// The player confirmed the loss. Deliberately not sent when the revive popup opens —
        /// that is <see cref="ReviveOfferShown"/>. Only a level the player actually gave up on
        /// counts as failed, otherwise the funnel double-counts every rescued level.
        /// </summary>
        public static void LevelFailed(int levelIndex, string failReason, int durationSeconds, int reviveCount)
        {
            Record("levelFailed", e =>
            {
                e["levelIndex"] = levelIndex;
                e["failReason"] = failReason ?? "unknown";
                e["durationSeconds"] = durationSeconds;
                e["reviveCount"] = reviveCount;
            });
        }

        /// <summary>The revive was paid for and granted — the counterpart to <see cref="ReviveOfferShown"/>.</summary>
        public static void LevelRevived(int levelIndex, string reviveReason)
        {
            Record("levelRevived", e =>
            {
                e["levelIndex"] = levelIndex;
                e["reviveReason"] = reviveReason ?? "unknown";
            });
        }

        /// <summary>
        /// The board ran out of moves and was rearranged. <paramref name="rescued"/> is false when
        /// every attempt still left a dead board — the case worth watching, since it means the
        /// level's deal can strand a player.
        /// </summary>
        public static void BoardReshuffled(int levelIndex, int attempts, bool rescued)
        {
            Record("boardReshuffled", e =>
            {
                e["levelIndex"] = levelIndex;
                e["attempts"] = attempts;
                e["rescued"] = rescued;
            });
        }

        /// <summary>A revive popup was put in front of the player. Pairs with <see cref="LevelRevived"/> to give the offer's take rate.</summary>
        public static void ReviveOfferShown(int levelIndex, string offerReason)
        {
            Record("reviveOfferShown", e =>
            {
                e["levelIndex"] = levelIndex;
                e["offerReason"] = offerReason ?? "unknown";
            });
        }

        /// <summary>The player watched a rewarded ad through to the reward.</summary>
        public static void RewardedAdCompleted(string placement)
        {
            Record("rewardedAdCompleted", e =>
            {
                e["placement"] = placement ?? "unknown";
            });
        }

        /// <summary>
        /// A rewarded ad was asked for and none was ready. Worth its own event: it is lost revenue
        /// that no impression-side metric reports, because the impression never happened.
        /// </summary>
        public static void RewardedAdUnavailable(string placement)
        {
            Record("rewardedAdUnavailable", e =>
            {
                e["placement"] = placement ?? "unknown";
            });
        }

        /// <summary>An IAP was granted. Fires on the grant, not the order, so re-delivered orders do not count twice.</summary>
        public static void IapPurchased(string productId)
        {
            Record("iapPurchased", e =>
            {
                // "productID", not "productId" — the capital D is deliberate and must match the
                // Event Manager schema exactly. The C# parameter keeps the codebase's own
                // productId spelling; only the wire key differs. Do not "fix" the casing here.
                e["productID"] = productId ?? "unknown";
            });
        }

        /// <summary>
        /// The one place an event reaches the SDK. Takes the name separately because
        /// <c>CustomEvent.Name</c> is internal to the package and cannot be read back for the
        /// failure log, and takes a fill delegate so each event still names itself exactly once.
        /// </summary>
        private static void Record(string name, Action<CustomEvent> fill)
        {
            if (!IsReady) return;

            try
            {
                var e = new CustomEvent(name);
                fill(e);
                AnalyticsService.Instance.RecordEvent(e);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Analytics] Failed to record '{name}': {ex.Message}");
            }
        }
    }
}
