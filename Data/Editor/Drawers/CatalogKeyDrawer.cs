using CupkekGames.EditorUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CupkekGames.Data;
using CupkekGames.Services;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CupkekGames.Data.Editor
{
    internal static class CatalogKeyHelper
    {
        public static List<string> GetRegisteredCatalogIds()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (!ServiceLocator.RegisteredServices.TryGetValue(typeof(ICatalog), out var map))
                return ids.OrderBy(x => x).ToList();

            foreach (string key in map.Keys)
            {
                if (!string.IsNullOrEmpty(key))
                    ids.Add(key);
            }

            return ids.OrderBy(x => x).ToList();
        }

        public static UnityEngine.Object ResolveValue(string catalogId, string key)
        {
            if (string.IsNullOrEmpty(catalogId) || string.IsNullOrEmpty(key))
                return null;
            IReadOnlyList<IAssetCatalog> assetCatalogs = ServiceLocator.GetAll<IAssetCatalog>(catalogId);
            for (int i = 0; i < assetCatalogs.Count; i++)
            {
                UnityEngine.Object value = assetCatalogs[i].GetValue(key);
                if (value != null)
                    return value;
            }

            return null;
        }

        public static bool HasAssetCatalog(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId))
                return false;
            return ServiceLocator.GetAll<IAssetCatalog>(catalogId).Count > 0;
        }

        public static bool HasValueCatalog(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId))
                return false;
            return ServiceLocator.GetAll<IValueCatalog>(catalogId).Count > 0;
        }

        public static bool HasAnyCatalog(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId))
                return false;
            return ServiceLocator.GetAll<ICatalog>(catalogId).Count > 0;
        }

        public static string ResolveDisplayValue(string catalogId, string key)
        {
            if (string.IsNullOrEmpty(catalogId) || string.IsNullOrEmpty(key))
                return null;
            IReadOnlyList<IValueCatalog> valueCatalogs = ServiceLocator.GetAll<IValueCatalog>(catalogId);
            for (int i = 0; i < valueCatalogs.Count; i++)
            {
                string value = valueCatalogs[i].GetDisplayValue(key);
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return null;
        }

        public static Texture2D ResolvePreview(string catalogId, string key)
        {
            if (string.IsNullOrEmpty(catalogId) || string.IsNullOrEmpty(key))
                return null;
            UnityEngine.Object value = ResolveValue(catalogId, key);
            if (value == null)
                return null;
            if (value is Sprite sprite)
                return SpritePreviewUtility.GetPreview(sprite);
            return AssetPreview.GetMiniThumbnail(value);
        }

        public static int ResolvePreviewSize(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId))
                return 20;
            IReadOnlyList<IAssetCatalog> assetCatalogs = ServiceLocator.GetAll<IAssetCatalog>(catalogId);
            for (int i = 0; i < assetCatalogs.Count; i++)
            {
                Type type = assetCatalogs[i].GetType();
                while (type != null)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AssetCatalog<>))
                    {
                        Type valueType = type.GetGenericArguments()[0];
                        if (typeof(Sprite).IsAssignableFrom(valueType))
                            return 48;
                        break;
                    }

                    type = type.BaseType;
                }
            }

            return 20;
        }

        /// <summary>
        /// Whether any <see cref="IAssetCatalog"/> registered under <paramref name="catalogId"/> can yield values compatible with <paramref name="constraint"/>.
        /// </summary>
        public static bool CatalogIdMatchesAssetType(string catalogId, Type constraint)
        {
            if (string.IsNullOrEmpty(catalogId) || constraint == null)
                return false;
            IReadOnlyList<IAssetCatalog> assetCatalogs = ServiceLocator.GetAll<IAssetCatalog>(catalogId);
            for (int i = 0; i < assetCatalogs.Count; i++)
            {
                if (CatalogMatchesAssetType(assetCatalogs[i], constraint))
                    return true;
            }

            return false;
        }

        public static List<string> FilterCatalogIdsByAssetType(List<string> catalogIds, Type assetType)
        {
            if (assetType == null || catalogIds == null || catalogIds.Count == 0)
                return catalogIds ?? new List<string>();

            var result = new List<string>();
            for (int i = 0; i < catalogIds.Count; i++)
            {
                if (CatalogIdMatchesAssetType(catalogIds[i], assetType))
                    result.Add(catalogIds[i]);
            }

            return result;
        }

        internal static bool CatalogMatchesAssetType(IAssetCatalog catalog, Type constraint)
        {
            if (catalog == null || constraint == null)
                return false;

            Type t = catalog.GetType();
            while (t != null)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(AssetCatalog<>))
                {
                    Type genArg = t.GetGenericArguments()[0];
                    return constraint.IsAssignableFrom(genArg);
                }

                t = t.BaseType;
            }

            return false;
        }
    }

    internal sealed class CatalogKeyAdvancedDropdown : AdvancedDropdown
    {
        public event Action<string> OnKeySelected;

        private readonly string _catalogId;
        private readonly bool _allowEmpty;
        private Dictionary<string, string> _nameToKey = new();

        public CatalogKeyAdvancedDropdown(string catalogId, bool allowEmpty, AdvancedDropdownState state) : base(state)
        {
            _catalogId = catalogId;
            _allowEmpty = allowEmpty;
            minimumSize = new Vector2(250, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(string.IsNullOrEmpty(_catalogId) ? "Select catalog first" : _catalogId);
            _nameToKey.Clear();

            if (_allowEmpty)
            {
                var noneItem = new AdvancedDropdownItem("(None)");
                _nameToKey[noneItem.name] = "";
                root.AddChild(noneItem);
                root.AddSeparator();
            }

            List<(string key, string group)> grouped = ResolveGroupedKeys();
            if (grouped == null || grouped.Count == 0)
                return root;

            bool hasGroups = grouped.Any(e => !string.IsNullOrEmpty(e.group));

            if (!hasGroups)
            {
                foreach (var entry in grouped.OrderBy(e => e.key))
                    root.AddChild(MakeItem(entry.key, entry.key));
            }
            else
            {
                var groups = grouped
                    .GroupBy(e => e.group ?? "")
                    .OrderBy(g => g.Key);

                foreach (var group in groups)
                {
                    var keys = group.OrderBy(e => e.key).ToList();

                    if (string.IsNullOrEmpty(group.Key))
                    {
                        foreach (var entry in keys)
                            root.AddChild(MakeItem(entry.key, entry.key));
                    }
                    else if (keys.Count == 1)
                    {
                        var entry = keys[0];
                        root.AddChild(MakeItem($"{group.Key}/{entry.key}", entry.key));
                    }
                    else
                    {
                        var folder = new AdvancedDropdownItem(group.Key);
                        foreach (var entry in keys)
                            folder.AddChild(MakeItem(entry.key, entry.key));
                        root.AddChild(folder);
                    }
                }
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (_nameToKey.TryGetValue(item.name, out string key))
                OnKeySelected?.Invoke(key);
        }

        private AdvancedDropdownItem MakeItem(string label, string key)
        {
            string displayLabel = label;
            if (!CatalogKeyHelper.HasAssetCatalog(_catalogId) && CatalogKeyHelper.HasValueCatalog(_catalogId))
            {
                string resolved = CatalogKeyHelper.ResolveDisplayValue(_catalogId, key);
                if (!string.IsNullOrEmpty(resolved))
                    displayLabel = $"{label} ({resolved})";
            }

            var item = new AdvancedDropdownItem(displayLabel);
            _nameToKey[item.name] = key;
            item.icon = CatalogKeyHelper.ResolvePreview(_catalogId, key);
            return item;
        }

        private List<(string key, string group)> ResolveGroupedKeys()
        {
            if (string.IsNullOrEmpty(_catalogId))
                return null;

            IReadOnlyList<ICatalog> registeredCatalogs = ServiceLocator.GetAll<ICatalog>(_catalogId);
            if (registeredCatalogs.Count == 0)
                return null;

            var results = new List<(string key, string group)>();
            var active = new List<(ICatalog source, string name, List<string> keys)>();

            for (int i = 0; i < registeredCatalogs.Count; i++)
            {
                ICatalog catalog = registeredCatalogs[i];
                List<string> keys = catalog.GetKeys()?.ToList();
                if (keys == null || keys.Count <= 0)
                    continue;
                string name = (catalog is UnityEngine.Object obj) ? obj.name : catalog.GetType().Name;
                active.Add((catalog, name, keys));
            }

            if (active.Count == 0)
                return results.Count > 0 ? results : null;

            bool useGroups = active.Count > 1;
            foreach ((_, string name, List<string> keys) in active)
            {
                string group = useGroups ? name : null;
                foreach (string key in keys)
                    results.Add((key, group));
            }

            return results;
        }
    }

    [CustomPropertyDrawer(typeof(CatalogKey))]
    [CustomPropertyDrawer(typeof(CatalogKeyConstraintAttribute))]
    public class CatalogKeyDrawer : PropertyDrawer
    {
        private static readonly Dictionary<int, AdvancedDropdownState> s_catalogKeyDropdownStates = new();

        /// <summary>
        /// Resolves constraint from attribute (when registered via CatalogKeyConstraintAttribute),
        /// then fieldInfo (when registered via CatalogKey type). Handles [SerializeReference] contexts
        /// where fieldInfo may be null.
        /// </summary>
        private CatalogKeyConstraintAttribute ResolveConstraint()
        {
            if (attribute is CatalogKeyConstraintAttribute fromAttr)
                return fromAttr;
            return fieldInfo?.GetCustomAttribute<CatalogKeyConstraintAttribute>();
        }

        /// <summary>
        /// Returns a warning message if the constrained catalog has no registered providers, or null if everything is fine.
        /// Covers both asset-type mismatch (existing) and missing catalog registration (new).
        /// </summary>
        private static string GetConstraintWarning(CatalogKeyConstraintAttribute constraint)
        {
            if (constraint == null || string.IsNullOrEmpty(constraint.CatalogId))
                return null;

            // Asset type constraint: warn if no matching asset catalog
            if (constraint.AssetType != null &&
                !CatalogKeyHelper.CatalogIdMatchesAssetType(constraint.CatalogId, constraint.AssetType))
                return $"Catalog '{constraint.CatalogId}' has no registered IAssetCatalog matching asset type '{constraint.AssetType.Name}'.";

            // General: warn if no catalog of any kind is registered
            if (!CatalogKeyHelper.HasAnyCatalog(constraint.CatalogId))
                return $"No catalog registered under '{constraint.CatalogId}'. Keys cannot be browsed until the catalog is loaded (e.g. via ServiceRegistrySO).";

            return null;
        }

        private static AdvancedDropdownState GetCatalogKeyDropdownState(SerializedProperty property)
        {
            int id = property.serializedObject.targetObject.GetHashCode();
            unchecked
            {
                id = id * 31 + StringComparer.Ordinal.GetHashCode(property.propertyPath);
            }

            if (!s_catalogKeyDropdownStates.TryGetValue(id, out var state) || state == null)
            {
                state = new AdvancedDropdownState();
                s_catalogKeyDropdownStates[id] = state;
            }

            return state;
        }

        private static void SyncConstraintCatalog(SerializedProperty catalogProp, CatalogKeyConstraintAttribute constraint)
        {
            if (constraint == null || string.IsNullOrEmpty(constraint.CatalogId))
                return;
            if (catalogProp.stringValue == constraint.CatalogId)
                return;
            catalogProp.stringValue = constraint.CatalogId;
            catalogProp.serializedObject.ApplyModifiedProperties();
        }

        private static string BuildCatalogKeySummaryText(SerializedProperty catalogProp, SerializedProperty keyProp)
        {
            string c = catalogProp.stringValue;
            string k = keyProp.stringValue;
            bool hasC = !string.IsNullOrEmpty(c);
            bool hasK = !string.IsNullOrEmpty(k);

            string displayKey = k;
            if (hasC && hasK)
            {
                string resolved = CatalogKeyHelper.ResolveDisplayValue(c, k);
                if (!string.IsNullOrEmpty(resolved))
                    displayKey = $"{k} ({resolved})";
            }

            if (hasC && hasK)
                return $"{c} / {displayKey}";
            if (hasC)
                return $"{c} / (None)";
            if (hasK)
                return $"(No catalog) / {displayKey}";
            return "(None)";
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float sp = EditorGUIUtility.standardVerticalSpacing;
            SerializedProperty catalogProp = property.FindPropertyRelative(nameof(CatalogKey.Catalog));
            SerializedProperty keyProp = property.FindPropertyRelative(nameof(CatalogKey.Key));
            if (catalogProp == null || keyProp == null)
                return lineH;

            float h = lineH;
            if (!property.isExpanded)
                return h;

            h += sp;
            CatalogKeyConstraintAttribute constraint = ResolveConstraint();

            h += MeasureCatalogRowImGuiHeight(catalogProp, constraint) + sp;
            string catalogId = catalogProp.stringValue;
            bool hasKeySources = !string.IsNullOrEmpty(catalogId) &&
                                 ServiceLocator.GetAll<ICatalog>(catalogId).Count > 0;
            bool hasAssetCatalog = CatalogKeyHelper.HasAssetCatalog(catalogId);
            float keyRowH = lineH;
            if (hasKeySources && hasAssetCatalog)
                keyRowH = Mathf.Max(lineH, CatalogKeyHelper.ResolvePreviewSize(catalogId));
            h += keyRowH;
            return h;
        }

        private static float MeasureCatalogRowImGuiHeight(SerializedProperty catalogProp,
            CatalogKeyConstraintAttribute constraint)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float sp = EditorGUIUtility.standardVerticalSpacing;
            if (constraint != null && !string.IsNullOrEmpty(constraint.CatalogId))
                return lineH;

            List<string> ids = CatalogKeyHelper.GetRegisteredCatalogIds();
            if (constraint?.AssetType != null)
                ids = CatalogKeyHelper.FilterCatalogIdsByAssetType(ids, constraint.AssetType);

            if (ids.Count == 0 && constraint?.AssetType != null)
                return lineH * 2f + sp + lineH + 4f;
            return lineH;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty catalogProp = property.FindPropertyRelative(nameof(CatalogKey.Catalog));
            SerializedProperty keyProp = property.FindPropertyRelative(nameof(CatalogKey.Key));
            if (catalogProp == null || keyProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Malformed CatalogKey");
                return;
            }

            CatalogKeyConstraintAttribute constraint = ResolveConstraint();

            SyncConstraintCatalog(catalogProp, constraint);

            float lineH = EditorGUIUtility.singleLineHeight;
            float sp = EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.BeginProperty(position, label, property);

            string catalogId = catalogProp.stringValue;
            bool hasAssetCatalog = CatalogKeyHelper.HasAssetCatalog(catalogId);
            int headerPreviewSize = hasAssetCatalog ? Mathf.Min(CatalogKeyHelper.ResolvePreviewSize(catalogId), 48) : 0;

            string headerWarning = GetConstraintWarning(constraint)
                ?? GetKeyWarningTooltip(catalogProp.stringValue, keyProp.stringValue);

            Rect headerRow = new Rect(position.x, position.y, position.width, lineH);
            float foldW = 14f;
            float warnIconW = headerWarning != null ? lineH + 2f : 0f;
            float previewReserve = headerPreviewSize > 0 ? headerPreviewSize + 6f : 0f;
            Rect foldRect = new Rect(headerRow.x, headerRow.y, foldW, lineH);
            Rect warnIconRect = new Rect(headerRow.x + foldW, headerRow.y, warnIconW, lineH);
            Rect summaryRect = new Rect(headerRow.x + foldW + warnIconW, headerRow.y,
                Mathf.Max(0f, headerRow.width - foldW - warnIconW - previewReserve), lineH);
            Rect previewHeaderRect = new Rect(headerRow.xMax - previewReserve + 4f, headerRow.y,
                headerPreviewSize, headerPreviewSize);

            property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, GUIContent.none, true);

            if (headerWarning != null)
            {
                GUIContent warnContent = EditorGUIUtility.IconContent("console.warnicon.sml");
                warnContent.tooltip = headerWarning;
                EditorGUI.LabelField(warnIconRect, warnContent);
            }

            EditorGUI.LabelField(summaryRect, BuildCatalogKeySummaryText(catalogProp, keyProp),
                EditorStyles.miniLabel);

            if (headerPreviewSize > 0)
            {
                Texture2D prev = CatalogKeyHelper.ResolvePreview(catalogProp.stringValue, keyProp.stringValue);
                if (prev != null)
                    GUI.DrawTexture(previewHeaderRect, prev, ScaleMode.ScaleToFit);
            }

            float y = position.y + lineH + sp;
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float catalogH = MeasureCatalogRowImGuiHeight(catalogProp, constraint);
                Rect catalogRow = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, catalogH));
                DrawCatalogRowImGui(catalogRow, catalogProp, constraint);
                y += catalogH + sp;

                string cid = catalogProp.stringValue;
                bool hk = !string.IsNullOrEmpty(cid) && ServiceLocator.GetAll<ICatalog>(cid).Count > 0;
                bool ha = CatalogKeyHelper.HasAssetCatalog(cid);
                float keyH = lineH;
                if (hk && ha)
                    keyH = Mathf.Max(lineH, CatalogKeyHelper.ResolvePreviewSize(cid));
                Rect keyRow = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, keyH));
                DrawKeyRowImGui(keyRow, property, catalogProp, keyProp);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static void DrawCatalogRowImGui(Rect row, SerializedProperty catalogProp,
            CatalogKeyConstraintAttribute constraint)
        {
            Rect labelRect = new Rect(row.x, row.y, EditorGUIUtility.labelWidth, row.height);
            Rect fieldRect = new Rect(row.x + EditorGUIUtility.labelWidth, row.y,
                Mathf.Max(0f, row.width - EditorGUIUtility.labelWidth), row.height);
            EditorGUI.LabelField(labelRect, "Catalog");

            if (constraint != null && !string.IsNullOrEmpty(constraint.CatalogId))
            {
                EditorGUI.LabelField(fieldRect, constraint.CatalogId);
                return;
            }

            List<string> ids = CatalogKeyHelper.GetRegisteredCatalogIds();
            if (constraint?.AssetType != null)
                ids = CatalogKeyHelper.FilterCatalogIdsByAssetType(ids, constraint.AssetType);

            if (ids.Count == 0)
            {
                if (constraint?.AssetType != null)
                {
                    float lineH = EditorGUIUtility.singleLineHeight;
                    float sp = EditorGUIUtility.standardVerticalSpacing;
                    float warnH = lineH * 2f;
                    EditorGUI.HelpBox(
                        new Rect(fieldRect.x, fieldRect.y, fieldRect.width, warnH),
                        $"No registered catalogs match asset type '{constraint.AssetType.Name}'. Enter catalog id manually.",
                        MessageType.Warning);
                    Rect tfRect = new Rect(fieldRect.x, fieldRect.y + warnH + 4f, fieldRect.width, lineH);
                    EditorGUI.PropertyField(tfRect, catalogProp, GUIContent.none);
                    return;
                }

                EditorGUI.PropertyField(fieldRect, catalogProp, GUIContent.none);
                return;
            }

            List<string> choices = new List<string>(ids);
            string current = catalogProp.stringValue;
            if (!string.IsNullOrEmpty(current) && choices.FindIndex(s => s == current) < 0)
                choices.Add(current);

            int IndexOfId(string id)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    if (string.Equals(choices[i], id, StringComparison.Ordinal))
                        return i;
                }

                return -1;
            }

            int index = IndexOfId(current);
            if (index < 0)
                index = 0;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(fieldRect, index, choices.ToArray());
            if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < choices.Count)
            {
                catalogProp.stringValue = choices[newIndex];
                catalogProp.serializedObject.ApplyModifiedProperties();
            }
        }

        private static string GetKeyWarningTooltip(string catalogId, string key)
        {
            if (string.IsNullOrEmpty(catalogId))
                return null;
            if (string.IsNullOrEmpty(key))
                return "Key is empty.";
            bool resolved = CatalogKeyHelper.HasAssetCatalog(catalogId)
                ? CatalogKeyHelper.ResolveValue(catalogId, key) != null
                : !string.IsNullOrEmpty(CatalogKeyHelper.ResolveDisplayValue(catalogId, key));
            if (!resolved)
                return $"Key '{key}' not found in catalog '{catalogId}'.";
            return null;
        }

        private void DrawKeyRowImGui(Rect row, SerializedProperty parent, SerializedProperty catalogProp,
            SerializedProperty keyProp)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            Rect labelRect = new Rect(row.x, row.y, EditorGUIUtility.labelWidth, row.height);
            Rect fieldRect = new Rect(row.x + EditorGUIUtility.labelWidth, row.y,
                Mathf.Max(0f, row.width - EditorGUIUtility.labelWidth), row.height);
            EditorGUI.LabelField(labelRect, "Key");

            string catalogId = catalogProp.stringValue;
            bool hasCatalog = !string.IsNullOrEmpty(catalogId);
            bool hasKeySources = hasCatalog && ServiceLocator.GetAll<ICatalog>(catalogId).Count > 0;
            bool hasAssetCatalog = CatalogKeyHelper.HasAssetCatalog(catalogId);
            bool hasValueCatalog = CatalogKeyHelper.HasValueCatalog(catalogId);

            // Draw warning icon for empty/unresolved key
            string keyWarning = GetKeyWarningTooltip(catalogId, keyProp.stringValue);
            if (keyWarning != null)
            {
                float iconW = lineH;
                Rect iconRect = new Rect(fieldRect.x, fieldRect.y, iconW, lineH);
                GUIContent warnContent = EditorGUIUtility.IconContent("console.warnicon.sml");
                warnContent.tooltip = keyWarning;
                EditorGUI.LabelField(iconRect, warnContent);
                fieldRect = new Rect(fieldRect.x + iconW + 2f, fieldRect.y,
                    Mathf.Max(0f, fieldRect.width - iconW - 2f), fieldRect.height);
            }

            if (!hasKeySources)
            {
                EditorGUI.PropertyField(fieldRect, keyProp, GUIContent.none);
                return;
            }

            string k = keyProp.stringValue;
            string buttonText;
            if (string.IsNullOrEmpty(k))
                buttonText = "(None)";
            else if (hasValueCatalog && !hasAssetCatalog)
            {
                string resolved = CatalogKeyHelper.ResolveDisplayValue(catalogId, k);
                buttonText = !string.IsNullOrEmpty(resolved) ? $"{k} ({resolved})" : k;
            }
            else
                buttonText = k;

            float pickW = Mathf.Min(220f, fieldRect.width * 0.45f);
            Rect pickRect = new Rect(fieldRect.x, fieldRect.y, pickW, row.height);
            Rect restRect = new Rect(fieldRect.x + pickW + 4f, fieldRect.y,
                Mathf.Max(0f, fieldRect.width - pickW - 4f), row.height);

            if (GUI.Button(pickRect, buttonText, EditorStyles.popup))
            {
                var dropdown = new CatalogKeyAdvancedDropdown(catalogId, allowEmpty: true, GetCatalogKeyDropdownState(parent));
                dropdown.OnKeySelected += key =>
                {
                    keyProp.stringValue = key;
                    keyProp.serializedObject.ApplyModifiedProperties();
                };
                dropdown.Show(GUIUtility.GUIToScreenRect(pickRect));
            }

            if (hasAssetCatalog)
            {
                float previewSize = CatalogKeyHelper.ResolvePreviewSize(catalogId);
                float objW = Mathf.Clamp(restRect.width - previewSize - 6f, 80f, restRect.width);
                Rect objRect = new Rect(restRect.x, restRect.y, objW, row.height);
                Rect imgRect = new Rect(restRect.xMax - previewSize, restRect.y, previewSize, previewSize);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.ObjectField(objRect, CatalogKeyHelper.ResolveValue(catalogId, k), typeof(UnityEngine.Object), false);
                }

                Texture2D tex = CatalogKeyHelper.ResolvePreview(catalogId, k);
                if (tex != null)
                    GUI.DrawTexture(imgRect, tex, ScaleMode.ScaleToFit);
            }
            else if (hasValueCatalog)
            {
                string resolved = CatalogKeyHelper.ResolveDisplayValue(catalogId, k);
                EditorGUI.LabelField(restRect, resolved ?? "", EditorStyles.miniLabel);
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var catalogProp = property.FindPropertyRelative(nameof(CatalogKey.Catalog));
            var keyProp = property.FindPropertyRelative(nameof(CatalogKey.Key));

            CatalogKeyConstraintAttribute constraint = ResolveConstraint();

            if (constraint != null && !string.IsNullOrEmpty(constraint.CatalogId))
            {
                if (catalogProp.stringValue != constraint.CatalogId)
                {
                    catalogProp.stringValue = constraint.CatalogId;
                    catalogProp.serializedObject.ApplyModifiedProperties();
                }
            }

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;

            // --- Header row: foldout toggle + summary label + preview ---
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;

            var foldout = new Foldout();
            foldout.text = property.displayName;
            foldout.value = false;
            foldout.style.flexGrow = 0;
            foldout.style.flexShrink = 0;

            var summaryLabel = new Label();
            summaryLabel.style.flexGrow = 1;
            summaryLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            summaryLabel.style.overflow = Overflow.Hidden;
            summaryLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            summaryLabel.style.opacity = 0.7f;

            string catalogId = catalogProp.stringValue;
            bool hasAssetCatalog = CatalogKeyHelper.HasAssetCatalog(catalogId);

            Image headerPreview = null;
            if (hasAssetCatalog)
            {
                int previewSize = CatalogKeyHelper.ResolvePreviewSize(catalogId);
                headerPreview = new Image();
                headerPreview.scaleMode = ScaleMode.ScaleToFit;
                headerPreview.style.flexGrow = 0;
                headerPreview.style.flexShrink = 0;
                headerPreview.style.width = previewSize;
                headerPreview.style.height = previewSize;
                headerPreview.style.backgroundColor = new Color(0f, 0f, 0f, 0.2f);
                headerPreview.style.borderTopLeftRadius = 4;
                headerPreview.style.borderTopRightRadius = 4;
                headerPreview.style.borderBottomLeftRadius = 4;
                headerPreview.style.borderBottomRightRadius = 4;
                headerPreview.style.marginLeft = 4;
                headerPreview.style.marginRight = 2;
            }

            // Header warning icon — shows for catalog issues OR empty/unresolved key
            var warnIcon = new Image();
            warnIcon.image = EditorGUIUtility.IconContent("console.warnicon.sml").image;
            warnIcon.scaleMode = ScaleMode.ScaleToFit;
            warnIcon.style.flexGrow = 0;
            warnIcon.style.flexShrink = 0;
            warnIcon.style.width = 16;
            warnIcon.style.height = 16;
            warnIcon.style.alignSelf = Align.Center;
            warnIcon.style.marginRight = 2;
            warnIcon.style.display = DisplayStyle.None;

            void UpdateSummary()
            {
                string c = catalogProp.stringValue;
                string k = keyProp.stringValue;
                bool hasC = !string.IsNullOrEmpty(c);
                bool hasK = !string.IsNullOrEmpty(k);

                string displayKey = k;
                if (hasC && hasK)
                {
                    string resolved = CatalogKeyHelper.ResolveDisplayValue(c, k);
                    if (!string.IsNullOrEmpty(resolved))
                        displayKey = $"{k} ({resolved})";
                }

                if (hasC && hasK) summaryLabel.text = $"{c} / {displayKey}";
                else if (hasC) summaryLabel.text = $"{c} / (None)";
                else if (hasK) summaryLabel.text = $"(No catalog) / {displayKey}";
                else summaryLabel.text = "(None)";

                if (headerPreview != null)
                    headerPreview.image = CatalogKeyHelper.ResolvePreview(c, k);

                // Update header warning
                string warning = GetConstraintWarning(constraint);
                if (warning == null && hasC)
                {
                    if (!hasK)
                        warning = "Key is empty.";
                    else if (CatalogKeyHelper.HasAnyCatalog(c))
                    {
                        bool resolved = CatalogKeyHelper.HasAssetCatalog(c)
                            ? CatalogKeyHelper.ResolveValue(c, k) != null
                            : !string.IsNullOrEmpty(CatalogKeyHelper.ResolveDisplayValue(c, k));
                        if (!resolved)
                            warning = $"Key '{k}' not found in catalog '{c}'.";
                    }
                }

                warnIcon.tooltip = warning ?? "";
                warnIcon.style.display = warning != null ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateSummary();

            headerRow.Add(foldout);
            headerRow.Add(warnIcon);
            headerRow.Add(summaryLabel);
            if (headerPreview != null)
                headerRow.Add(headerPreview);
            root.Add(headerRow);

            // --- Foldout body: catalog row + key row ---
            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Column;
            body.style.paddingLeft = 16;
            body.style.display = DisplayStyle.None;

            body.Add(CreateCatalogRow(property, catalogProp, constraint));
            body.Add(CreateKeyPickerRow(property, catalogProp, keyProp, UpdateSummary));
            root.Add(body);

            foldout.RegisterValueChangedCallback(evt =>
            {
                body.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                summaryLabel.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
                if (headerPreview != null)
                    headerPreview.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
            });

            root.TrackSerializedObjectValue(property.serializedObject, _ => UpdateSummary());

            return root;
        }

        private VisualElement CreateCatalogRow(SerializedProperty parent, SerializedProperty catalogProp,
            CatalogKeyConstraintAttribute constraint)
        {
            var (row, _, inputContainer) = EditorUIElements.CreatePropertyRow("Catalog");

            if (constraint != null && !string.IsNullOrEmpty(constraint.CatalogId))
            {
                var valueLabel = new Label(constraint.CatalogId);
                valueLabel.style.flexGrow = 1;
                valueLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                inputContainer.Add(valueLabel);
                return row;
            }

            var ids = CatalogKeyHelper.GetRegisteredCatalogIds();
            if (constraint?.AssetType != null)
                ids = CatalogKeyHelper.FilterCatalogIdsByAssetType(ids, constraint.AssetType);

            if (ids.Count == 0)
            {
                if (constraint?.AssetType != null)
                {
                    var col = new VisualElement();
                    col.style.flexDirection = FlexDirection.Column;
                    col.Add(new HelpBox(
                        $"No registered catalogs match asset type '{constraint.AssetType.Name}'. Enter catalog id manually.",
                        HelpBoxMessageType.Warning));
                    col.Add(row);
                    var tf = new TextField();
                    tf.BindProperty(catalogProp);
                    tf.style.flexGrow = 1;
                    inputContainer.Add(tf);
                    return col;
                }

                var fallback = new TextField();
                fallback.BindProperty(catalogProp);
                fallback.style.flexGrow = 1;
                inputContainer.Add(fallback);
                return row;
            }

            int IndexOfId(string id)
            {
                for (int i = 0; i < ids.Count; i++)
                    if (string.Equals(ids[i], id, StringComparison.Ordinal))
                        return i;
                return -1;
            }

            var choices = new List<string>(ids);
            string current = catalogProp.stringValue;
            if (!string.IsNullOrEmpty(current) && IndexOfId(current) < 0)
                choices.Add(current);

            int index = IndexOfId(current);
            if (index < 0) index = 0;

            var dropdown = new DropdownField(choices, index);
            dropdown.style.flexGrow = 1;
            dropdown.RegisterValueChangedCallback(evt =>
            {
                catalogProp.stringValue = evt.newValue;
                catalogProp.serializedObject.ApplyModifiedProperties();
            });
            inputContainer.Add(dropdown);
            return row;
        }

        private VisualElement CreateKeyPickerRow(SerializedProperty parent, SerializedProperty catalogProp,
            SerializedProperty keyProp, Action onChanged)
        {
            // Wrapper that rebuilds when catalogs register
            var wrapper = new VisualElement();
            wrapper.style.flexGrow = 1;
            var dropdownState = new AdvancedDropdownState();

            // Capture stable references — SerializedProperty objects can become stale
            // after IMGUI popups (AdvancedDropdown) cause inspector refreshes.
            SerializedObject serializedObj = parent.serializedObject;
            string catalogPath = catalogProp.propertyPath;
            string keyPath = keyProp.propertyPath;

            // Fresh property accessors that always return valid references
            string GetCatalog() => serializedObj.FindProperty(catalogPath)?.stringValue ?? "";
            string GetKey() => serializedObj.FindProperty(keyPath)?.stringValue ?? "";

            void SetKey(string value)
            {
                serializedObj.Update();
                var prop = serializedObj.FindProperty(keyPath);
                if (prop != null)
                {
                    prop.stringValue = value;
                    serializedObj.ApplyModifiedProperties();
                }
            }

            void BuildContent()
            {
                wrapper.Clear();
                var (row, _, inputContainer) = EditorUIElements.CreatePropertyRow("Key");

                string catalogId = GetCatalog();
                bool hasCatalog = !string.IsNullOrEmpty(catalogId);
                bool hasKeySources = hasCatalog && ServiceLocator.GetAll<ICatalog>(catalogId).Count > 0;
                bool hasAssetCatalog = CatalogKeyHelper.HasAssetCatalog(catalogId);
                bool hasValueCatalog = CatalogKeyHelper.HasValueCatalog(catalogId);

                // Warning icon for empty/unresolved key
                var keyWarnIcon = new Image();
                keyWarnIcon.image = EditorGUIUtility.IconContent("console.warnicon.sml").image;
                keyWarnIcon.scaleMode = ScaleMode.ScaleToFit;
                keyWarnIcon.style.flexGrow = 0;
                keyWarnIcon.style.flexShrink = 0;
                keyWarnIcon.style.width = 16;
                keyWarnIcon.style.height = 16;
                keyWarnIcon.style.alignSelf = Align.Center;
                keyWarnIcon.style.marginRight = 2;
                keyWarnIcon.style.display = DisplayStyle.None;

                void UpdateKeyWarning()
                {
                    string cid = GetCatalog();
                    string k = GetKey();
                    string tooltip = GetKeyWarningTooltip(cid, k);
                    keyWarnIcon.style.display = tooltip != null ? DisplayStyle.Flex : DisplayStyle.None;
                    keyWarnIcon.tooltip = tooltip ?? "";
                }

                inputContainer.Add(keyWarnIcon);

                if (!hasKeySources)
                {
                    var tf = new TextField();
                    tf.BindProperty(keyProp);
                    tf.style.flexGrow = 1;
                    inputContainer.Add(tf);
                    UpdateKeyWarning();
                    row.TrackSerializedObjectValue(parent.serializedObject, _ =>
                    {
                        UpdateKeyWarning();
                        onChanged?.Invoke();
                    });
                    wrapper.Add(row);
                    return;
                }

                // Button that opens the AdvancedDropdown
                var button = new Button();
                button.style.flexGrow = 1;
                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.alignSelf = Align.Center;
                inputContainer.Add(button);

                ObjectField previewField = null;
                Image previewImage = null;
                Label stringValueLabel = null;

                if (hasAssetCatalog)
                {
                    previewField = new ObjectField();
                    previewField.SetEnabled(false);
                    previewField.style.flexGrow = 0;
                    previewField.style.flexShrink = 0;
                    previewField.style.width = 150;
                    previewField.style.alignSelf = Align.Center;
                    inputContainer.Add(previewField);

                    int previewSize = CatalogKeyHelper.ResolvePreviewSize(GetCatalog());
                    previewImage = new Image();
                    previewImage.scaleMode = ScaleMode.ScaleToFit;
                    previewImage.style.flexGrow = 0;
                    previewImage.style.flexShrink = 0;
                    previewImage.style.width = previewSize;
                    previewImage.style.height = previewSize;
                    previewImage.style.backgroundColor = new Color(0f, 0f, 0f, 0.2f);
                    previewImage.style.borderTopLeftRadius = 4;
                    previewImage.style.borderTopRightRadius = 4;
                    previewImage.style.borderBottomLeftRadius = 4;
                    previewImage.style.borderBottomRightRadius = 4;
                    previewImage.style.marginLeft = 4;
                    previewImage.style.marginRight = 2;
                    previewImage.style.marginTop = 2;
                    previewImage.style.marginBottom = 2;
                    inputContainer.Add(previewImage);
                }
                else if (hasValueCatalog)
                {
                    stringValueLabel = new Label();
                    stringValueLabel.style.flexGrow = 0;
                    stringValueLabel.style.flexShrink = 1;
                    stringValueLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                    stringValueLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                    stringValueLabel.style.opacity = 0.7f;
                    stringValueLabel.style.marginLeft = 4;
                    stringValueLabel.style.minWidth = 0;
                    inputContainer.Add(stringValueLabel);
                }

                void UpdatePreview()
                {
                    string cid = GetCatalog();
                    string k = GetKey();

                    if (previewField != null && previewImage != null)
                    {
                        if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(k))
                        {
                            previewField.value = null;
                            previewImage.image = null;
                        }
                        else
                        {
                            previewField.value = CatalogKeyHelper.ResolveValue(cid, k);
                            previewImage.image = CatalogKeyHelper.ResolvePreview(cid, k);
                        }
                    }

                    if (stringValueLabel != null)
                    {
                        string resolved = CatalogKeyHelper.ResolveDisplayValue(cid, k);
                        stringValueLabel.text = !string.IsNullOrEmpty(resolved) ? resolved : "";
                    }
                }

                void UpdateButtonText()
                {
                    string cid = GetCatalog();
                    string k = GetKey();
                    if (string.IsNullOrEmpty(k))
                    {
                        button.text = "(None)";
                    }
                    else
                    {
                        string display = CatalogKeyHelper.ResolveDisplayValue(cid, k);
                        button.text = !string.IsNullOrEmpty(display) ? $"{k} ({display})" : k;
                    }
                    UpdatePreview();
                    UpdateKeyWarning();
                    onChanged?.Invoke();
                }

                UpdateButtonText();
                row.TrackSerializedObjectValue(parent.serializedObject, _ => UpdateButtonText());

                button.clicked += () =>
                {
                    string cid = GetCatalog();
                    if (string.IsNullOrEmpty(cid))
                        return;

                    var dropdown = new CatalogKeyAdvancedDropdown(cid, allowEmpty: true, dropdownState);
                    dropdown.OnKeySelected += key =>
                    {
                        SetKey(key);
                        UpdateButtonText();
                    };
                    dropdown.Show(button.worldBound);
                };

                wrapper.Add(row);
            } // end BuildContent

            BuildContent();

            // Rebuild when catalogs register (handles editor-time service registration timing)
            string initCatalogId = GetCatalog();
            bool hasSourcesNow = !string.IsNullOrEmpty(initCatalogId) &&
                ServiceLocator.GetAll<ICatalog>(initCatalogId).Count > 0;
            if (!hasSourcesNow)
            {
                void OnServicesChanged()
                {
                    string cid = GetCatalog();
                    if (string.IsNullOrEmpty(cid))
                        return;
                    if (ServiceLocator.GetAll<ICatalog>(cid).Count > 0)
                    {
                        ServiceLocator.OnChanged -= OnServicesChanged;
                        BuildContent();
                    }
                }

                ServiceLocator.OnChanged += OnServicesChanged;
                wrapper.RegisterCallback<DetachFromPanelEvent>(_ => ServiceLocator.OnChanged -= OnServicesChanged);
            }

            return wrapper;
        }
    }
}
