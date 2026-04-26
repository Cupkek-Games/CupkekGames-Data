using System;
using CupkekGames.Systems;
using UnityEngine;

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
        }

        public override void Initialize()
        {
            ApplyDefaultToActualForReset();
            SetPlaySessionApplied();
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
            if (toDefault)
                _defaultData = data;
            else
            {
                _actualData = data;
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
            if (Application.isPlaying)
                SetPlaySessionApplied();
        }
    }
}
