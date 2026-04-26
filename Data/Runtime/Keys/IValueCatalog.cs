namespace CupkekGames.Data
{
    /// <summary>
    /// Non-generic catalog of values keyed by string; provides a display string for editors (e.g. <see cref="CatalogKey"/> drawer).
    /// </summary>
    public interface IValueCatalog : ICatalog
    {
        string GetDisplayValue(string key);
    }

    /// <summary>
    /// Typed value catalog; register alongside <see cref="IValueCatalog"/> for type-safe runtime resolution.
    /// </summary>
    public interface IValueCatalog<out T> : IValueCatalog
    {
        T GetValue(string key);
    }
}
