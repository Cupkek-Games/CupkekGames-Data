using System;
using CupkekGames.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace CupkekGames.Data.DropTable
{
    [Serializable]
    public class DropEntry
    {
        public DropEntry() { }

        public CatalogKey Key;

        [Range(0f, 1f)]
        public float Chance = 0.5f;

        [Min(1)]
        public int MinAmount = 1;

        [Min(1)]
        public int MaxAmount = 1;

        public DropEntry(DropEntry other)
        {
            if (other == null)
                return;
            Key = other.Key;
            Chance = other.Chance;
            MinAmount = other.MinAmount;
            MaxAmount = other.MaxAmount;
        }
    }
}
