using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using System;

namespace Unity.UI.Builder
{
    internal class BuilderDocument : ScriptableObject, IBuilderSelectionNotifier, ISerializationCallbackReceiver
    {
        public enum CanvasTheme
        {
            Default,
            Dark,
            Light
        }

        [SerializeField]
        private VisualTreeAsset m_OpenendVisualTreeAsset;

        // This is for automatic style path fixing after a uss file name change.
        [SerializeField]
        private string m_OpenendMainStyleSheetOldPath;

        [SerializeField]
        private StyleSheet m_OpenendMainStyleSheet;

        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset;

        [SerializeField]
        private StyleSheet m_MainStyleSheet;

        private StyleSheet m_CanvasThemeStyleSheet;

        private bool m_HasUnsavedChanges = false;

        [SerializeField]
        private CanvasTheme m_CurrentCanvasTheme;

        public string uxmlPath
        {
            get { return AssetDatabase.GetAssetPath(m_OpenendVisualTreeAsset); }
        }

        public string ussOldPath
        {
            get { return m_OpenendMainStyleSheetOldPath; }
        }

        public string ussPath
        {
            get { return AssetDatabase.GetAssetPath(m_OpenendMainStyleSheet); }
        }

        public VisualTreeAsset visualTreeAsset
        {
            get
            {
                if (m_VisualTreeAsset == null)
                    m_VisualTreeAsset = VisualTreeAssetUtilities.CreateInstance();

                return m_VisualTreeAsset;
            }
        }

        public StyleSheet mainStyleSheet
        {
            get
            {
                if (m_MainStyleSheet == null)
                    m_MainStyleSheet = StyleSheetUtilities.CreateInstance();

                return m_MainStyleSheet;
            }
        }

        public bool hasUnsavedChanges
        {
            get { return m_HasUnsavedChanges; }
            set
            {
                if (value == m_HasUnsavedChanges)
                    return;

                m_HasUnsavedChanges = value;
            }
        }
        
        public CanvasTheme currentCanvasTheme
        {
            get { return m_CurrentCanvasTheme; }
            set
            {
                if (value == m_CurrentCanvasTheme)
                    return;

                m_CurrentCanvasTheme = value;
            }
        }

        public BuilderDocument()
        {
            hasUnsavedChanges = false;
            Clear();
        }

        public static BuilderDocument CreateInstance()
        {
            var newDoc = ScriptableObject.CreateInstance<BuilderDocument>();
            newDoc.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
            newDoc.name = "BuilderDocument";
            return newDoc;
        }

        public void RefreshStyle(VisualElement documentElement)
        {
            if (!documentElement.styleSheets.Contains(mainStyleSheet))
            {
                documentElement.styleSheets.Clear();
                documentElement.styleSheets.Add(mainStyleSheet);
            }

            StyleCache.ClearStyleCache();
            UnityEngine.UIElements.StyleSheets.StyleSheetCache.ClearCaches();
            mainStyleSheet.FixRuleReferences();
            documentElement.IncrementVersion((VersionChangeType) (-1));
        }

        public void Clear()
        {
            m_OpenendVisualTreeAsset = null;
            m_OpenendMainStyleSheet = null;
            m_OpenendMainStyleSheetOldPath = string.Empty;

            if (m_VisualTreeAsset != null)
            {
                Undo.ClearUndo(m_VisualTreeAsset);

                if (m_VisualTreeAsset.inlineSheet != null)
                {
                    Undo.ClearUndo(m_VisualTreeAsset.inlineSheet);
                    ScriptableObject.DestroyImmediate(m_VisualTreeAsset.inlineSheet);
                }

                ScriptableObject.DestroyImmediate(m_VisualTreeAsset);
                m_VisualTreeAsset = null;
            }

            if (m_MainStyleSheet != null)
            {
                Undo.ClearUndo(m_MainStyleSheet);
                ScriptableObject.DestroyImmediate(m_MainStyleSheet);
                m_MainStyleSheet = null;
            }
        }

        public void ChangeDocumentTheme(VisualElement documentElement, CanvasTheme canvasTheme)
        {
            m_CurrentCanvasTheme = canvasTheme;
            RefreshStyle(documentElement);
        }

        public void NewDocument(VisualElement documentElement)
        {
            Clear();
            documentElement.Clear();
            BuilderSharedStyles.GetSelectorContainerElement(documentElement).Clear();

            // Re-run initializations and setup, even though there's nothing to clone.
            ReloadDocumentToCanvas(documentElement);

            hasUnsavedChanges = false;
        }

        public void SaveNewDocument(VisualTreeAsset visualTreeAsset, StyleSheet styleSheet)
        {
            m_OpenendVisualTreeAsset = visualTreeAsset;
            m_OpenendMainStyleSheet = styleSheet;

            m_OpenendMainStyleSheetOldPath = AssetDatabase.GetAssetPath(styleSheet);

            hasUnsavedChanges = false;
        }

