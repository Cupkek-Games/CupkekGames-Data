using System;
using UnityEngine;

namespace CupkekGames.Data
{
    /// <summary>
    /// Optional constraints for <see cref="CatalogKey"/> fields in the Inspector (catalog/key pickers).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CatalogKeyConstraintAttribute : PropertyAttribute
    {
        public string CatalogId { get; }
        public Type AssetType { get; }

        /// <summary>Lock to a single catalog id (hides catalog dropdown).</summary>
        public CatalogKeyConstraintAttribute(string catalogId)
        {
            CatalogId = catalogId ?? "";
        }

        /// <summary>Only show catalogs backed by <see cref="AssetCatalog{T}"/> whose <c>T</c> satisfies the constraint.</summary>
        public CatalogKeyConstraintAttribute(Type assetType)
        {
            AssetType = assetType;
        }

        /// <summary>Lock catalog id and require matching asset catalog type.</summary>
        public CatalogKeyConstraintAttribute(string catalogId, Type assetType)
        {
            CatalogId = catalogId ?? "";
            AssetType = assetType;
        }
    }
}
