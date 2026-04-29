#if UNITY_EDITOR
using CupkekGames.EditorUI;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CupkekGames.Data.Primitives.Editor
{
    [CustomPropertyDrawer(typeof(SerializedGuid))]
    public class SerializedGuidDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 22f;
        private const float ButtonGap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty guidProperty = property.FindPropertyRelative("ValueStr");
            if (guidProperty == null)
            {
                EditorGUI.LabelField(position, label.text, "Malformed SerializedGuid");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            Rect row = EditorGUI.PrefixLabel(position, label);
            float buttonsWidth = 3f * ButtonWidth + 2f * ButtonGap;
            Rect textRect = new Rect(row.x, row.y, Mathf.Max(0f, row.width - buttonsWidth - ButtonGap), row.height);
            float bx = textRect.xMax + ButtonGap;

            EditorGUI.PropertyField(textRect, guidProperty, GUIContent.none);

            Rect r = new Rect(bx, row.y, ButtonWidth, row.height);
            if (GUI.Button(r, new GUIContent("G", "Generate new GUID")))
            {
                guidProperty.stringValue = Guid.NewGuid().ToString();
                guidProperty.serializedObject.ApplyModifiedProperties();
            }

            r.x += ButtonWidth + ButtonGap;
            if (GUI.Button(r, new GUIContent("C", "Copy to clipboard")))
                EditorGUIUtility.systemCopyBuffer = guidProperty.stringValue;

            r.x += ButtonWidth + ButtonGap;
            if (GUI.Button(r, new GUIContent("R", "Reset to empty GUID")))
            {
                guidProperty.stringValue = Guid.Empty.ToString();
                guidProperty.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var guidProperty = property.FindPropertyRelative("ValueStr");

            var (container, label, inputRow) = EditorUIElements.CreatePropertyRow(property.displayName);

            var textField = new TextField
            {
                bindingPath = guidProperty.propertyPath
            };
            textField.style.flexGrow = 1;
            textField.style.flexShrink = 1;
            textField.style.minWidth = 0;
            inputRow.Add(textField);

            var generateButton = new Button(() =>
            {
                guidProperty.stringValue = Guid.NewGuid().ToString();
                guidProperty.serializedObject.ApplyModifiedProperties();
            })
            {
                text = "G",
                tooltip = "Generate new GUID",
                style = { width = 24, flexShrink = 0, marginLeft = 1, marginRight = 1 }
            };
            inputRow.Add(generateButton);

            var copyButton = new Button(() =>
            {
                EditorGUIUtility.systemCopyBuffer = guidProperty.stringValue;
            })
            {
                text = "C",
                tooltip = "Copy to clipboard",
                style = { width = 24, flexShrink = 0, marginLeft = 1, marginRight = 1 }
            };
            inputRow.Add(copyButton);

            var resetButton = new Button(() =>
            {
                guidProperty.stringValue = Guid.Empty.ToString();
                guidProperty.serializedObject.ApplyModifiedProperties();
            })
            {
                text = "R",
                tooltip = "Reset to empty GUID",
                style = { width = 24, flexShrink = 0, marginLeft = 1, marginRight = 1 }
            };
            inputRow.Add(resetButton);

            return container;
        }
    }
}
#endif
