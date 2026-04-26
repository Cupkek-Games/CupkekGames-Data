namespace CupkekGames.Data
{
    public interface IAssetResolver
    {
        T Resolve<T>(string key) where T : UnityEngine.Object;
        bool CanResolve<T>(string key) where T : UnityEngine.Object;
    }
}
