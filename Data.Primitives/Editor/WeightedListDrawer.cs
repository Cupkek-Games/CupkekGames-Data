#if UNITY_EDITOR
using CupkekGames.Core.Editor;
using CupkekGames.Data.Primitives;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CupkekGames.Data.Primitives.Editor
{
    /// <summary>
    /// Unity does not reliably apply <see cref="KeyValueDatabaseDrawer"/> to derived generic types such as
    /// <see cref="WeightedList{TKey}"/> in IMGUI; this drawer forwards to the same UI as KeyValueDatabase.
    /// </summary>
    [CustomPropertyDrawer(typeof(WeightedList<>), true)]
    public class WeightedListDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return KeyValueDatabaseDrawer.GetKeyValueDatabasePropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            KeyValueDatabaseDrawer.DrawKeyValueDatabaseProperty(position, property, label);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return KeyValueDatabaseDrawer.CreateKeyValueDatabaseVisualElement(property);
        }
    }
}
#endif
