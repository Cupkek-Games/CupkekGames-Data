namespace CupkekGames.Data
{
    /// <summary>
    /// Mutable runtime state paired with static <see cref="IFeature"/> definitions on data models.
    /// Store on runtime entities (e.g. inventory stacks); each implementation controls field serialization.
    /// </summary>
    public interface IFeatureStateData
    {
        /// <summary>Deep copy when cloning the owning runtime entity (e.g. stack split).</summary>
        IFeatureStateData CloneState();
    }
}
