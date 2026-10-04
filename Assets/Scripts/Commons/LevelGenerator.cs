using System;
using System.Collections.Generic;
using UnityEngine;

namespace Commons
{
    /// <summary>
    /// Everything that decides one level, in the form the level builder edits it.
    ///
    /// Frozen and hidden are counts only. The game picks which pieces carry them every time the
    /// level loads (<c>GameController.ApplyBoardModifiers</c>), so the builder has nothing to place.
    /// </summary>
    [Serializable]
    public class LevelRecipe
    {
        [Tooltip("Pieces in the level. Rounded down to a multiple of 3.")]
        public int totalGoal = 60;

        [Tooltip("Dish types in the level. Capped at TotalGoal / 3, since each type needs a triple.")]
        public int totalVariation = 13;

        [Tooltip("Pieces dealt frozen, across the whole level. The dealer warns and places what fits " +
                 "if this is more than the board can hold.")]
        public int totalFrozen;

        [Tooltip("Rows waiting on plates with their dish hidden, across the whole level.")]
        public int totalHidden;

        [Range(1, 12)] public int totalBaskets = 12;
        [Tooltip("Baskets that start Locked (open when the player matches the dish shown on them).")]
        public int lockedBaskets;
        [Tooltip("Baskets that start Closed (the player buys them open with coins or an ad).")]
        public int closedBaskets;

        [Tooltip("Rows holding one piece. Together with the two-piece rows, this is the board's free space.")]
        public int singleRows = 6;
        [Tooltip("Rows holding two pieces.")]
        public int doubleRows = 6;

        [Tooltip("Customers in the level, spread evenly over the pieces cleared.")]
        public int requestCount;
    }

    /// <summary>
    /// Builds a complete <see cref="LevelData"/> from a <see cref="LevelRecipe"/>, following the
    /// shape every hand-tuned level from 21 up already has: each dish appears in whole triples, no
    /// row ever holds three of one dish, the free space comes from a handful of one- and two-piece
    /// rows, and the rows are split evenly over the baskets that start open.
    /// </summary>
    public static class LevelGenerator
    {
        private const int MaxRowAttempts = 200;

        /// <summary>Fills <paramref name="target"/> in place. Returns what had to be adjusted.</summary>
        public static List<string> Generate(LevelRecipe recipe, LevelData target, System.Random rng)
        {
            var notes = new List<string>();

            int goal = recipe.totalGoal - recipe.totalGoal % 3;
            if (goal != recipe.totalGoal) notes.Add($"TotalGoal rounded down to {goal}.");
            if (goal < 3) throw new ArgumentException("TotalGoal must be at least 3.");

            int triples = goal / 3;
            int variation = Mathf.Clamp(recipe.totalVariation, 1, triples);
            if (variation != recipe.totalVariation) notes.Add($"TotalVariation capped at {variation}.");

            int baskets = Mathf.Clamp(recipe.totalBaskets, 1, 12);
            int locked = Mathf.Max(0, recipe.lockedBaskets);
            int closed = Mathf.Max(0, recipe.closedBaskets);
            if (baskets - locked - closed < 1)
                throw new ArgumentException("At least one basket has to start open.");

            int singles = Mathf.Max(0, recipe.singleRows);
            int doubles = Mathf.Max(0, recipe.doubleRows);
            // Full rows hold 3, so the partial rows have to account for goal mod 3 (which is 0).
            while ((singles + 2 * doubles) % 3 != 0) singles++;
            if (singles != recipe.singleRows) notes.Add($"Single rows raised to {singles} so the pieces divide into rows.");
            if (singles + 2 * doubles > goal) throw new ArgumentException("More pieces in partial rows than in the level.");

            List<int> pieces = BuildPieces(triples, variation, rng);
            DimsumCombination[] rows = BuildRows(pieces, singles, doubles, rng);

            target.TotalGoal = goal;
            target.TotalVariation = variation;
            target.TotalFrozen = Mathf.Max(0, recipe.totalFrozen);
            target.TotalHidden = Mathf.Max(0, recipe.totalHidden);
            target.currentLevel = rows;
            BuildBaskets(baskets, locked, closed, rows.Length, rng,
                out target.firstDisplayed, out target.currentDropArea);
            target.requestMissions = BuildRequests(goal, Mathf.Max(0, recipe.requestCount), rng);

            return notes;
        }

