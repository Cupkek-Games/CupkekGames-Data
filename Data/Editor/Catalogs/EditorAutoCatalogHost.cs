#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace CupkekGames.Data.Editor
{
    /// <summary>
    /// Single owner of the <see cref="EditorAutoCatalog{T}"/> lifecycle. Keeps the live instances,
    /// (re)registers them after <c>ServiceLocator.ClearAll()</c> on play-mode boundaries, and drops
    /// their caches on any asset change.
    ///
    /// <para>
    /// Editor-auto-catalogs must be <b>live only in edit mode</b>: at runtime their consumers resolve
    /// elsewhere, and a registration surviving into play would stack with runtime catalogs under the
    /// same id (exactly what the pre-play <c>ClearAll</c> exists to prevent). So entering play is a
    /// one-way clear (never re-added by us) and registration is guarded on
    /// <see cref="EditorApplication.isPlayingOrWillChangePlaymode"/>; we restore only on return to edit.
    /// A non-generic owner is required because each closed <c>EditorAutoCatalog&lt;T&gt;</c> has its own
    /// statics — this is the single shared registry, subscription, and asset watcher for all of them.
    /// </para>
    /// </summary>
    internal static class EditorAutoCatalogHost
    {
        static readonly List<IEditorAutoCatalog> Instances = new();

        // Static ctor runs on the first Add() — i.e. the first consumer's [InitializeOnLoad], once per
        // domain. ServiceRegistrySOEditor calls ServiceLocator.ClearAll() on both play boundaries; we
        // re-register on return to edit, deferred via delayCall so it lands AFTER that synchronous ClearAll.
        static EditorAutoCatalogHost()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += RegisterAll;
            };
        }

        public static void Add(IEditorAutoCatalog instance)
        {
            if (instance == null || Instances.Contains(instance))
                return;
            Instances.Add(instance);
            RegisterOne(instance);
        }

        static void RegisterAll()
        {
            for (int i = 0; i < Instances.Count; i++)
                RegisterOne(Instances[i]);
        }

        static void RegisterOne(IEditorAutoCatalog instance)
        {
            // Editor-time dropdown source only — never live during play.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            instance.RegisterIntoLocator();
        }

        internal static void InvalidateAll()
        {
            for (int i = 0; i < Instances.Count; i++)
                Instances[i].InvalidateCache();
        }
    }

    /// <summary>
    /// Drops every <see cref="EditorAutoCatalog{T}"/> cache when project assets change, so a new /
    /// renamed / deleted key appears in the dropdowns without a domain reload.
    /// </summary>
    internal sealed class EditorAutoCatalogWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            => EditorAutoCatalogHost.InvalidateAll();
    }
}
#endif
