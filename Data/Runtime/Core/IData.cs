namespace CupkekGames.Data
{
    public interface IData
    {
        bool Validate();
        void OnAfterDeserialize();

        /// <summary>
        /// Deep copy for isolating runtime <c>actual</c> from <c>default</c> (e.g. play reset) without sharing
        /// mutable reference-type fields. Must not depend on registered services (no service locator).
        /// </summary>
        IData CloneData();
    }
}
