using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CupkekGames.Core.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CupkekGames.Data.Editor
{
    [CustomPropertyDrawer(typeof(IFeature), true)]
    public class FeatureDrawer : PropertyDrawer
    {
        // Cache per element type (e.g. IFeature, IItemFeature, IUnitFeatureDefinition)
        private static readonly Dictionary<Type, Type[]> _typesByConstraint = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float sp = EditorGUIUtility.standardVerticalSpacing;
            return line + sp + EditorImGuiDrawing.GetChildPropertiesHeight(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Type[] types = GetFilteredTypes();
            EditorGUI.BeginProperty(position, label, property);
            float line = EditorGUIUtility.singleLineHeight;
            float sp = EditorGUIUtility.standardVerticalSpacing;
            string typeName = property.managedReferenceValue?.GetType().Name ?? "(null)";
            Rect btnRect = new Rect(position.x, position.y, position.width, line);
            if (EditorGUI.DropdownButton(btnRect, new GUIContent($"{label.text}: {typeName}", "Select feature type"),
                    FocusType.Keyboard))
            {
                var items = BuildDropdownItems(types, property);
                var screenRect = new Rect(
                    GUIUtility.GUIToScreenPoint(new Vector2(btnRect.x, btnRect.yMax)),
                    new Vector2(btnRect.width, 1));

                SearchableDropdown.Show(screenRect, items, selectedKey =>
                {
                    var type = Type.GetType(selectedKey);
                    if (type == null) return;
                    property.serializedObject.Update();
                    property.managedReferenceValue = Activator.CreateInstance(type);
                    property.serializedObject.ApplyModifiedProperties();
                }, property.managedReferenceValue?.GetType().AssemblyQualifiedName);
            }

            float y = position.y + line + sp;
            EditorGUI.indentLevel++;
            EditorImGuiDrawing.DrawChildProperties(position, property, ref y);
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Type[] types = GetFilteredTypes();

            var container = new VisualElement();
            string currentTypeName = property.managedReferenceValue?.GetType().Name ?? "(null)";
            var childContainer = new VisualElement();
            childContainer.style.paddingLeft = 15;

            var typeButton = new VisualElement();
            typeButton.style.flexDirection = FlexDirection.Row;
            typeButton.style.alignItems = Align.Center;
            typeButton.style.borderTopWidth = 1;
            typeButton.style.borderBottomWidth = 1;
            typeButton.style.borderLeftWidth = 1;
            typeButton.style.borderRightWidth = 1;
            typeButton.style.borderTopColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            typeButton.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            typeButton.style.borderLeftColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            typeButton.style.borderRightColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            typeButton.style.borderTopLeftRadius = 3;
            typeButton.style.borderTopRightRadius = 3;
            typeButton.style.borderBottomLeftRadius = 3;
            typeButton.style.borderBottomRightRadius = 3;
            typeButton.style.paddingTop = 2;
            typeButton.style.paddingBottom = 2;
            typeButton.style.paddingLeft = 6;
            typeButton.style.paddingRight = 4;
            typeButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

            var typeLabel = new Label(currentTypeName);
            typeLabel.style.flexGrow = 1;
            typeLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            typeButton.Add(typeLabel);

            var arrow = new Label("\u25BC");
            arrow.style.fontSize = 8;
            arrow.style.unityTextAlign = TextAnchor.MiddleCenter;
            arrow.style.width = 14;
            arrow.style.color = new Color(0.7f, 0.7f, 0.7f);
            typeButton.Add(arrow);

            typeButton.RegisterCallback<ClickEvent>(_ =>
            {
                var items = BuildDropdownItems(types, property);
                SearchableDropdown.Show(typeButton, items, selectedKey =>
                {
                    var type = Type.GetType(selectedKey);
                    if (type == null) return;
                    property.serializedObject.Update();
                    property.managedReferenceValue = Activator.CreateInstance(type);
                    property.serializedObject.ApplyModifiedProperties();
                    typeLabel.text = type.Name;
                    RebuildChildren(childContainer, property);
                }, property.managedReferenceValue?.GetType().AssemblyQualifiedName);
            });

            container.Add(typeButton);
            container.Add(childContainer);
            RebuildChildren(childContainer, property);
            return container;
        }

        /// <summary>
        /// Gets the filtered types for this drawer instance based on the declaring field's
        /// list element type. E.g. List&lt;IItemFeature&gt; only shows IItemFeature types.
        /// </summary>
        private Type[] GetFilteredTypes()
        {
            Type constraint = ResolveElementType();
            return GetOrBuildCache(constraint);
        }

        /// <summary>
        /// Resolves the element type from the declaring field.
        /// For List&lt;IItemFeature&gt;, returns IItemFeature.
        /// For List&lt;IFeature&gt; or non-list fields, returns IFeature.
        /// </summary>
        private Type ResolveElementType()
        {
            if (fieldInfo == null) return typeof(IFeature);

            Type fieldType = fieldInfo.FieldType;

            // Check if it's a generic list
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = fieldType.GetGenericArguments()[0];
                if (typeof(IFeature).IsAssignableFrom(elementType))
                    return elementType;
            }

            return typeof(IFeature);
        }

        private static Type[] GetOrBuildCache(Type constraint)
        {
            if (_typesByConstraint.TryGetValue(constraint, out Type[] cached))
                return cached;

            var types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface) continue;
                        if (!constraint.IsAssignableFrom(type)) continue;
                        types.Add(type);
                    }
                }
                catch (ReflectionTypeLoadException) { }
            }

            types.Sort((a, b) =>
            {
                var groupA = a.GetCustomAttribute<FeatureGroupAttribute>()?.Group ?? "";
                var groupB = b.GetCustomAttribute<FeatureGroupAttribute>()?.Group ?? "";
                int cmp = string.Compare(groupA, groupB, StringComparison.Ordinal);
                return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });

            var result = types.ToArray();
            _typesByConstraint[constraint] = result;
            return result;
        }

        private static List<SearchableDropdown.DropdownItem> BuildDropdownItems(Type[] types, SerializedProperty property)
        {
            var items = new List<SearchableDropdown.DropdownItem>();
            for (int i = 0; i < types.Length; i++)
            {
                var group = types[i].GetCustomAttribute<FeatureGroupAttribute>()?.Group ?? "";
                items.Add(new SearchableDropdown.DropdownItem
                {
                    Key = types[i].AssemblyQualifiedName,
                    DisplayName = types[i].Name,
                    Group = group
                });
            }
            return items;
        }

        private static void RebuildChildren(VisualElement childContainer, SerializedProperty property)
        {
            childContainer.Clear();
            if (property.managedReferenceValue == null) return;

            var iter = property.Copy();
            var end = property.GetEndProperty();
            bool enterChildren = true;

            while (iter.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iter, end))
            {
                enterChildren = false;
                childContainer.Add(new PropertyField(iter));
            }

            childContainer.Bind(property.serializedObject);
        }
    }
}
