using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CupkekGames.Data.Editor
{
    [CustomPropertyDrawer(typeof(SerializableReference<>), true)]
    public class SerializableReferenceDrawer : PropertyDrawer
    {
        private Type GetGenericArgument()
        {
            var type = fieldInfo.FieldType;
            if (type.IsGenericType)
                return type.GetGenericArguments()[0];
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SerializableReference<>))
                    return type.GetGenericArguments()[0];
                type = type.BaseType;
            }
            return typeof(UnityEngine.Object);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var modeProp = property.FindPropertyRelative("_mode");
            var assetProp = property.FindPropertyRelative("_assetReference");
            var inlineProp = property.FindPropertyRelative("_inlineValue");
            float line = EditorGUIUtility.singleLineHeight;
            float sp = EditorGUIUtility.standardVerticalSpacing;
            if (modeProp == null || assetProp == null || inlineProp == null)
                return line;
            float h = line + sp;
            h += modeProp.enumValueIndex == 0
                ? EditorGUI.GetPropertyHeight(inlineProp, true)
                : EditorGUI.GetPropertyHeight(assetProp, true);
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var modeProp = property.FindPropertyRelative("_mode");
            var assetProp = property.FindPropertyRelative("_assetReference");
            var inlineProp = property.FindPropertyRelative("_inlineValue");
            if (modeProp == null || assetProp == null || inlineProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Malformed SerializableReference");
                return;
            }

            float line = EditorGUIUtility.singleLineHeight;
            float sp = EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.BeginProperty(position, label, property);

            Rect header = new Rect(position.x, position.y, position.width, line);
            Rect labelRect = new Rect(header.x, header.y, EditorGUIUtility.labelWidth, line);
            Rect modeRect = new Rect(labelRect.xMax, header.y, Mathf.Max(60f, header.width - labelRect.width), line);
            EditorGUI.LabelField(labelRect, label);
            EditorGUI.BeginChangeCheck();
            int mode = EditorGUI.Popup(modeRect, modeProp.enumValueIndex, new[] { "Inline", "Asset" });
            if (EditorGUI.EndChangeCheck())
            {
                modeProp.enumValueIndex = mode;
                modeProp.serializedObject.ApplyModifiedProperties();
            }

            bool isInline = modeProp.enumValueIndex == 0;
            float y = header.yMax + sp;
            if (isInline)
            {
                float ih = EditorGUI.GetPropertyHeight(inlineProp, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, ih), inlineProp, true);
            }
            else
            {
                float ah = EditorGUI.GetPropertyHeight(assetProp, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, ah), assetProp, GUIContent.none, true);
            }

            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var modeProp = property.FindPropertyRelative("_mode");
            var assetProp = property.FindPropertyRelative("_assetReference");
            var inlineProp = property.FindPropertyRelative("_inlineValue");

            var container = new VisualElement();

            // Header row: label + Inline/Asset toggle
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 2;

            var headerLabel = new Label(property.displayName);
            headerLabel.style.flexGrow = 1;
            headerLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            headerLabel.style.minWidth = 120;
            headerRow.Add(headerLabel);

            var inlineBtn = new Button { text = "Inline" };
            var assetBtn = new Button { text = "Asset" };
            inlineBtn.style.flexGrow = 1;
            assetBtn.style.flexGrow = 1;
            inlineBtn.style.borderTopRightRadius = 0;
            inlineBtn.style.borderBottomRightRadius = 0;
            inlineBtn.style.marginRight = 0;
            assetBtn.style.borderTopLeftRadius = 0;
            assetBtn.style.borderBottomLeftRadius = 0;
            assetBtn.style.marginLeft = 0;

            var activeBg = new Color(0.25f, 0.5f, 0.8f, 0.7f);
            var inactiveBg = new Color(0.3f, 0.3f, 0.3f, 0.3f);

            void UpdateToggleStyle()
            {
                bool isInline = modeProp.enumValueIndex == 0;
                inlineBtn.style.backgroundColor = isInline ? activeBg : inactiveBg;
                assetBtn.style.backgroundColor = isInline ? inactiveBg : activeBg;
                inlineBtn.style.unityFontStyleAndWeight = isInline ? FontStyle.Bold : FontStyle.Normal;
                assetBtn.style.unityFontStyleAndWeight = isInline ? FontStyle.Normal : FontStyle.Bold;
            }

            inlineBtn.clicked += () =>
            {
                modeProp.enumValueIndex = 0;
                modeProp.serializedObject.ApplyModifiedProperties();
                UpdateToggleStyle();
            };
            assetBtn.clicked += () =>
            {
                modeProp.enumValueIndex = 1;
                modeProp.serializedObject.ApplyModifiedProperties();
                UpdateToggleStyle();
            };

            var toggleGroup = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            toggleGroup.Add(inlineBtn);
            toggleGroup.Add(assetBtn);
            headerRow.Add(toggleGroup);

            UpdateToggleStyle();
            container.Add(headerRow);

            var valueContainer = new VisualElement();
            valueContainer.style.paddingLeft = 15;
            container.Add(valueContainer);

            void RebuildValue()
            {
                valueContainer.Clear();

                if (modeProp.enumValueIndex == 0) // Inline
                {
                    valueContainer.Add(new PropertyField(inlineProp));
                }
                else // Asset
                {
                    var genericArg = GetGenericArgument();
                    var filterType = typeof(UnityEngine.Object).IsAssignableFrom(genericArg)
                        ? genericArg
                        : typeof(UnityEngine.Object);

                    var objectField = new ObjectField("Asset")
                    {
                        objectType = filterType,
                        allowSceneObjects = false
                    };
                    objectField.BindProperty(assetProp);
                    valueContainer.Add(objectField);
                }

                valueContainer.Bind(property.serializedObject);
            }

            RebuildValue();
            container.TrackPropertyValue(modeProp, _ =>
            {
                UpdateToggleStyle();
                RebuildValue();
            });

            return container;
        }
    }
}
