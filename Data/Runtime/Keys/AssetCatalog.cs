using CupkekGames.AssetFinder;
using CupkekGames.KeyValueDatabases;
using System.Collections.Generic;
using CupkekGames.Services;
using UnityEngine;
using UnityEngine.Serialization;

namespace CupkekGames.Data
{
    public class AssetCatalog<T> : ServiceProviderSO, ICatalog, IAssetCatalog, IAssetCatalog<T> where T : Object
    {
        [SerializeField]
        private string _catalogId;

        [AssetFinder(typeof(Object))]
        [SerializeField]
        private KeyValueDatabase<string, T> _database = new();

        public string CatalogId => _catalogId;
        public KeyValueDatabase<string, T> Database => _database;

        public IEnumerable<string> GetKeys() => _database.Keys;

        /// <summary>
        /// Resolve <paramref name="key"/>, expecting a hit. A miss returns null and logs
        /// a warning in the editor / development builds (typo'd key or deleted asset are
        /// otherwise indistinguishable at the call site). Null/empty keys return null
        /// silently — an unset <see cref="CatalogKey"/> is not an error. Use
        /// <see cref="TryGetValue"/> to probe without the warning.
        /// </summary>
        public T GetValue(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_database.TryGetValue(key, out T value)) return value;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                $"[AssetCatalog] '{_catalogId}' has no entry for key '{key}'. " +
                "Typo'd key or deleted asset? Use TryGetValue to probe without this warning.", this);
#endif
            return null;
        }

        /// <summary>Silent probe — no miss warning. Use in multi-catalog lookup loops.</summary>
        public bool TryGetValue(string key, out T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = null;
                return false;
            }
            return _database.TryGetValue(key, out value);
        }

        public bool ContainsKey(string key) => !string.IsNullOrEmpty(key) && _database.ContainsKey(key);

        Object IAssetCatalog.GetValue(string key) => GetValue(key);

        bool IAssetCatalog.TryGetValue(string key, out Object value)
        {
            bool found = TryGetValue(key, out T typed);
            value = typed;
            return found;
        }

        public override void RegisterServices()
        {
            ServiceLocator.Register(this, typeof(ICatalog), _catalogId, append: true);
            ServiceLocator.Register(this, typeof(IAssetCatalog), _catalogId, append: true);
            ServiceLocator.Register(this, typeof(IAssetCatalog<T>), _catalogId, append: true);
        }

        public override void UnregisterServices()
        {
            ServiceLocator.RemoveInstance(this, typeof(ICatalog));
            ServiceLocator.RemoveInstance(this, typeof(IAssetCatalog));
            ServiceLocator.RemoveInstance(this, typeof(IAssetCatalog<T>));
        }
    }
}