        /// <summary>Every type gets one triple, the rest are dealt out round-robin.</summary>
        private static List<int> BuildPieces(int triples, int variation, System.Random rng)
        {
            var types = new List<int>();
            for (int t = 0; t < variation; t++) types.Add(t);

            var pieces = new List<int>(triples * 3);
            for (int i = 0; i < triples; i++)
            {
                if (i % variation == 0) Shuffle(types, rng);
                int type = types[i % variation];
                pieces.Add(type);
                pieces.Add(type);
                pieces.Add(type);
            }
            return pieces;
        }

        private static DimsumCombination[] BuildRows(List<int> pieces, int singles, int doubles, System.Random rng)
        {
            int full = (pieces.Count - singles - 2 * doubles) / 3;

            // A row that is already a triple would clear itself the moment it lands; reshuffle
            // until none is. With more than a handful of types this almost always takes one pass.
            for (int attempt = 0; attempt < MaxRowAttempts; attempt++)
            {
                Shuffle(pieces, rng);
                var rows = new List<DimsumCombination>(full + singles + doubles);
                int p = 0;
                bool ok = true;

                for (int r = 0; r < full && ok; r++, p += 3)
                {
                    if (pieces[p] == pieces[p + 1] && pieces[p + 1] == pieces[p + 2]) ok = false;
                    rows.Add(new DimsumCombination(pieces[p], pieces[p + 1], pieces[p + 2]));
                }
                if (!ok) continue;

                for (int r = 0; r < doubles; r++, p += 2)
                {
                    var slots = new[] { pieces[p], pieces[p + 1], -1 };
                    Shuffle(slots, rng);
                    rows.Add(new DimsumCombination(slots[0], slots[1], slots[2]));
                }

                for (int r = 0; r < singles; r++, p++)
                {
                    var slots = new[] { pieces[p], -1, -1 };
                    Shuffle(slots, rng);
                    rows.Add(new DimsumCombination(slots[0], slots[1], slots[2]));
                }

                Shuffle(rows, rng);
                return rows.ToArray();
            }

            throw new InvalidOperationException("Could not deal rows without a ready-made triple; add more dish types.");
        }

        private static void BuildBaskets(int baskets, int locked, int closed, int rows, System.Random rng,
            out DisplayedBasket[] displayed, out int[] drops)
        {
            displayed = new DisplayedBasket[baskets];
            var order = new List<int>();
            for (int b = 0; b < baskets; b++) order.Add(b);
            Shuffle(order, rng);
            for (int i = 0; i < locked; i++) displayed[order[i]] = DisplayedBasket.Locked;
            for (int i = locked; i < locked + closed; i++) displayed[order[i]] = DisplayedBasket.Closed;

            int open = baskets - locked - closed;
            drops = new int[baskets];
            int given = 0;
            for (int b = 0; b < baskets; b++)
            {
                if (displayed[b] != DisplayedBasket.Displayed) continue;
                // Earlier baskets take the remainder, matching the hand-made levels.
                drops[b] = rows / open + (given < rows % open ? 1 : 0);
                given++;
            }
        }

        /// <summary>
        /// Customers arrive at even fractions of the pieces cleared. Orders alternate 2, 1, 2
        /// items, and every fourth asks for something already on top, so a run of hard orders
        /// always gets a breather.
        /// </summary>
        private static RequestCharacter[] BuildRequests(int goal, int count, System.Random rng)
        {
            var requests = new RequestCharacter[count];
            int[] pattern = { 2, 1, 2 };
            int start = rng.Next(pattern.Length);
            for (int i = 0; i < count; i++)
            {
                requests[i] = new RequestCharacter
                {
                    totalRequestItems = pattern[(start + i) % pattern.Length],
                    requestTimeShow = Mathf.Round(goal * (i + 1f) / (count + 1f)),
                    getOnTopOnly = i % 4 == 3,
                };
            }
            return requests;
        }

        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int n = list.Count - 1; n > 0; n--)
            {
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}
