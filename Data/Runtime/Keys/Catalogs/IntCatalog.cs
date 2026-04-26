using System.Collections.Generic;
using System.Globalization;
using CupkekGames.Core;
using CupkekGames.Systems;
using UnityEngine;

namespace CupkekGames.Data
{
    [CreateAssetMenu(fileName = "IntCatalog", menuName = "CupkekGames/Data/Catalog/Value/Int")]
    public class IntCatalog : ServiceProviderSO, ICatalog, IValueCatalog<int>
    {
        [SerializeField]
        private string _catalogId;

        [SerializeField]
        private KeyValueDatabase<string, int> _database = new();

        public string CatalogId => _catalogId;
        public KeyValueDatabase<string, int> Database => _database;

        public IEnumerable<string> GetKeys() => _database.Keys;

        public int GetValue(string key) => _database.GetValue(key);

        public string GetDisplayValue(string key) => GetValue(key).ToString(CultureInfo.InvariantCulture);

        public override void RegisterServices()
        {
            ServiceLocator.Register(this, typeof(ICatalog), _catalogId, append: true);
            ServiceLocator.Register(this, typeof(IValueCatalog), _catalogId, append: true);
            ServiceLocator.Register(this, typeof(IValueCatalog<int>), _catalogId, append: true);
        }

        public override void UnregisterServices()
        {
            ServiceLocator.RemoveInstance(this, typeof(ICatalog));
            ServiceLocator.RemoveInstance(this, typeof(IValueCatalog));
            ServiceLocator.RemoveInstance(this, typeof(IValueCatalog<int>));
        }
    }
}
