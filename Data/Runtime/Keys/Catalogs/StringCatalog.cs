using CupkekGames.KeyValueDatabases;
using System.Collections.Generic;
using CupkekGames.Services;
using UnityEngine;

namespace CupkekGames.Data
{
    [CreateAssetMenu(fileName = "StringCatalog", menuName = "CupkekGames/Data/Catalog/Value/String")]
    public class StringCatalog : ServiceProviderSO, ICatalog, IValueCatalog<string>
    {
        [SerializeField]
        private string _catalogId;

        [SerializeField]
        private KeyValueDatabase<string, string> _database = new();

        public string CatalogId => _catalogId;
        public KeyValueDatabase<string, string> Database => _database;

        public IEnumerable<string> GetKeys() => _database.Keys;

        public string GetValue(string key) => _database.GetValue(key);

        public string GetDisplayValue(string key) => GetValue(key);

        public override void RegisterServices()
        {
            ServiceLocator.Register(this, typeof(ICatalog), _catalogId, append: true);
            ServiceLocator.Register(this, typeof(IValueCatalog), _catalogId, append: true);
            ServiceLocator.Register(this, typeof(IValueCatalog<string>), _catalogId, append: true);
        }

        public override void UnregisterServices()
        {
            ServiceLocator.RemoveInstance(this, typeof(ICatalog));
            ServiceLocator.RemoveInstance(this, typeof(IValueCatalog));
            ServiceLocator.RemoveInstance(this, typeof(IValueCatalog<string>));
        }
    }
}
