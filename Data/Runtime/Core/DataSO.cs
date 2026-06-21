using System;
using CupkekGames.Services;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
#endif

namespace CupkekGames.Data
{
    /// <summary>
    /// Non-generic base so play-mode bootstrap can <see cref="Object.FindObjectsByType{T}(FindObjectsSortMode)"/>.
    /// </summary>
    public abstract class DataSO : ScriptableObject
    {
        [NonSerialized] private int _appliedPlaySessionId = -1;

        /// <summary>
        /// Applies <see cref="IDataSO.Initialize"/> once per play session when <see cref="Application.isPlaying"/>.
        /// </summary>
        public void EnsurePlaySessionInitialized() => TryEnsurePlaySessionInitialized();

        /// <returns><c>true</c> if <see cref="Initialize"/> ran this call (useful for editor <c>SetDirty</c>).</returns>
        public bool TryEnsurePlaySessionInitialized()
        {
            if (!Application.isPlaying)
                return false;
            if (_appliedPlaySessionId == DataSOPlaySession.Id)
                return false;
            Initialize();
            return true;
        }

        /// <summary>Must invoke <see cref="SetPlaySessionApplied"/> when <see cref="Application.isPlaying"/> after mutating actual data.</summary>
        public abstract void Initialize();

        /// <summary>
        /// Copies <c>default</c> → <c>actual</c> via <see cref="IData.CloneData"/> (respects <c>_resetOnStart</c>).
        /// Does not update play-session id — use from editor before <see cref="Application.isPlaying"/> is true, or via <see cref="Initialize"/>.
        /// </summary>
        public abstract void ApplyDefaultToActualForReset();

        protected void SetPlaySessionApplied()
        {
            if (Application.isPlaying)
                _appliedPlaySessionId = DataSOPlaySession.Id;
        }

        protected virtual void OnEnable()
        {
            if (Application.isPlaying)
                EnsurePlaySessionInitialized();
        }
    }

    public abstract class DataSO<T> : DataSO, IDataSO where T : IData, new()
    {
        [SerializeField] private T _defaultData = new();
        [SerializeField] private bool _resetOnStart = true;
        [SerializeField] private T _actualData;

        public T Data
        {
            get => _actualData;
            set => _actualData = value;
        }

        IData IDataSO.Data => _actualData;

        private static IDataSerializer _serializer;
        private static IDataSerializer Serializer => _serializer ??= ServiceLocator.Get<IDataSerializer>();

        public override void ApplyDefaultToActualForReset()
        {
            if (_resetOnStart)
                _actualData = CloneFromDefault(_defaultData);
            else
                _actualData ??= CloneFromDefault(_defaultData);
            AssignDeterministicActualRefIds();
        }

        public override void Initialize()
        {
            ApplyDefaultToActualForReset();
            SetPlaySessionApplied();
            ValidateInDevBuilds(_actualData, "play-session initialize");
        }

        private static T CloneFromDefault(T source)
        {
            if (source == null)
                return new T();
            return (T)source.CloneData();
        }

        public void LoadFromJson(string json, bool toDefault = false)
        {
            var data = Serializer.Deserialize<T>(json);
            data.OnAfterDeserialize();
            ValidateInDevBuilds(data, "LoadFromJson");
            if (toDefault)
                _defaultData = data;
            else
            {
                _actualData = data;
                AssignDeterministicActualRefIds();
                if (Application.isPlaying)
                    SetPlaySessionApplied();
            }
        }

        public string ToJson(bool useDefault = false)
        {
            return Serializer.Serialize(useDefault ? _defaultData : (_actualData ?? _defaultData));
        }

        public void ResetToDefault()
        {
            _actualData = CloneFromDefault(_defaultData);
            AssignDeterministicActualRefIds();
            if (Application.isPlaying)
                SetPlaySessionApplied();
            ValidateInDevBuilds(_actualData, "ResetToDefault");
        }

        /// <summary>
        /// Auto-validate at the points where data becomes live (play-session
        /// initialize, JSON load, reset). Editor + development builds only —
        /// stripped from release builds. A failure warns with the asset name;
        /// it does not block, since <see cref="IData.Validate"/> is a health
        /// check, not a gate.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void ValidateInDevBuilds(T data, string context)
        {
            if (data == null) return;
            if (!data.Validate())
            {
                Debug.LogWarning($"[DataSO] '{name}' failed {typeof(T).Name}.Validate() after {context}.", this);
            }
        }

