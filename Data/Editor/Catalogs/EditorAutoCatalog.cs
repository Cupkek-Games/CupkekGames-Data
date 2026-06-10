#if UNITY_EDITOR
using System.Collections.Generic;
using CupkekGames.Data;
using CupkekGames.Services;
using UnityEngine;

namespace CupkekGames.Data.Editor
{
    /// <summary>
    /// Editor-only catalog whose keys/values are <b>derived by scanning the project</b> — no authored
    /// asset, no <c>ServiceRegistrySO</c>, no manual key list. Subclass it, supply an <see cref="Id"/>
    /// and a <see cref="Build"/> projection, and register the instance from an <c>[InitializeOnLoad]</c>
    /// static constructor via <see cref="Register"/>:
    /// <code>
    /// [InitializeOnLoad]
    /// sealed class FooAutoCatalog : EditorAutoCatalog&lt;FooSO&gt;
    /// {
    ///     static FooAutoCatalog() => Register(new FooAutoCatalog());
    ///     protected override string Id => "Foo";
    ///     protected override IEnumerable&lt;KeyValuePair&lt;string, FooSO&gt;&gt; Build() => ...;
    /// }
    /// </code>
    ///
    /// <para>
    /// The base owns everything generic: registration into the <see cref="ServiceLocator"/> under
    /// <see cref="ICatalog"/> / <see cref="IAssetCatalog"/> / <see cref="IAssetCatalog{T}"/>, the editor
    /// play-mode lifecycle (re-registered on return to edit; never live during play — see
    /// <see cref="EditorAutoCatalogHost"/>), and AssetDatabase-change cache invalidation. It co-registers
    /// (<c>append</c>) with any real <see cref="AssetCatalog{T}"/> asset under the same id, so the two coexist.
    /// </para>
    /// </summary>
    public abstract class EditorAutoCatalog<T> : ICatalog, IAssetCatalog, IAssetCatalog<T>, IEditorAutoCatalog
        where T : Object
    {
        /// <summary>Catalog id this source registers under (the <c>[CatalogKeyConstraint]</c> id).</summary>
        protected abstract string Id { get; }

        /// <summary>
        /// Project scan → (key, value) pairs. Yield freely; duplicate keys resolve <b>first-wins</b>, and
        /// empty keys / null values are dropped. Run lazily and re-run after any asset change.
        /// </summary>
        protected abstract IEnumerable<KeyValuePair<string, T>> Build();

        Dictionary<string, T> _map;
        Dictionary<string, T> Map => _map ??= BuildMap();

        Dictionary<string, T> BuildMap()
        {
            var map = new Dictionary<string, T>(System.StringComparer.Ordinal);
            foreach (KeyValuePair<string, T> pair in Build())
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Value == null)
                    continue;
                if (!map.ContainsKey(pair.Key)) // first-wins
                    map[pair.Key] = pair.Value;
            }

            return map;
        }

        // ── ICatalog / IAssetCatalog ─────────────────────────────────
        public string CatalogId => Id;

        public IEnumerable<string> GetKeys() => Map.Keys;

        public T GetValue(string key) =>
            !string.IsNullOrEmpty(key) && Map.TryGetValue(key, out T value) ? value : null;

        Object IAssetCatalog.GetValue(string key) => GetValue(key);

        // ── Editor lifecycle seam (driven by EditorAutoCatalogHost) ──
        void IEditorAutoCatalog.InvalidateCache() => _map = null;

        void IEditorAutoCatalog.RegisterIntoLocator()
        {
            ServiceLocator.Remove(this); // drop any prior registration so re-registering can't stack duplicates
            ServiceLocator.Register(this, typeof(ICatalog), Id, append: true);
            ServiceLocator.Register(this, typeof(IAssetCatalog), Id, append: true);
            ServiceLocator.Register(this, typeof(IAssetCatalog<T>), Id, append: true);
        }

        /// <summary>
        /// Register an instance and hook it into the shared editor lifecycle. Call once from the
        /// subclass's <c>[InitializeOnLoad]</c> static constructor.
        /// </summary>
        protected static void Register(EditorAutoCatalog<T> instance) => EditorAutoCatalogHost.Add(instance);
    }

    /// <summary>
    /// Non-generic seam so <see cref="EditorAutoCatalogHost"/> can hold and refresh
    /// <see cref="EditorAutoCatalog{T}"/> instances of any <c>T</c> in one list.
    /// </summary>
    internal interface IEditorAutoCatalog
    {
        void RegisterIntoLocator();
        void InvalidateCache();
    }
}
#endif
