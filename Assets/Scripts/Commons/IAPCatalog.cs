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
    /// </summary>
    public static class IAPCatalog
    {
        // --- Consumables: spend real money, get currency/items, can be bought repeatedly ---
        public const string Coins500 = "coins_500";
        public const string Coins1200 = "coins_1200";
        public const string Coins3000 = "coins_3000";
        public const string Coins8000 = "coins_8000";
        public const string LivesRefill = "lives_refill";
        public const string PowerupBundle = "powerup_bundle";
        public const string BoosterBundle = "booster_bundle";

        // --- Non-consumables: bought once, owned forever, restorable ---
        public const string RemoveAds = "remove_ads";
        public const string StarterPack = "starter_pack";

        /// <summary>
        /// What the player receives when a product is granted. Powerups and boosters are
        /// credited evenly across every slot (powerup1..4 / booster1..3) rather than
        /// singling one out, which keeps the bundles simple to reason about.
        /// </summary>
        public struct Reward
        {
            public int gold;
            public int lives;
            public int powerupEach;
            public int boosterEach;
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
                [LivesRefill] = new Reward { lives = 30 },
                [PowerupBundle] = new Reward { powerupEach = 5 },
                [BoosterBundle] = new Reward { boosterEach = 5 },
                [RemoveAds] = new Reward { removesAds = true },
                [StarterPack] = new Reward
                {
                    gold = 1000, lives = 10, powerupEach = 3, boosterEach = 3
                },
            };

        /// <summary>
        /// Non-consumables survive a reinstall, so they are re-applied from the store on
        /// every launch instead of being trusted to local save data.
        /// </summary>
        public static bool IsNonConsumable(string productId) =>
            productId == RemoveAds || productId == StarterPack;

        /// <summary>The catalog handed to <c>StoreController.FetchProducts</c>.</summary>
        public static List<ProductDefinition> Definitions => new List<ProductDefinition>
        {
            new ProductDefinition(Coins500, ProductType.Consumable),
            new ProductDefinition(Coins1200, ProductType.Consumable),
            new ProductDefinition(Coins3000, ProductType.Consumable),
            new ProductDefinition(Coins8000, ProductType.Consumable),
            new ProductDefinition(LivesRefill, ProductType.Consumable),
            new ProductDefinition(PowerupBundle, ProductType.Consumable),
            new ProductDefinition(BoosterBundle, ProductType.Consumable),
            new ProductDefinition(RemoveAds, ProductType.NonConsumable),
            new ProductDefinition(StarterPack, ProductType.NonConsumable),
        };
    }
}
