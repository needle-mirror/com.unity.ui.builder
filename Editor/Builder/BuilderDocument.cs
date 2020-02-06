using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using System;
using System.IO;

namespace Unity.UI.Builder
{
    internal class BuilderDocument : ScriptableObject, IBuilderSelectionNotifier, ISerializationCallbackReceiver, IBuilderAssetPostprocessor
    {
        static readonly string s_DiskJsonFileName = "UIBuilderDocument.json";
        static readonly string s_DiskJsonFolderPath = "Library/UIBuilder";
        static readonly string s_DiskSettingsJsonFolderPath = "Library/UIBuilder/DocumentSettings";

        public enum CanvasTheme
        {
            Default,
            Dark,
            Light,
            Runtime
        }

        [SerializeField]
        VisualTreeAsset m_VisualTreeAssetBackup;

        [SerializeField]
        string m_OpenendVisualTreeAssetOldPath;

        // This is for automatic style path fixing after a uss file name change.
        [SerializeField]
        string m_OpenendMainStyleSheetOldPath;

        [SerializeField]
        StyleSheet m_MainStyleSheetBackup;

        [SerializeField]
        VisualTreeAsset m_VisualTreeAsset;

        [SerializeField]
        StyleSheet m_MainStyleSheet;

        StyleSheet m_CanvasThemeStyleSheet;

        bool m_HasUnsavedChanges = false;
        WeakReference<Action> m_UnsavedChangesStateChangeCallback;
        bool m_DocumentBeingSavedExplicitly = false;

        [SerializeField]
        CanvasTheme m_CurrentCanvasTheme;

        [SerializeField]
        BuilderDocumentSettings m_Settings;

        WeakReference<IBuilderViewportWindow> m_PrimaryViewportWindow = new WeakReference<IBuilderViewportWindow>(null);

        List<BuilderPaneWindow> m_RegisteredWindows = new List<BuilderPaneWindow>();

        public IBuilderViewportWindow primaryViewportWindow
        {
            get
            {
                if (m_PrimaryViewportWindow == null)
                    return null;

                IBuilderViewportWindow window;
                bool isReferenceValid = m_PrimaryViewportWindow.TryGetTarget(out window);
                if (!isReferenceValid)
                    return null;

                return window;
            }
            private set
            {
                m_PrimaryViewportWindow.SetTarget(value);
            }
        }

        public BuilderDocumentSettings settings
        {
            get
            {
                CreateOrLoadSettingsObject();
                return m_Settings;
            }
        }

        public string uxmlFileName
        {
            get
            {
                var path = uxmlPath;
                if (path == null)
                    return null;

                var fileName = Path.GetFileName(path);
                return fileName;
            }
        }

        public string uxmlOldPath
        {
            get { return m_OpenendVisualTreeAssetOldPath; }
        }

        public string uxmlPath
        {
            get { return AssetDatabase.GetAssetPath(m_VisualTreeAsset); }
        }

        public string ussOldPath
        {
            get { return m_OpenendMainStyleSheetOldPath; }
        }

