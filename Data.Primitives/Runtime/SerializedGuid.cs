using System;
using UnityEngine;

namespace CupkekGames.Data.Primitives
{
    [Serializable]
    public struct SerializedGuid
    {
        public string ValueStr;

        public readonly Guid Value()
        {
            if (string.IsNullOrEmpty(ValueStr))
            {
                return Guid.Empty;
            }

            return new Guid(ValueStr);
        }

        public SerializedGuid(string value = "")
        {
            if (Guid.TryParse(value, out Guid result))
            {
                ValueStr = value;
            }
            else
            {
                ValueStr = Guid.Empty.ToString();
            }
        }

        public SerializedGuid(Guid value)
        {
            ValueStr = value.ToString();
        }

        public static SerializedGuid NewGUID()
        {
            return new SerializedGuid(Guid.NewGuid().ToString());
        }

        public static implicit operator string(SerializedGuid guid) => guid.ValueStr;
        public static implicit operator SerializedGuid(string value) => new SerializedGuid(value);
        public static implicit operator Guid(SerializedGuid guid) => guid.Value();
        public static implicit operator SerializedGuid(Guid value) => new SerializedGuid(value);

        public override readonly bool Equals(object obj)
        {
            return obj switch
            {
                SerializedGuid other => Value() == other.Value(),
                Guid guid => Value() == guid,
                _ => false
            };
        }

        public override readonly int GetHashCode() => Value().GetHashCode();

        public static bool operator ==(SerializedGuid left, SerializedGuid right) => left.Value() == right.Value();
        public static bool operator !=(SerializedGuid left, SerializedGuid right) => left.Value() != right.Value();
        public static bool operator ==(SerializedGuid left, Guid right) => left.Value() == right;
        public static bool operator !=(SerializedGuid left, Guid right) => left.Value() != right;
        public static bool operator ==(Guid left, SerializedGuid right) => left == right.Value();
        public static bool operator !=(Guid left, SerializedGuid right) => left != right.Value();
    }
}
