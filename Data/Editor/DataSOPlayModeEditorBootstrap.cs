using UnityEditor;
using UnityEngine;

namespace CupkekGames.Data.Editor
{
    [InitializeOnLoad]
    internal static class DataSOPlayModeEditorBootstrap
    {
        static DataSOPlayModeEditorBootstrap()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            // Application.isPlaying is still false; OnEnable on SOs often runs before isPlaying flips — reset here so
            // Inspector and serialized actual match default before Awake. Session id + Initialize() run via OnEnable /
            // editor-only AfterSceneLoad batch (DataSOPlayModeRuntime). Uses CloneData (no ServiceLocator).
            ApplyDefaultToAllDataSOsAndDirty();
        }

        private static DataSO[] FindAllDataSOsInMemory() => Resources.FindObjectsOfTypeAll<DataSO>();

        private static void ApplyDefaultToAllDataSOsAndDirty()
        {
            DataSO[] all = FindAllDataSOsInMemory();
            for (int i = 0; i < all.Length; i++)
            {
                DataSO so = all[i];
                if (so == null)
                    continue;
                if (!EditorUtility.IsPersistent(so))
                    continue;
                so.ApplyDefaultToActualForReset();
                EditorUtility.SetDirty(so);
            }
        }
    }
}
