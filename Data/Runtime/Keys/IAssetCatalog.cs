using UnityEngine;

namespace CupkekGames.Data
{
    /// <summary>
    /// Resolves Unity assets (or other <see cref="Object"/> values) by string key for catalog <see cref="ICatalog.CatalogId"/>.
    /// </summary>
    public interface IAssetCatalog : ICatalog
    {
        Object GetValue(string key);
    }

    /// <summary>
    /// Typed asset catalog; register alongside <see cref="IAssetCatalog"/> so callers can resolve without casting when <typeparamref name="T"/> matches stored values.
    /// </summary>
    public interface IAssetCatalog<out T> : IAssetCatalog where T : Object
    {
        new T GetValue(string key);
    }
}
