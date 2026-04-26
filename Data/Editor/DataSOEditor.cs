using System;
using CupkekGames.Core.Editor;
using CupkekGames.Systems;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CupkekGames.Data.Editor
{
    [CustomEditor(typeof(DataSO<>), true)]
    public class DataSOEditor : UnityEditor.Editor
    {
        private TextField _jsonField;
        private EditorTabView _tabView;

        private void OnEnable()
        {
            ServiceLocator.OnChanged += RefreshJsonPreview;
        }

        private void OnDisable()
        {
            ServiceLocator.OnChanged -= RefreshJsonPreview;
        }

        private void RefreshJsonPreview()
        {
            if (_jsonField == null) return;

            if (!ServiceLocator.Has<IDataSerializer>())
            {
                _jsonField.value = "(IDataSerializer not registered)";
                return;
            }

            // IDataSerializer may be registered before its dependencies (e.g. NewtonsoftDataSerializer needs
            // SerializationManager). ServiceLocator.OnChanged runs per registration, so avoid throwing here.
            try
            {
                bool useDefault = _tabView == null || _tabView.ActiveIndex == 0;
                _jsonField.value = GetDataSO().ToJson(useDefault);
            }
            catch (Exception ex)
            {
                _jsonField.value = $"(JSON preview unavailable — {ex.Message})";
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            // Tabs
            _tabView = new EditorTabView();
            _tabView.style.marginTop = 4;
            root.Add(_tabView);

            // Reset on start toggle (right-aligned in tab bar)
            var resetOnStartProp = serializedObject.FindProperty("_resetOnStart");
            var resetToggle = new Toggle("Reset on Start");
            resetToggle.style.flexShrink = 0;
            resetToggle.BindProperty(resetOnStartProp);
            _tabView.AddToolbarRight(resetToggle);

            // Default data tab
            var defaultTab = _tabView.AddTab("Default");
            var defaultDataProp = serializedObject.FindProperty("_defaultData");
            defaultTab.Add(new PropertyField(defaultDataProp));

            // Actual data tab
            var actualTab = _tabView.AddTab("Actual");
            var actualDataProp = serializedObject.FindProperty("_actualData");
            actualTab.Add(new PropertyField(actualDataProp));

            _tabView.OnTabChanged += _ => RefreshJsonPreview();

            // Buttons
            VisualElement buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginTop = 8;

            Button copyButton = new Button(CopyJson) { text = "Copy JSON" };
            copyButton.style.flexGrow = 1;
            copyButton.style.flexBasis = 0;
            buttonRow.Add(copyButton);

            Button pasteButton = new Button(PasteJson) { text = "Paste JSON" };
            pasteButton.style.flexGrow = 1;
            pasteButton.style.flexBasis = 0;
            buttonRow.Add(pasteButton);

            root.Add(buttonRow);

            Button validateButton = new Button(ValidateData) { text = "Validate" };
            validateButton.style.flexGrow = 1;
            validateButton.style.flexBasis = 0;

            Button resetButton = new Button(ResetToDefault) { text = "Reset to Default" };
            resetButton.style.flexGrow = 1;
            resetButton.style.flexBasis = 0;

            VisualElement actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.marginTop = 4;
            actionRow.Add(validateButton);
            actionRow.Add(resetButton);
            root.Add(actionRow);

            Foldout jsonFoldout = new Foldout { text = "JSON Preview", value = false };
            jsonFoldout.style.marginTop = 8;

            _jsonField = new TextField { multiline = true, isReadOnly = true };
            _jsonField.style.whiteSpace = WhiteSpace.Normal;
            _jsonField.style.minHeight = 100;

            if (!ServiceLocator.Has<IDataSerializer>())
                _jsonField.value = "(IDataSerializer not registered)";
            else
            {
                try
                {
                    _jsonField.value = GetDataSO().ToJson(useDefault: true);
                }
                catch (Exception ex)
                {
                    _jsonField.value = $"(JSON preview unavailable — {ex.Message})";
                }
            }

            jsonFoldout.Add(_jsonField);
            root.Add(jsonFoldout);

            // Track serialized object changes to refresh JSON preview
            root.TrackSerializedObjectValue(serializedObject, _ => RefreshJsonPreview());

            return root;
        }

        private void CopyJson()
        {
            if (!RequireSerializer()) return;
            bool useDefault = _tabView.ActiveIndex == 0;
            try
            {
                GUIUtility.systemCopyBuffer = GetDataSO().ToJson(useDefault);
                Debug.Log($"{(useDefault ? "Default" : "Actual")} data JSON copied to clipboard.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Copy JSON failed: {ex.Message}");
            }
        }

        private void PasteJson()
        {
            if (!RequireSerializer()) return;
            string json = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("Clipboard is empty.");
                return;
            }

            bool toDefault = _tabView.ActiveIndex == 0;
            Undo.RecordObject(target, "Paste JSON");
            try
            {
                GetDataSO().LoadFromJson(json, toDefault);
                EditorUtility.SetDirty(target);
                serializedObject.Update();
                Debug.Log($"JSON pasted to {(toDefault ? "default" : "actual")} data.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Paste JSON failed: {ex.Message}");
            }
        }

        private void ValidateData()
        {
            IData data = GetDataSO().Data;
            bool valid = data.Validate();
            if (valid)
                Debug.Log($"[{target.name}] Validation passed.");
            else
                Debug.LogWarning($"[{target.name}] Validation failed.");
        }

        private void ResetToDefault()
        {
            Undo.RecordObject(target, "Reset to Default");
            var defaultProp = serializedObject.FindProperty("_defaultData");
            var actualProp = serializedObject.FindProperty("_actualData");
            actualProp.boxedValue = defaultProp.boxedValue;
            serializedObject.ApplyModifiedProperties();
            Debug.Log($"[{target.name}] Data reset to default.");
        }

        private IDataSO GetDataSO()
        {
            return (IDataSO)target;
        }

        private bool RequireSerializer()
        {
            if (ServiceLocator.Has<IDataSerializer>()) return true;
            Debug.LogError("IDataSerializer is not registered in ServiceLocator.");
            return false;
        }
    }
}
