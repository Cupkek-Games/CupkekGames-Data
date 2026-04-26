using System;
using UnityEngine;

namespace CupkekGames.Data
{
    [Serializable]
    public class SerializableReference<T> where T : class
    {
        [SerializeField] private ReferenceMode _mode = ReferenceMode.Inline;
        [SerializeField] private UnityEngine.Object _assetReference;
        [SerializeReference] private T _inlineValue;

        public T Value
        {
            get => _mode switch
            {
                ReferenceMode.Asset => _assetReference as T,
                ReferenceMode.Inline => _inlineValue,
                _ => null
            };
            set
            {
                if (value is UnityEngine.Object obj)
                {
                    _mode = ReferenceMode.Asset;
                    _assetReference = obj;
                    _inlineValue = null;
                }
                else
                {
                    _mode = ReferenceMode.Inline;
                    _inlineValue = value;
                    _assetReference = null;
                }
            }
        }

        public bool HasValue => _mode switch
        {
            ReferenceMode.Asset => _assetReference != null,
            ReferenceMode.Inline => _inlineValue != null,
            _ => false
        };

        public ReferenceMode Mode => _mode;
    }
}
