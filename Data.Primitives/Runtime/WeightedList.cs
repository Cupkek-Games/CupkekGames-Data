using CupkekGames.KeyValueDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CupkekGames.Data.Primitives
{
    [Serializable]
    public class WeightedList<TKey> : KeyValueDatabase<TKey, int>
    {
        private TKey GetValueAtWeight(int weight, IEnumerable<TKey> keys)
        {
            foreach (TKey key in keys)
            {
                weight -= GetValue(key);
                if (weight <= 0)
                {
                    return key;
                }
            }

            return default;
        }
        private TKey GetValueAtWeight(int weight) => GetValueAtWeight(weight, Keys);

        private int GetTotalWeight() => Values.Sum();

        private int GetRandomWeight() => UnityEngine.Random.Range(0, GetTotalWeight());

        public TKey GetRandomItem() => GetValueAtWeight(GetRandomWeight());

        public List<TKey> GetRandomUniqueItems(int numberOfItemsToSelect)
        {
            if (numberOfItemsToSelect >= Count)
            {
                return Keys.ToList();
            }

            List<TKey> result = new();
            List<TKey> copiedKeys = new(Keys);

            int totalWeight = GetTotalWeight();

            for (int i = 0; i < numberOfItemsToSelect; i++)
            {
                int randomValue = UnityEngine.Random.Range(0, totalWeight);
                TKey randomKey = GetValueAtWeight(randomValue, copiedKeys);

                result.Add(randomKey);

                copiedKeys.Remove(randomKey);
                totalWeight -= GetValue(randomKey);
            }

            return result;
        }

        public WeightedList<TKey> GetCopy()
        {
            WeightedList<TKey> copy = new();
            foreach (TKey key in Keys)
            {
                copy.TryAdd(key, GetValue(key));
            }

            return copy;
        }

        public WeightedList<TKey> GetCopyAndCombine(WeightedList<TKey> other)
        {
            WeightedList<TKey> result = GetCopy();

            foreach (TKey key in other.Keys)
            {
                if (result.ContainsKey(key))
                {
                    result.TryUpdate(key, result.GetValue(key) + other.GetValue(key));
                }
                else
                {
                    result.TryAdd(key, other.GetValue(key));
                }
            }

            return result;
        }
    }
}
