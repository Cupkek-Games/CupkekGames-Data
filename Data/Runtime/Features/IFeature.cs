namespace CupkekGames.Data
{
    public interface IFeature
    {
        /// <summary>Deep copy for <see cref="IData.CloneData"/> on owning data types (e.g. item definitions).</summary>
        IFeature CloneFeature();
    }
}