        /// <summary>
        /// Editor-only: pins deterministic managed-reference ids (1, 2, 3, … in a stable walk order) onto
        /// <c>_actualData</c>'s <c>[SerializeReference]</c> graph. <c>_actualData</c> is re-cloned every play
        /// session, so without this Unity mints fresh random ids each time and the asset's YAML churns on save
        /// (pure <c>rid</c> renumbering, identical data). Pinning makes the serialized output byte-stable — the
        /// asset only changes in VCS when the data actually changes — while keeping <c>_actualData</c>
        /// serialized and inspectable. Small ids never collide with <c>_defaultData</c>'s large auto-assigned
        /// ids, which are left untouched. Fail-safe: any error falls back to Unity's default id assignment.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void AssignDeterministicActualRefIds()
        {
#if UNITY_EDITOR
            try
            {
                if (_actualData == null)
                    return;
                long next = 1;
                var visited = new HashSet<object>(RefComparer.Instance);
                PinManagedRefs(this, _actualData, ref next, visited, 0);
            }
            catch
            {
                // Non-fatal: leave Unity's auto-assigned ids (pre-pin behavior). Never break the asset.
            }
#endif
        }

#if UNITY_EDITOR
        // Depth-first walk over the serialized graph, pinning every [SerializeReference] instance to the next
        // sequential id. Order need not match Unity's serialization order — only be deterministic, which it is
        // (stable reflection field order over an identically-shaped clone), so each ref gets the same id every clone.
        private static void PinManagedRefs(UnityEngine.Object owner, object node, ref long next, HashSet<object> visited, int depth)
        {
            if (node == null || depth > 12)
                return;

            for (Type cur = node.GetType(); cur != null && cur != typeof(object); cur = cur.BaseType)
            {
                foreach (FieldInfo field in cur.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!IsUnitySerializedField(field))
                        continue;

                    object value;
                    try { value = field.GetValue(node); }
                    catch { continue; }
                    if (value == null)
                        continue;

                    bool managed = Attribute.IsDefined(field, typeof(SerializeReference));

                    if (value is System.Collections.IList list && !(value is string))
                    {
                        foreach (object element in list)
                            HandleManagedRefMember(owner, element, managed, ref next, visited, depth);
                    }
                    else
                    {
                        HandleManagedRefMember(owner, value, managed, ref next, visited, depth);
                    }
                }
            }
        }

        private static void HandleManagedRefMember(UnityEngine.Object owner, object element, bool managed, ref long next, HashSet<object> visited, int depth)
        {
            if (element == null)
                return;

            Type t = element.GetType();
            if (t.IsPrimitive || t.IsEnum || element is string || element is decimal || element is UnityEngine.Object)
                return;

            if (managed)
            {
                if (!visited.Add(element))
                    return; // shared/cyclic reference: pin once
                UnityEngine.Serialization.ManagedReferenceUtility.SetManagedReferenceIdForObject(owner, element, next++);
                PinManagedRefs(owner, element, ref next, visited, depth + 1); // nested [SerializeReference] inside this ref
            }
            else if (IsNestedSerializable(t))
            {
                if (!visited.Add(element))
                    return;
                PinManagedRefs(owner, element, ref next, visited, depth + 1); // plain serializable: may hold [SerializeReference] fields
            }
        }

        private static bool IsUnitySerializedField(FieldInfo field)
        {
            if (field.IsStatic || field.IsInitOnly)
                return false;
            if (Attribute.IsDefined(field, typeof(NonSerializedAttribute)))
                return false;
            if (Attribute.IsDefined(field, typeof(SerializeReference)))
                return true;
            if (field.IsPublic)
                return true;
            return Attribute.IsDefined(field, typeof(SerializeField));
        }

        private static bool IsNestedSerializable(Type t)
        {
            if (t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal))
                return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
                return false;
            if (t.Namespace != null && (t.Namespace == "System" || t.Namespace.StartsWith("System.") || t.Namespace.StartsWith("UnityEngine")))
                return false;
            return Attribute.IsDefined(t, typeof(SerializableAttribute));
        }

        private sealed class RefComparer : IEqualityComparer<object>
        {
            public static readonly RefComparer Instance = new RefComparer();
            bool IEqualityComparer<object>.Equals(object a, object b) => ReferenceEquals(a, b);
            int IEqualityComparer<object>.GetHashCode(object o) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o);
        }
#endif
    }
}
