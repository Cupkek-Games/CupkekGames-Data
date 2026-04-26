#if UNITY_EDITOR
using UnityEngine;

namespace CupkekGames.Data
{
    /// <summary>
    /// After first scene load, catch any <see cref="DataSO"/> that did not get a timely <see cref="DataSO.OnEnable"/>
    /// with <c>isPlaying</c> true (ordering varies). Editor only — player builds use <see cref="DataSO.OnEnable"/> +
    /// <see cref="DataSO.EnsurePlaySessionInitialized"/> when each asset loads.
    /// </summary>
    internal static class DataSOPlayModeRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyLoadedDataSOs()
        {
            DataSO[] all = Resources.FindObjectsOfTypeAll<DataSO>();
            for (int i = 0; i < all.Length; i++)
            {
                DataSO so = all[i];
                if (so != null)
                    so.EnsurePlaySessionInitialized();
            }
        }
    }
}
#endif
