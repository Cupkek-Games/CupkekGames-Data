using System.Collections.Generic;
using System.Globalization;
using CupkekGames.Core;
using CupkekGames.Systems;
using UnityEngine;

namespace CupkekGames.Data
{
    [CreateAssetMenu(fileName = "FloatCatalog", menuName = "CupkekGames/Data/Catalog/Value/Float")]
    public class FloatCatalog : ServiceProviderSO, ICatalog, IValueCatalog<float>
    {
        [SerializeField]
        private string _catalogId;

        [SerializeField]
        private KeyValueDatabase<string, float> _database = new();

        public string CatalogId => _catalogId;
        public KeyValueDatabase<string, float> Database => _database;

        public IEnumerable<string> GetKeys() => _database.Keys;

        public float GetValue(string key) => _database.GetValue(key);

        public string GetDisplayValue(string key) => GetValue(key).ToString(CultureInfo.InvariantCulture);

        public override void RegisterServices()
        {
            ServiceLocator.Register(this, typeof(ICatalog), _catalogId, append: true);
            ServiceLocator.Register(this, typeof(IValueCatalog), _catalogId, append: true);
            ServiceLocator.Register(this, typeof(IValueCatalog<float>), _catalogId, append: true);
        }

        public override void UnregisterServices()
        {
            ServiceLocator.RemoveInstance(this, typeof(ICatalog));
            ServiceLocator.RemoveInstance(this, typeof(IValueCatalog));
            ServiceLocator.RemoveInstance(this, typeof(IValueCatalog<float>));
        }
    }
}
