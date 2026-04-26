using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace CupkekGames.Data
{
    /// <summary>
    /// Serializable reference to an entry in an <see cref="IAssetCatalog"/> / <see cref="ICatalog"/> registered under <see cref="Catalog"/>.
    /// </summary>
    [Serializable]
    public struct CatalogKey : IEquatable<CatalogKey>
    {
        public string Catalog;
        public string Key;

        [IgnoreDataMember]
        public bool IsEmpty => string.IsNullOrEmpty(Key);

        public bool Equals(CatalogKey other) =>
            string.Equals(Catalog, other.Catalog, StringComparison.Ordinal) &&
            string.Equals(Key, other.Key, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is CatalogKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Catalog ?? "", Key ?? "");

        public override string ToString() => $"{Catalog}/{Key}";

        public static bool operator ==(CatalogKey a, CatalogKey b) => a.Equals(b);
        public static bool operator !=(CatalogKey a, CatalogKey b) => !a.Equals(b);
    }
}
