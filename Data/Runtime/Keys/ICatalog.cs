using System.Collections.Generic;

namespace CupkekGames.Data
{
    /// <summary>
    /// Lists keys for a catalog registered in <see cref="CupkekGames.Systems.ServiceLocator"/> under <see cref="CatalogId"/>.
    /// </summary>
    public interface ICatalog
    {
        string CatalogId { get; }
        IEnumerable<string> GetKeys();
    }
}
