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

            // Application.isPlaying is still false; OnEnable on SOs often runs before isPlaying flips — reset
            // _actualData -> default here (in memory) so the Inspector / early Awake see default before play.
            // The reset rides into play via Unity's domain-reload backup; session id + Initialize() then run via
            // OnEnable / the editor AfterSceneLoad batch (DataSOPlayModeRuntime). Uses CloneData (no ServiceLocator).
            //
            // Deliberately does NOT SetDirty: marking the asset dirty persists the freshly re-cloned _actualData
            // (with new managed-reference rids) to disk, churning the asset in VCS on every Play. The on-disk
            // _actualData is irrelevant — it's reset from default at runtime regardless.
            ApplyDefaultToAllDataSOs();
        }

        private static DataSO[] FindAllDataSOsInMemory() => Resources.FindObjectsOfTypeAll<DataSO>();

        private static void ApplyDefaultToAllDataSOs()
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
            }
        }
    }
}
