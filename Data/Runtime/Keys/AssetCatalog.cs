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

        public T GetValue(string key) => _database.GetValue(key);

        Object IAssetCatalog.GetValue(string key) => GetValue(key);

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
