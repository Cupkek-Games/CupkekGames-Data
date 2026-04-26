namespace CupkekGames.Data
{
    public interface IDataSerializer
    {
        string Serialize<T>(T data);
        T Deserialize<T>(string json);
        void Populate<T>(string json, T target);
        T Clone<T>(T source);
    }
}
