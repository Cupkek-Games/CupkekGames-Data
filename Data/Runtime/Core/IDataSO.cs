namespace CupkekGames.Data
{
    public interface IDataSO
    {
        IData Data { get; }
        void Initialize();
        void LoadFromJson(string json, bool toDefault = false);
        string ToJson(bool useDefault = false);
        void ResetToDefault();
    }
}
