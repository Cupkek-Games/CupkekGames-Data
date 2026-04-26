using System;
using System.Collections.Generic;
using CupkekGames.Systems;
using UnityEngine;
using UnityEngine.Serialization;

namespace CupkekGames.Data
{
    [CreateAssetMenu(fileName = "KeyCatalog", menuName = "CupkekGames/Data/Catalog/Keys/Keys Only")]
    public class KeyCatalog : ServiceProviderSO, ICatalog
    {
        [SerializeField]
        private string _catalogId;

        [SerializeField]
        private string[] _keys = Array.Empty<string>();

        public string CatalogId => _catalogId;

        public IEnumerable<string> GetKeys() => _keys;

        public override void RegisterServices()
        {
            ServiceLocator.Register(this, typeof(ICatalog), _catalogId, append: true);
        }

        public override void UnregisterServices()
        {
            ServiceLocator.RemoveInstance(this, typeof(ICatalog));
        }
    }
}
