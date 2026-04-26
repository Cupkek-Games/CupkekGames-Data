using System;
using System.Collections.Generic;
using CupkekGames.Data;
using UnityEngine;

namespace CupkekGames.Data.DropTable
{
    [Serializable]
    public struct DropResult
    {
        public CatalogKey Key;
        public int Amount;

        public DropResult(CatalogKey key, int amount)
        {
            Key = key;
            Amount = amount;
        }
    }

    [Serializable]
    public class DropTable : IData
    {
        [SerializeField] private List<DropEntry> _entries = new();

        public DropTable() { }

        public IReadOnlyList<DropEntry> Entries => _entries;

        /// <summary>
        /// Evaluates the drop table and returns a list of drop results.
        /// </summary>
        /// <param name="minimumDrops">Minimum number of drops guaranteed. If fewer entries pass the roll,
        /// the highest-chance entries are forced in until the minimum is met.</param>
        /// <param name="chanceMultiplier">Global multiplier applied to all chances (clamped 0–1 after).</param>
        /// <param name="limit">Maximum number of drops. 0 = unlimited.</param>
        /// <param name="chanceModifier">Per-entry chance override. Return value replaces the entry's chance.</param>
        public List<DropResult> Evaluate(
            int minimumDrops = 0,
            float chanceMultiplier = 1f,
            int limit = 0,
            Func<DropEntry, float> chanceModifier = null)
        {
            if (chanceModifier == null)
                return Evaluate(minimumDrops, chanceMultiplier, limit, modifier: null);

            return Evaluate(
                minimumDrops,
                chanceMultiplier,
                limit,
                modifier: entry => (chance: chanceModifier(entry), amountAddition: 0));
        }

        /// <summary>
        /// Evaluates the drop table and returns a list of drop results.
        /// </summary>
        /// <param name="minimumDrops">Minimum number of drops guaranteed. If fewer entries pass the roll,
        /// the highest-effective-chance entries are forced in until the minimum is met.</param>
        /// <param name="chanceMultiplier">Global multiplier applied to all chances when <paramref name="modifier"/> is null.</param>
        /// <param name="limit">Maximum number of drops. 0 = unlimited.</param>
        /// <param name="modifier">
        /// Optional per-entry modifier. When provided, it fully overrides the effective chance and provides
        /// an amount bonus to add to the entry's min/max amounts.
        /// </param>
        /// <remarks>
        /// Effective chance semantics:
        /// - If <paramref name="modifier"/> is null: effectiveChance = entry.Chance * chanceMultiplier.
        /// - If <paramref name="modifier"/> is not null: effectiveChance = modifier(entry).chance
        ///   (chanceMultiplier is ignored when modifier is provided; returned chance is treated as final effective chance).
        ///
        /// Amount semantics (applies both to normal rolls and forced minimum fill):
        /// - Start from entry.MinAmount..entry.MaxAmount (inclusive).
        /// - Add amountAddition to both min and max.
        /// - Clamp so min >= 1 and max >= min.
        /// - Pick random amount from min..max (inclusive). If min == max, amount is fixed.
        /// </remarks>
        public List<DropResult> Evaluate(
            int minimumDrops = 0,
            float chanceMultiplier = 1f,
            int limit = 0,
            Func<DropEntry, (float chance, int amountAddition)> modifier = null)
        {
            // Minimum must win over limit (legacy-feel semantics).
            if (limit > 0 && minimumDrops > 0 && minimumDrops > limit)
                limit = minimumDrops;

            var results = new List<DropResult>();
            var pending = new List<(DropEntry entry, float chance, int amountAddition)>();

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Key.IsEmpty)
                    continue;

                int amountAddition = 0;
                float chance;
                if (modifier != null)
                {
                    var m = modifier(entry);
                    chance = m.chance;
                    amountAddition = m.amountAddition;
                }
                else
                {
                    chance = entry.Chance * chanceMultiplier;
                }

                // Note: clamp after modifier/multiplier, so modifier can safely return out-of-range values.
                chance = Mathf.Clamp01(chance);

                if (UnityEngine.Random.value <= chance)
                {
                    int amount = EvaluateAmount(entry, amountAddition);
                    results.Add(new DropResult(entry.Key, amount));
                }
                else
                {
                    pending.Add((entry, chance, amountAddition));
                }
            }

            // Guarantee minimum drops by forcing highest-effective-chance entries that didn't pass.
            if (minimumDrops > 0 && results.Count < minimumDrops)
            {
                pending.Sort((a, b) => b.chance.CompareTo(a.chance));

                for (int i = 0; i < pending.Count && results.Count < minimumDrops; i++)
                {
                    var p = pending[i];
                    int amount = EvaluateAmount(p.entry, p.amountAddition);
                    results.Add(new DropResult(p.entry.Key, amount));
                }
            }

            // Enforce limit (min already won over limit above).
            if (limit > 0 && results.Count > limit)
                results.RemoveRange(limit, results.Count - limit);

            return results;
        }

        private static int EvaluateAmount(DropEntry entry, int amountAddition)
        {
            // Start from entry.Min..entry.Max, add amountAddition to both bounds.
            int min = entry.MinAmount + amountAddition;
            int max = entry.MaxAmount + amountAddition;

            // Clamp so min >= 1 and max >= min.
            if (min < 1)
                min = 1;
            if (max < min)
                max = min;

            // Pick random amount if range is valid; otherwise fixed.
            return min >= max
                ? min
                : UnityEngine.Random.Range(min, max + 1);
        }

        public bool Validate() => true;
        public void OnAfterDeserialize() { }

        public DropTable(DropTable other)
        {
            if (other?._entries == null)
                return;
            for (int i = 0; i < other._entries.Count; i++)
                _entries.Add(new DropEntry(other._entries[i]));
        }

        public IData CloneData() => new DropTable(this);
    }
}
