using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace CupkekGames.Data
{
    /// <summary>
    /// Per-editor-play / per-player-launch session id. Uses <see cref="RuntimeInitializeLoadType.BeforeSceneLoad"/>
    /// so it still bumps when Enter Play Mode disables Domain Reload (same idea as SequencerSessionState).
    /// </summary>
    internal static partial class DataSOPlaySession
    {
        [NoAutoStaticsCleanup]
        internal static int Id;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BumpPlaySessionId() => Id++;
    }
}