        private void ReloadDocumentToCanvas(VisualElement documentElement)
        {
            // Load the asset.
            documentElement.Clear();
            try
            {
                visualTreeAsset.LinkedCloneTree(documentElement);
            }
            catch (Exception e)
            {
                Debug.LogError("Invalid UXML or USS: " + e.ToString());
                Clear();
            }
            documentElement.SetProperty(
                BuilderConstants.ElementLinkedVisualTreeAssetVEPropertyName, visualTreeAsset);

            // TODO: For now, don't allow stylesheets in root elements.
            foreach (var rootElement in documentElement.Children())
                rootElement.styleSheets.Clear();

            // Refresh styles.
            RefreshStyle(documentElement);

            // Add shared styles.
            BuilderSharedStyles.AddSelectorElementsFromStyleSheet(documentElement, mainStyleSheet);
        }

        public void LoadDocument(VisualTreeAsset visualTreeAsset, VisualElement documentElement)
        {
            NewDocument(documentElement);

            if (visualTreeAsset == null)
                return;

            m_VisualTreeAsset = visualTreeAsset.DeepCopy();

            // Load styles.
            // TODO: For now we only support one stylesheet so we just load the first one we find.
            var styleSheetsUsed = m_VisualTreeAsset.GetAllReferencedStyleSheets();
            var styleSheet = styleSheetsUsed.Count > 0 ? styleSheetsUsed[0] : null;

            m_MainStyleSheet = styleSheet.DeepCopy();

            m_OpenendVisualTreeAsset = visualTreeAsset;
            m_OpenendMainStyleSheet = styleSheet;

            m_OpenendMainStyleSheetOldPath = AssetDatabase.GetAssetPath(styleSheet);

            hasUnsavedChanges = false;

            ReloadDocumentToCanvas(documentElement);
        }

        private void AddStyleSheetToRootAsset(VisualElementAsset rootAsset, string newUssPath = null)
        {
            var localUssPath = ussPath;

            if (!string.IsNullOrEmpty(newUssPath))
                localUssPath = newUssPath;

            if (string.IsNullOrEmpty(localUssPath))
            {
#if UNITY_2019_3_OR_NEWER
                rootAsset.AddStyleSheet(mainStyleSheet);
#endif
                rootAsset.AddStyleSheetPath(
                    BuilderConstants.VisualTreeAssetStyleSheetPathAsInstanceIdSchemeName +
                    mainStyleSheet.GetInstanceID().ToString());
            }
            else
            {
#if UNITY_2019_3_OR_NEWER
                rootAsset.AddStyleSheet(mainStyleSheet);
#endif
                rootAsset.AddStyleSheetPath(localUssPath);
            }
        }

        public void AddStyleSheetToAllRootElements(string newUssPath = null)
        {
            foreach (var asset in visualTreeAsset.visualElementAssets)
            {
                if (asset.parentId != 0)
                    continue; // Not a root asset.

                AddStyleSheetToRootAsset(asset, newUssPath);
            }
        }

        private void AddStyleSheetToRootIfNeeded(VisualElement element)
        {
            var rootElement = BuilderSharedStyles.GetDocumentRootLevelElement(element);
            if (rootElement == null)
                return;

            var rootAsset = rootElement.GetVisualElementAsset();
            if (rootAsset == null)
                return;

            AddStyleSheetToRootAsset(rootAsset);
        }

        public void SelectionChanged()
        {
            // Selection changes don't affect the document.
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            hasUnsavedChanges = true;

            if (element != null) // Add StyleSheet to this element's root element.
                AddStyleSheetToRootIfNeeded(element);
            else // Add StyleSheet to all root elements since one might match this new selector.
                AddStyleSheetToAllRootElements();
        }

        public void StylingChanged(List<string> styles)
        {
            hasUnsavedChanges = true;
        }

        public void OnAfterBuilderDeserialize(VisualElement documentElement)
        {
            // Fix unserialized rule references in Selectors in StyleSheets.
            // VTA.inlineSheet only has Rules so it does not need this fix.
            if (m_MainStyleSheet != null)
                m_MainStyleSheet.FixRuleReferences();

            ReloadDocumentToCanvas(documentElement);
        }

        public void OnBeforeSerialize()
        {
            // Do nothing.
        }

        public void OnAfterDeserialize()
        {
            // Fix unserialized rule references in Selectors in StyleSheets.
            // VTA.inlineSheet only has Rules so it does not need this fix.
            if (m_MainStyleSheet != null)
                m_MainStyleSheet.FixRuleReferences();
        }
    }
}