        public string ussPath
        {
            get { return AssetDatabase.GetAssetPath(m_MainStyleSheet); }
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

        public Action unsavedChangesStateChanged
        {
            set
            {
                m_UnsavedChangesStateChangeCallback = new WeakReference<Action>(value, true);
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

                if (m_UnsavedChangesStateChangeCallback.TryGetTarget(out Action action))
                {
                    action.Invoke();
                }
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

        public float viewportZoomScale
        {
            get
            {
                return settings.ZoomScale;
            }
            set
            {
                settings.ZoomScale = value;
                SaveSettingsToDisk();
            }
        }

        public Vector2 viewportContentOffset
        {
            get
            {
                return settings.PanOffset;
            }
            set
            {
                settings.PanOffset = value;
                SaveSettingsToDisk();
            }
        }

        string diskJsonFolderPath
        {
            get
            {
                var path = Application.dataPath + "/../" + s_DiskJsonFolderPath;
                path = Path.GetFullPath(path);
                return path;
            }
        }

        string diskJsonFilePath
        {
            get
            {
                var path = diskJsonFolderPath + "/" + s_DiskJsonFileName;
                return path;
            }
        }

        string diskSettingsJsonFolderPath
        {
            get
            {
                var path = Application.dataPath + "/../" + s_DiskSettingsJsonFolderPath;
                path = Path.GetFullPath(path);
                return path;
            }
        }

        public BuilderDocument()
        {
            hasUnsavedChanges = false;
            EditorApplication.playModeStateChanged += PlayModeStateChange;
            EditorApplication.wantsToQuit += UnityWantsToQuit;
            Clear();
        }

        void OnEnable()
        {
            BuilderAssetPostprocessor.Register(this);
        }

        void OnDisable()
        {
            BuilderAssetPostprocessor.Unregister(this);
        }

        public void RegisterWindow(BuilderPaneWindow window)
        {
            if (window == null || m_RegisteredWindows.Contains(window))
                return;

            m_RegisteredWindows.Add(window);

            if (window is IBuilderViewportWindow)
            {
                primaryViewportWindow = window as IBuilderViewportWindow;
                BroadcastChange();
            }
        }

        public void UnregisterWindow(BuilderPaneWindow window)
        {
            if (window == null)
                return;

            var removed = m_RegisteredWindows.Remove(window);
            if (!removed)
                return;

            if (window is IBuilderViewportWindow && primaryViewportWindow == window as IBuilderViewportWindow)
            {
                primaryViewportWindow = null;
                BroadcastChange();
            }
        }

        public void BroadcastChange()
        {
            foreach (var window in m_RegisteredWindows)
                window.PrimaryViewportWindowChanged();
        }

        public static BuilderDocument CreateInstance()
        {
            var newDoc = ScriptableObject.CreateInstance<BuilderDocument>();
            newDoc.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
            newDoc.name = "BuilderDocument";
            newDoc.LoadFromDisk();
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

        public bool CheckForUnsavedChanges(bool assetModifiedExternally = false)
        {
            if (!hasUnsavedChanges)
                return true;

            int option = -1;
            if (assetModifiedExternally)
            {
                // TODO: Nothing can be done here yet, other than telling the user
                // what just happened. Adding the ability to save unsaved changes
                // after a file has been modified externally will require some
                // major changes to the document flow.
                BuilderDialogsUtility.DisplayDialog(
                    BuilderConstants.SaveDialogExternalChangesPromptTitle,
                    BuilderConstants.SaveDialogExternalChangesPromptMessage);

                return true;
            }
            else
            {
                option = BuilderDialogsUtility.DisplayDialogComplex(
                    BuilderConstants.SaveDialogSaveChangesPromptTitle,
                    BuilderConstants.SaveDialogSaveChangesPromptMessage,
                    BuilderConstants.DialogSaveActionOption,
                    BuilderConstants.DialogCancelOption,
                    BuilderConstants.DialogDontSaveActionOption);

                switch (option)
                {
                    // Save
                    case 0:
                        if (!string.IsNullOrEmpty(uxmlPath) && !string.IsNullOrEmpty(ussPath))
                        {
                            SaveNewDocument(uxmlPath, ussPath, null, false);
                            return true;
                        }
                        else
                        {
                            var builderWindow = Builder.ActiveWindow;
                            if (builderWindow == null)
                                builderWindow = Builder.ShowWindow();

                            builderWindow.toolbar.PromptSaveDocumentDialog();
                            return false;
                        }
                    // Cancel
                    case 1:
                        return false;
                    // Don't Save
                    case 2:
                        RestoreAssetsFromBackup();
                        return true;
                }
            }

            return true;
        }

        public void OnPostProcessAsset(string assetPath)
        {
            if (m_DocumentBeingSavedExplicitly)
                return;

            var newVisualTreeAsset = m_VisualTreeAsset;
            var newMainStyleSheet = m_MainStyleSheet;
            if (assetPath == uxmlOldPath)
            {
                newVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
            }
            else if (assetPath == ussOldPath)
            {
                newMainStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
            }
            else
            {
                return;
            }

            var builderWindow = Builder.ActiveWindow;
            if (builderWindow == null)
                builderWindow = Builder.ShowWindow();

            builderWindow.toolbar.LoadDocument(newVisualTreeAsset, true);
        }

        void RestoreAssetsFromBackup()
        {
            if (m_VisualTreeAsset != null && m_VisualTreeAssetBackup != null)
                m_VisualTreeAssetBackup.DeepOverwrite(m_VisualTreeAsset);

            if (m_MainStyleSheet != null && m_MainStyleSheetBackup != null)
                m_MainStyleSheetBackup.DeepOverwrite(m_MainStyleSheet);
        }

        void ClearBackups()
        {
            m_VisualTreeAssetBackup.Destroy();
            m_VisualTreeAssetBackup = null;

            m_MainStyleSheetBackup.Destroy();
            m_MainStyleSheetBackup = null;
        }

        void PlayModeStateChange(PlayModeStateChange state)
        {
            ClearUndo();
        }

        bool UnityWantsToQuit() => CheckForUnsavedChanges();

        void ClearUndo()
        {
            m_VisualTreeAsset.ClearUndo();
            m_MainStyleSheet.ClearUndo();
        }

        void Clear()
        {
            ClearUndo();

            ClearBackups();
            m_OpenendVisualTreeAssetOldPath = string.Empty;
            m_OpenendMainStyleSheetOldPath = string.Empty;

            if (m_VisualTreeAsset != null)
            {
                if (!AssetDatabase.Contains(m_VisualTreeAsset))
                    m_VisualTreeAsset.Destroy();

                m_VisualTreeAsset = null;
            }

            if (m_MainStyleSheet != null)
            {
                if (!AssetDatabase.Contains(m_MainStyleSheet))
                    m_MainStyleSheet.Destroy();

                m_MainStyleSheet = null;
            }

            m_Settings = null;
        }

        public void ChangeDocumentTheme(VisualElement documentElement, CanvasTheme canvasTheme)
        {
            m_CurrentCanvasTheme = canvasTheme;
            RefreshStyle(documentElement);
        }

        public void NewDocument(VisualElement documentRootElement)
        {
            Clear();

            if (documentRootElement != null)
            {
                documentRootElement.Clear();
                BuilderSharedStyles.GetSelectorContainerElement(documentRootElement).Clear();

                // Re-run initializations and setup, even though there's nothing to clone.
                ReloadDocumentToCanvas(documentRootElement);
            }

            hasUnsavedChanges = false;

            SaveToDisk();
        }

        public bool SaveNewDocument(string uxmlPath, string ussPath, VisualElement documentRootElement, bool isSaveAs)
        {
            if (!isSaveAs)
            {
                var ussInstanceId = mainStyleSheet.GetInstanceID().ToString();
                m_VisualTreeAsset.FixStyleSheetPaths(ussInstanceId, ussPath);

                // Fix old paths if the uss filename/path has since been changed.
                m_VisualTreeAsset.ReplaceStyleSheetPaths(ussOldPath, ussPath);

#if UNITY_2019_3_OR_NEWER
                AddStyleSheetToAllRootElements(ussPath);
#endif
            }
            else
            {
                visualTreeAsset.ReplaceStyleSheetPaths(ussOldPath, ussPath);
            }

            // Need to save a backup before the AssetDatabase.Refresh().
            var tempVisualTreeAsset = m_VisualTreeAsset.DeepCopy();
            var tempMainStyleSheet = m_MainStyleSheet.DeepCopy();

            WriteToFiles(uxmlPath, ussPath);

            m_DocumentBeingSavedExplicitly = true;
            try
            {
                AssetDatabase.Refresh();
            }
            finally
            {
                m_DocumentBeingSavedExplicitly = false;
            }

            var loadedVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            var loadedStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);

            bool needFullRefresh =
                loadedVisualTreeAsset != m_VisualTreeAsset ||
                loadedStyleSheet != m_MainStyleSheet;

            if (needFullRefresh && documentRootElement != null)
            {
                NewDocument(documentRootElement);

                // Re-init setting.
                if (m_Settings != null)
                {
                    m_Settings.UxmlGuid = AssetDatabase.AssetPathToGUID(uxmlPath);
                    m_Settings.UxmlPath = uxmlPath;
                    SaveSettingsToDisk();
                }
            }
            else
            {
                m_VisualTreeAsset.ClearUndo();
                m_MainStyleSheet.ClearUndo();
                ClearBackups();
            }

            // Recreate backups.
            m_VisualTreeAssetBackup = loadedVisualTreeAsset.DeepCopy();
            m_MainStyleSheetBackup = loadedStyleSheet.DeepCopy();

            // To get all the selection markers into the new assets.
            tempVisualTreeAsset.DeepOverwrite(loadedVisualTreeAsset);
            tempMainStyleSheet.DeepOverwrite(loadedStyleSheet);

            // Destroy temps.
            tempVisualTreeAsset.Destroy();
            tempMainStyleSheet.Destroy();

            m_VisualTreeAsset = loadedVisualTreeAsset;
            m_MainStyleSheet = loadedStyleSheet;

            // Reset asset name.
            m_VisualTreeAsset.name = Path.GetFileNameWithoutExtension(uxmlPath);

            m_VisualTreeAsset.ConvertAllAssetReferencesToPaths();

            m_OpenendVisualTreeAssetOldPath = uxmlPath;
            m_OpenendMainStyleSheetOldPath = ussPath;

            hasUnsavedChanges = false;

            SaveToDisk();

            return needFullRefresh;
        }

        void WriteToFiles(string uxmlPath, string ussPath)
        {
            var uxmlText = visualTreeAsset.GenerateUXML(uxmlPath, true);
            var ussText = mainStyleSheet.GenerateUSS();

            // Make sure the folders exist.
            var uxmlFolder = Path.GetDirectoryName(uxmlPath);
            if (!Directory.Exists(uxmlFolder))
                Directory.CreateDirectory(uxmlFolder);
            var ussFolder = Path.GetDirectoryName(ussPath);
            if (!Directory.Exists(ussFolder))
                Directory.CreateDirectory(ussFolder);

            File.WriteAllText(uxmlPath, uxmlText);
            File.WriteAllText(ussPath, ussText);
        }

        void ReloadDocumentToCanvas(VisualElement documentElement)
        {
            if (documentElement == null)
                return;

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

            m_VisualTreeAssetBackup = visualTreeAsset.DeepCopy();
            m_VisualTreeAsset = visualTreeAsset;
            m_VisualTreeAsset.ConvertAllAssetReferencesToPaths();

            // Load styles.
            // TODO: For now we only support one stylesheet so we just load the first one we find.
            var styleSheetsUsed = m_VisualTreeAsset.GetAllReferencedStyleSheets();
            var styleSheet = styleSheetsUsed.Count > 0 ? styleSheetsUsed[0] : null;

            m_MainStyleSheetBackup = null;
            if (styleSheet != null)
            {
                m_MainStyleSheetBackup = styleSheet.DeepCopy();
            }
            m_MainStyleSheet = styleSheet;

            m_OpenendVisualTreeAssetOldPath = AssetDatabase.GetAssetPath(m_VisualTreeAsset);
            m_OpenendMainStyleSheetOldPath = AssetDatabase.GetAssetPath(styleSheet);

            hasUnsavedChanges = false;

            CreateOrLoadSettingsObject();

            ReloadDocumentToCanvas(documentElement);

            SaveToDisk();
        }

        void AddStyleSheetToRootAsset(VisualElementAsset rootAsset, string newUssPath = null)
        {
            if (rootAsset.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                return;

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
                if (!visualTreeAsset.IsRootElement(asset))
                    continue; // Not a root asset.

                AddStyleSheetToRootAsset(asset, newUssPath);
            }
        }

        void AddStyleSheetToRootIfNeeded(VisualElement element)
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

        void LoadFromDisk()
        {
            var path = diskJsonFilePath;

            if (!File.Exists(path))
                return;

            var json = File.ReadAllText(path);
            EditorJsonUtility.FromJsonOverwrite(json, this);

            // Very important we convert asset references to paths here after a restore.
            if (m_VisualTreeAsset != null)
                m_VisualTreeAsset.ConvertAllAssetReferencesToPaths();
        }

        public void SaveToDisk()
        {
            var json = EditorJsonUtility.ToJson(this, true);

            var folderPath = diskJsonFolderPath;
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            File.WriteAllText(diskJsonFilePath, json);
        }

        void CreateOrLoadSettingsObject()
        {
            if (m_Settings != null)
                return;

            m_Settings = new BuilderDocumentSettings();

            var diskDataFound = LoadSettingsFromDisk(m_Settings);
            if (diskDataFound)
                return;

            m_Settings.UxmlGuid = AssetDatabase.AssetPathToGUID(uxmlPath);
            m_Settings.UxmlPath = uxmlPath;
        }

        bool LoadSettingsFromDisk(BuilderDocumentSettings settings)
        {
            var guid = AssetDatabase.AssetPathToGUID(uxmlPath);
            if (string.IsNullOrEmpty(guid))
                return false;

            var folderPath = diskSettingsJsonFolderPath;
            var fileName = guid + ".json";
            var path = folderPath + "/" + fileName;

            if (!File.Exists(path))
                return false;

            var json = File.ReadAllText(path);
            EditorJsonUtility.FromJsonOverwrite(json, settings);

            return true;
        }

        public void SaveSettingsToDisk()
        {
            if (string.IsNullOrEmpty(settings.UxmlGuid) || string.IsNullOrEmpty(settings.UxmlPath))
                return;

            var json = EditorJsonUtility.ToJson(m_Settings, true);

            var folderPath = diskSettingsJsonFolderPath;
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = settings.UxmlGuid + ".json";
            var filePath = folderPath + "/" + fileName;
            File.WriteAllText(filePath, json);
        }
    }
}
