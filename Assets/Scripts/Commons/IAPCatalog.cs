using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace Commons
{
    /// <summary>
    /// Single source of truth for the in-app purchase catalog.
    ///
    /// The ids below must match, character for character, the product ids created in the
    /// Google Play Console (and mirrored in the Unity Cloud Dashboard catalog). The full
    /// list of what to create on the cloud side lives in next_step.md at the project
    /// root — edit both places together if a product is added, renamed, or removed.
    ///
    /// The shop tab in Home was generated from this catalog once and is now maintained by hand,
    /// so **adding a product here does not add its card** — the row has to be added in
    /// <c>Home.unity</c> as well. Whatever a card prints must match <see cref="Rewards"/>, or the
    /// shelf advertises something different from what the purchase actually grants. Prices are
    /// the exception: never author them, <see cref="UI.IAPBuyButton"/> fills them in from the
    /// store at runtime in the player's own currency.
    /// </summary>
    public static class IAPCatalog
    {
        // --- Gold: consumable, repeatable. Six tiers, shown as a 3x2 grid in the shop tab. ---
        public const string Coins500 = "coins_500";
        public const string Coins1200 = "coins_1200";
        public const string Coins3000 = "coins_3000";
        public const string Coins8000 = "coins_8000";
        public const string Coins20000 = "coins_20000";
        public const string Coins50000 = "coins_50000";

        // Lives are not sold on their own. Every route out of an empty life bar goes through a
        // bundle, so there is deliberately no lives_refill product.


        // --- Bundles: consumable, repeatable. Mixed gold / power-ups / lives. ---
        public const string PowerupBundle = "powerup_bundle";
        public const string BundleBig = "bundle_big";
        public const string BundleGreat = "bundle_great";
        public const string BundleUltra = "bundle_ultra";
        public const string BundleSuperior = "bundle_superior";
        public const string BundleLegendary = "bundle_legendary";

        // --- Non-consumables: bought once, owned forever, restorable ---
        public const string RemoveAds = "remove_ads";
        public const string StarterPack = "starter_pack";
        public const string BundleNoAds = "bundle_no_ads";

        /// <summary>
        /// What the player receives when a product is granted. Power-ups are credited evenly
        /// across every slot (powerup1..4) rather than singling one out, which keeps the
        /// bundles simple to reason about.
        ///
        /// There is deliberately no booster reward: boosters are not implemented in the game
        /// yet, so every bundle that would have carried them carries extra lives instead.
        ///
        /// Lives are only ever sold as unlimited time: <see cref="unlimitedLifeHours"/> buys a
        /// window during which lives are not consumed at all, and the shop prints it as "2h" /
        /// "1d" next to an infinity heart. <see cref="lives"/> tops up the counter (capped at
        /// <see cref="GameSetting.MaxLife"/>) and is kept for rewards that are not purchases.
        /// </summary>
        public struct Reward
        {
            public int gold;
            public int lives;
            public int unlimitedLifeHours;
            public int powerupEach;
            public bool removesAds;
        }

        /// <summary>Product id to reward. Every id in <see cref="Definitions"/> has an entry here.</summary>
        public static readonly IReadOnlyDictionary<string, Reward> Rewards =
            new Dictionary<string, Reward>
            {
                [Coins500] = new Reward { gold = 500 },
                [Coins1200] = new Reward { gold = 1200 },
                [Coins3000] = new Reward { gold = 3000 },
                [Coins8000] = new Reward { gold = 8000 },
                [Coins20000] = new Reward { gold = 20000 },
                [Coins50000] = new Reward { gold = 50000 },

                [PowerupBundle] = new Reward { powerupEach = 5, unlimitedLifeHours = 1 },
                [BundleBig] = new Reward { gold = 2500, powerupEach = 2, unlimitedLifeHours = 2 },
                [BundleGreat] = new Reward { gold = 5500, powerupEach = 3, unlimitedLifeHours = 3 },
                [BundleUltra] = new Reward { gold = 12000, powerupEach = 6, unlimitedLifeHours = 6 },
                [BundleSuperior] = new Reward { gold = 25000, powerupEach = 12, unlimitedLifeHours = 12 },
                [BundleLegendary] = new Reward { gold = 50000, powerupEach = 24, unlimitedLifeHours = 24 },

                [RemoveAds] = new Reward { removesAds = true },
                [StarterPack] = new Reward { gold = 4000, powerupEach = 2, unlimitedLifeHours = 2 },
                [BundleNoAds] = new Reward
                {
                    gold = 2000, powerupEach = 2, unlimitedLifeHours = 1, removesAds = true
                },
            };

        /// <summary>
        /// Non-consumables survive a reinstall, so they are re-applied from the store on
        /// every launch instead of being trusted to local save data. Anything that turns ads
        /// off permanently has to be in here, or the entitlement is lost on reinstall.
        /// </summary>
        public static bool IsNonConsumable(string productId) =>
            productId == RemoveAds ||
            productId == StarterPack ||
            productId == BundleNoAds;

        /// <summary>The catalog handed to <c>StoreController.FetchProducts</c>.</summary>
        public static List<ProductDefinition> Definitions => new List<ProductDefinition>
        {
            new ProductDefinition(Coins500, ProductType.Consumable),
            new ProductDefinition(Coins1200, ProductType.Consumable),
            new ProductDefinition(Coins3000, ProductType.Consumable),
            new ProductDefinition(Coins8000, ProductType.Consumable),
            new ProductDefinition(Coins20000, ProductType.Consumable),
            new ProductDefinition(Coins50000, ProductType.Consumable),

            new ProductDefinition(PowerupBundle, ProductType.Consumable),
            new ProductDefinition(BundleBig, ProductType.Consumable),
            new ProductDefinition(BundleGreat, ProductType.Consumable),
            new ProductDefinition(BundleUltra, ProductType.Consumable),
            new ProductDefinition(BundleSuperior, ProductType.Consumable),
            new ProductDefinition(BundleLegendary, ProductType.Consumable),

            new ProductDefinition(RemoveAds, ProductType.NonConsumable),
            new ProductDefinition(StarterPack, ProductType.NonConsumable),
            new ProductDefinition(BundleNoAds, ProductType.NonConsumable),
        };
    }
}
