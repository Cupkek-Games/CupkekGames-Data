using System.Collections.Generic;

namespace CupkekGames.Data
{
    /// <summary>
    /// Lists keys for a catalog registered in <see cref="CupkekGames.Services"/> under <see cref="CatalogId"/>.
    /// </summary>
    public interface ICatalog
    {
        string CatalogId { get; }
        IEnumerable<string> GetKeys();
    }
}
