using CupkekGames.KeyValueDatabase;
using System.Collections.Generic;
using CupkekGames.Services;
using UnityEngine;

namespace CupkekGames.Data
{
    [CreateAssetMenu(fileName = "BoolCatalog", menuName = "CupkekGames/Data/Catalog/Value/Bool")]
    public class BoolCatalog : ServiceProviderSO, ICatalog, IValueCatalog<bool>
    {
        [SerializeField]
        private string _catalogId;

        [SerializeField]
        private KeyValueDatabase<string, bool> _database = new();

        public string CatalogId => _catalogId;
        public KeyValueDatabase<string, bool> Database => _database;

        public IEnumerable<string> GetKeys() => _database.Keys;

        public bool GetValue(string key) => _database.GetValue(key);

        public string GetDisplayValue(string key) => GetValue(key).ToString();

        public override void RegisterServices()
        {
            ServiceLocator.Register(this, typeof(ICatalog), _catalogId, append: true);
            ServiceLocator.Register(this, typeof(IValueCatalog), _catalogId, append: true);
            ServiceLocator.Register(this, typeof(IValueCatalog<bool>), _catalogId, append: true);
        }

        public override void UnregisterServices()
        {
            ServiceLocator.RemoveInstance(this, typeof(ICatalog));
            ServiceLocator.RemoveInstance(this, typeof(IValueCatalog));
            ServiceLocator.RemoveInstance(this, typeof(IValueCatalog<bool>));
        }
    }
}
