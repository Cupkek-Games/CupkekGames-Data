using UnityEngine;

namespace CupkekGames.Data
{
    /// <summary>
    /// Resolves Unity assets (or other <see cref="Object"/> values) by string key for catalog <see cref="ICatalog.CatalogId"/>.
    /// </summary>
    public interface IAssetCatalog : ICatalog
    {
        /// <summary>
        /// Resolve <paramref name="key"/>, expecting a hit. Implementations may log a
        /// dev-build warning on a miss — use <see cref="TryGetValue"/> when a miss is
        /// expected (probing across multiple co-registered catalogs).
        /// </summary>
        Object GetValue(string key);

        /// <summary>Silent probe: true + value on hit, false on miss, never warns.</summary>
        bool TryGetValue(string key, out Object value);
    }

    /// <summary>
    /// Typed asset catalog; register alongside <see cref="IAssetCatalog"/> so callers can resolve without casting when <typeparamref name="T"/> matches stored values.
    /// </summary>
    public interface IAssetCatalog<out T> : IAssetCatalog where T : Object
    {
        new T GetValue(string key);
    }
}
