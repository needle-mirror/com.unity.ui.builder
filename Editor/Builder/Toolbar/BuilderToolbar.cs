using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal class BuilderToolbar : VisualElement
    {
        private Builder m_Builder;
        private BuilderSelection m_Selection;
        private ModalPopup m_SaveDialog;
        private BuilderViewport m_Viewport;
        private BuilderExplorer m_Explorer;
        private BuilderLibrary m_Library;
        private BuilderTooltipPreview m_TooltipPreview;

        private ObjectField m_DocumentField;
        private ToolbarMenu m_CanvasThemeMenu;

        private TextField m_SaveDialogUxmlPathField;
        private Button m_SaveDialogUxmlLocationButton;
        private TextField m_SaveDialogUssPathField;
        private Button m_SaveDialogUssLocationButton;
        private Button m_SaveDialogSaveButton;
        private Button m_SaveDialogCancelButton;
        private Label m_SaveDialogTitleLabel;

        private string m_SaveDialogValidationBoxMessage;
        private IMGUIContainer m_SaveDialogValidationBox;

        private IVisualElementScheduledItem m_InGamePreviewScheduledItem;
        private bool m_IsDialogSaveAs;
        private bool m_HasModifiedUssPathManually = false;

        private string m_LastSavePath = "Assets";

        private BuilderDocument document
        {
            get { return m_Builder.document; }
        }

        public BuilderToolbar(
            Builder builder,
            BuilderSelection selection,
            ModalPopup saveDialog,
            BuilderViewport viewport,
            BuilderExplorer explorer,
            BuilderLibrary library,
            BuilderTooltipPreview tooltipPreview)
        {
            m_Builder = builder;
            m_Selection = selection;
            m_SaveDialog = saveDialog;
            m_Viewport = viewport;
            m_Explorer = explorer;
            m_Library = library;
            m_TooltipPreview = tooltipPreview;

            // Query the UI
            m_SaveDialogUxmlPathField = m_SaveDialog.Q<TextField>("save-dialog-uxml-path");
            m_SaveDialogUxmlLocationButton = m_SaveDialog.Q<Button>("save-dialog-uxml-location-button");
            m_SaveDialogUssPathField = m_SaveDialog.Q<TextField>("save-dialog-uss-path");
            m_SaveDialogUssLocationButton = m_SaveDialog.Q<Button>("save-dialog-uss-location-button");
            m_SaveDialogSaveButton = m_SaveDialog.Q<Button>("save-dialog-save-button");
            m_SaveDialogCancelButton = m_SaveDialog.Q<Button>("save-dialog-cancel-button");
            m_SaveDialogTitleLabel = m_SaveDialog.Q<Label>("title");

            m_SaveDialogUxmlPathField.RegisterValueChangedCallback(OnUxmlPathFieldChange);
            m_SaveDialogUssPathField.RegisterValueChangedCallback(OnUssPathFieldChange);

            m_SaveDialogSaveButton.clickable.clicked += SaveDocument;
            m_SaveDialogCancelButton.clickable.clicked += m_SaveDialog.Hide;
            m_SaveDialogUxmlLocationButton.clickable.clicked += OnUxmlLocationButtonClick;
            m_SaveDialogUssLocationButton.clickable.clicked += OnUssLocationButtonClick;

            var saveDialogValidationBoxContainer = m_SaveDialog.Q("save-dialog-validation-box");
            m_SaveDialogValidationBox = new IMGUIContainer(DrawSaveDialogValidationMessage);
            m_SaveDialogValidationBox.style.overflow = Overflow.Hidden;
            saveDialogValidationBoxContainer.Add(m_SaveDialogValidationBox);

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderToolbar.uxml");
            template.CloneTree(this);

            var newButton = this.Q<ToolbarButton>("new-button");
            var newTestButton = this.Q<ToolbarButton>("new-test-button");
            var saveButton = this.Q<ToolbarButton>("save-button");
            var saveAsButton = this.Q<ToolbarButton>("save-as-button");
            var ingameButton = this.Q<ToolbarToggle>("in-game-button");
            var previewButton = this.Q<ToolbarToggle>("preview-button");

            newButton.clickable.clicked += NewDocument;
            newTestButton.clickable.clicked += NewTestDocument;
            saveButton.clickable.clicked += PromptSaveDocumentDialog;
            saveAsButton.clickable.clicked += PromptSaveAsDocumentDialog;
            ingameButton.RegisterValueChangedCallback(ToggleInGameMode);
            previewButton.RegisterValueChangedCallback(TogglePreviewMode);

            m_DocumentField = this.Q<ObjectField>("document-field");
            m_DocumentField.objectType = typeof(VisualTreeAsset);
            m_DocumentField.RegisterValueChangedCallback(LoadDocument);

            m_CanvasThemeMenu = this.Q<ToolbarMenu>("canvas-theme-menu");
            SetUpCanvasThemeMenu();
            ChangeCanvasTheme(document.currentCanvasTheme);
            UpdateCanvasThemeMenuStatus();

            if (!Unsupported.IsDeveloperMode())
                newTestButton.style.display = DisplayStyle.None;
        }

        private void DrawSaveDialogValidationMessage()
        {
            EditorGUILayout.HelpBox(m_SaveDialogValidationBoxMessage, MessageType.Error, true);
        }

        private void DisplaySaveDialogValidationMessage(string message)
        {
            m_SaveDialogValidationBoxMessage = message;
            m_SaveDialogValidationBox.style.display = DisplayStyle.Flex;
            m_SaveDialogSaveButton.SetEnabled(false);

        }

        private void ClearSaveDialogValidationMessage()
        {
            m_SaveDialogValidationBox.style.display = DisplayStyle.None;
            m_SaveDialogValidationBoxMessage = string.Empty;
            m_SaveDialogSaveButton.SetEnabled(true);
        }

        private void ValidateSaveDialogPath(string value)
        {
            if (value.StartsWith("Assets/") || value.StartsWith("Packages/"))
            {
                ClearSaveDialogValidationMessage();
            }
            else
            {
                DisplaySaveDialogValidationMessage(BuilderConstants.SaveDialogInvalidPathMessage);
            }
        }

        public bool CheckForUnsavedChanges()
        {
            if (document.hasUnsavedChanges)
            {
                if (!EditorUtility.DisplayDialog(
                    BuilderConstants.SaveDialogDiscardChangesPromptTitle,
                    BuilderConstants.SaveDialogDiscardChangesPromptMessage,
                    "Discard", "Go Back"))
                {
                    return false;
                }
            }

            return true;
        }

        private void OnUxmlPathFieldChange(ChangeEvent<string> evt)
        {
            if (m_HasModifiedUssPathManually)
                return;

            var newUxmlPath = evt.newValue;
            var lastDot = newUxmlPath.LastIndexOf('.');

            string newUssPath;
            if (lastDot < 0 || newUxmlPath.Substring(lastDot) != ".uxml")
                newUssPath = newUxmlPath + ".uss";
            else
                newUssPath = newUxmlPath.Substring(0, lastDot) + ".uss";

            m_SaveDialogUssPathField.SetValueWithoutNotify(newUssPath);
            ValidateSaveDialogPath(evt.newValue);
        }

        private void OnUssPathFieldChange(ChangeEvent<string> evt)
        {
            m_HasModifiedUssPathManually = true;
            ValidateSaveDialogPath(evt.newValue);
        }

        private void OpenSaveFileDialog(string title, TextField field, string extension)
        {
            var newPath = EditorUtility.SaveFilePanel(
                "Choose USS File Location",
                Path.GetDirectoryName(field.value),
                Path.GetFileName(field.value),
                extension);

            if (string.IsNullOrWhiteSpace(newPath))
                return;

            var appPathLength = Application.dataPath.Length - 6; // - "Assets".Length
            newPath = newPath.Substring(appPathLength);

            field.value = newPath;
        }

        private void OnUxmlLocationButtonClick()
        {
            OpenSaveFileDialog("Choose UXML File Location", m_SaveDialogUxmlPathField, "uxml");
        }

        private void OnUssLocationButtonClick()
        {
            OpenSaveFileDialog("Choose USS File Location", m_SaveDialogUssPathField, "uss");
        }

        private void NewDocument()
        {
            if (!CheckForUnsavedChanges())
                return;

            ResetViewData();

            m_Selection.ClearSelection(null);

            document.NewDocument(m_Viewport.documentElement);

            m_DocumentField.SetValueWithoutNotify(null);

            m_Selection.NotifyOfHierarchyChange(document);
            m_Selection.NotifyOfStylingChange(document);

            m_Library.ResetCurrentlyLoadedUxmlStyles();
        }

        private void NewTestDocument()
        {
            if (!CheckForUnsavedChanges())
                return;

            ResetViewData();

            var testAsset = BuilderConstants.UIBuilderPackagePath +
                "/SampleDocument/BuilderSampleCanvas.uxml";
            var originalAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(testAsset);
            LoadDocumentInternal(originalAsset);
        }

        private void ResetViewData()
        {
            // For now, this is just resetting the Canvas size.
            m_Builder
                .rootVisualElement
                .Query().Where(e => e is IResetableViewData)
                .ForEach(e => (e as IResetableViewData).ResetViewData());
        }

        private string GenerateNewDocumentName(string ext, string currentPath)
        {
            string name = "NewUI." + ext; // Default name if new

            switch (ext)
            {
                case "uxml":
                {
                    if (!string.IsNullOrEmpty(document.uxmlPath))
                    {
                        name = Path.GetFileName(currentPath);
                    }
                    break;
                }
                case "uss":
                {
                    // Default: Set name to existing uss document name
                    if (!string.IsNullOrEmpty(document.ussPath))
                    {
                        name = Path.GetFileName(currentPath);
                    } // Else: Set name to existing uxml document name
                    else if (!string.IsNullOrEmpty(document.uxmlPath))
                    {
                        name = Path.GetFileNameWithoutExtension(document.uxmlPath) + ".uss";
                    }
                    break;
                }
            }
            return name;
        }

        public void PromptSaveDocumentDialog()
        {
            PromptSaveDocumentDialog(false);
        }
        private void PromptSaveAsDocumentDialog()
        {
            PromptSaveDocumentDialog(true);
        }

        private void PromptSaveDocumentDialog(bool isSaveAs)
        {
            m_IsDialogSaveAs = isSaveAs;
            m_SaveDialogTitleLabel.text = isSaveAs ? BuilderConstants.SaveAsNewDocumentsDialogMessage : BuilderConstants.NewDocumentsDialogMessage;

            if (!string.IsNullOrEmpty(document.uxmlPath) && !string.IsNullOrEmpty(document.ussPath) && !isSaveAs)
            {
                SaveDocument(document.uxmlPath, document.ussPath);
                return;
            }

            var currentPath = string.Empty;
            var lastSelectedPath = m_LastSavePath;
            if (!string.IsNullOrEmpty(lastSelectedPath))
                currentPath = lastSelectedPath;

            if (!string.IsNullOrEmpty(currentPath) && !Directory.Exists(currentPath))
                currentPath = Path.GetDirectoryName(currentPath);

            currentPath = currentPath.Replace('\\', '/');

            if (!string.IsNullOrEmpty(currentPath))
                currentPath = currentPath + "/";

            ValidateSaveDialogPath(currentPath);

            if (string.IsNullOrEmpty(document.uxmlPath) || isSaveAs)
            {
                m_SaveDialogUxmlPathField.SetValueWithoutNotify(
                    currentPath + GenerateNewDocumentName("uxml", document.uxmlPath));
                m_SaveDialogUxmlPathField.SetEnabled(true);
            }
            else
            {
                m_SaveDialogUxmlPathField.SetValueWithoutNotify(document.uxmlPath);
                m_SaveDialogUxmlPathField.SetEnabled(false);
            }

            if (string.IsNullOrEmpty(document.ussPath) || isSaveAs)
            {
                m_SaveDialogUssPathField.SetValueWithoutNotify(
                    currentPath + GenerateNewDocumentName("uss", document.ussPath));
                m_SaveDialogUssPathField.SetEnabled(true);
            }
            else
            {
                m_SaveDialogUssPathField.SetValueWithoutNotify(document.ussPath);
                m_SaveDialogUssPathField.SetEnabled(false);
            }

            if (Path.GetFileNameWithoutExtension(m_SaveDialogUxmlPathField.value) ==
                Path.GetFileNameWithoutExtension(m_SaveDialogUssPathField.value))
            {
                m_HasModifiedUssPathManually = false;
            }
            else
            {
                m_HasModifiedUssPathManually = true;
            }

            m_SaveDialog.Show();
        }

        private void SaveDocument()
        {
            var uxmlPath = m_SaveDialogUxmlPathField.value;
            var ussPath = m_SaveDialogUssPathField.value;

            if (!uxmlPath.EndsWith(".uxml"))
                uxmlPath = uxmlPath + ".uxml";
            if (!ussPath.EndsWith(".uss"))
                ussPath = ussPath + ".uss";

            SaveDocument(uxmlPath, ussPath);
        }
        
        private void SaveDocument(string uxmlPath, string ussPath)
        {
            // Set asset.
            var needFullRefresh = document.SaveNewDocument(
                uxmlPath, ussPath, m_Builder.documentRootElement, m_IsDialogSaveAs);

            // Update any uses out there of the currently edited and saved USS.
            RetainedMode.FlagStyleSheetChange();

            // Save last save path.
            m_LastSavePath = Path.GetDirectoryName(uxmlPath);

            // Set doc field value.
            m_DocumentField.SetValueWithoutNotify(document.visualTreeAsset);

            m_SaveDialog.Hide();

            if (needFullRefresh)
                m_Builder.OnEnableAfterAllSerialization();
            else
                m_Selection.NotifyOfHierarchyChange(document);
        }

        public void OnAfterBuilderDeserialize()
        {
            VisualTreeAsset docFieldValue = null;
            if (!string.IsNullOrEmpty(m_Builder.document.uxmlPath))
                docFieldValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_Builder.document.uxmlPath);
            m_DocumentField.SetValueWithoutNotify(docFieldValue);

            ChangeCanvasTheme(document.currentCanvasTheme);
        }

        private void LoadDocument(ChangeEvent<Object> evt)
        {
            var visualTreeAsset = evt.newValue as VisualTreeAsset;

            if (!CheckForUnsavedChanges())
            {
                m_DocumentField.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            LoadDocumentInternal(visualTreeAsset);
        }

        public bool LoadDocument(VisualTreeAsset visualTreeAsset)
        {
            if (!CheckForUnsavedChanges())
                return false;

            LoadDocumentInternal(visualTreeAsset);

            return true;
        }

        private void LoadDocumentInternal(VisualTreeAsset visualTreeAsset)
        {
            m_DocumentField.SetValueWithoutNotify(visualTreeAsset);

            m_Selection.ClearSelection(null);

            ResetViewData();

            document.LoadDocument(visualTreeAsset, m_Viewport.documentElement);

            m_Selection.NotifyOfStylingChange(document);
            m_Selection.NotifyOfHierarchyChange(document);

            m_Library.ResetCurrentlyLoadedUxmlStyles();
        }

        private Camera m_InGamePreviewCamera;
        private RenderTexture m_InGamePreviewRenderTexture;
        private Rect m_InGamePreviewRect;
        private Texture2D m_InGamePreviewTexture2D;

        private void SetupInGamePreview()
        {
            int width = 2 * (int)m_Viewport.documentElement.resolvedStyle.width;
            int height = 2 * (int)m_Viewport.documentElement.resolvedStyle.height;
            m_InGamePreviewRenderTexture = new RenderTexture(width, height, 1);

            m_InGamePreviewCamera = Camera.allCameras[0];

            m_InGamePreviewRect = new Rect(0, 0, width, height);

            m_InGamePreviewTexture2D = new Texture2D(width, height);
        }

        private void TearDownInGamePreview()
        {
            m_Viewport.documentElement.style.backgroundImage = null;

            m_InGamePreviewTexture2D = null;

            m_InGamePreviewCamera = null;

            m_InGamePreviewRenderTexture = null;
        }

        private void UpdateInGameBackground()
        {
            m_InGamePreviewCamera.targetTexture = m_InGamePreviewRenderTexture;

            RenderTexture.active = m_InGamePreviewRenderTexture;
            m_InGamePreviewCamera.Render();

            m_InGamePreviewTexture2D.ReadPixels(m_InGamePreviewRect, 0, 0);
            m_InGamePreviewTexture2D.Apply(false);

            RenderTexture.active = null;
            m_InGamePreviewCamera.targetTexture = null;

            m_Viewport.documentElement.style.backgroundImage = m_InGamePreviewTexture2D;
            m_Viewport.IncrementVersion(VersionChangeType.Repaint);
        }

        private void ToggleInGameMode(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                if (Camera.allCameras != null && Camera.allCameras.Length > 0)
                {
                    SetupInGamePreview();
                    m_InGamePreviewScheduledItem = m_Viewport.documentElement.schedule.Execute(UpdateInGameBackground);
                    m_InGamePreviewScheduledItem.Every(50);
                }
            }
            else
            {
                m_InGamePreviewScheduledItem.Pause();
                m_InGamePreviewScheduledItem = null;
                TearDownInGamePreview();
            }
        }

        private void SetUpCanvasThemeMenu()
        {
            m_CanvasThemeMenu.menu.AppendAction("Default", a =>
                {
                    ChangeCanvasTheme(BuilderDocument.CanvasTheme.Default);
                    UpdateCanvasThemeMenuStatus();
                },
                a => document.currentCanvasTheme == BuilderDocument.CanvasTheme.Default
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);
            m_CanvasThemeMenu.menu.AppendAction("Dark", a =>
                {
                    ChangeCanvasTheme(BuilderDocument.CanvasTheme.Dark);
                    UpdateCanvasThemeMenuStatus();
                },
                a => document.currentCanvasTheme == BuilderDocument.CanvasTheme.Dark
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);
            m_CanvasThemeMenu.menu.AppendAction("Light", a =>
                {
                    ChangeCanvasTheme(BuilderDocument.CanvasTheme.Light);
                    UpdateCanvasThemeMenuStatus();
                },
                a => document.currentCanvasTheme == BuilderDocument.CanvasTheme.Light
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);

        }

        private void ChangeCanvasTheme(BuilderDocument.CanvasTheme theme)
        {
            ApplyCanvasTheme(m_Viewport.documentElement, theme);
            ApplyCanvasTheme(m_TooltipPreview, theme);

            document.ChangeDocumentTheme(m_Viewport.documentElement, theme);
        }

        private void ApplyCanvasTheme(VisualElement element, BuilderDocument.CanvasTheme theme)
        {
            element.styleSheets.Remove(UIElementsEditorUtility.s_DefaultCommonDarkStyleSheet);
            element.styleSheets.Remove(UIElementsEditorUtility.s_DefaultCommonLightStyleSheet);

            StyleSheet themeStyleSheet = null;

            element.RemoveFromClassList(BuilderConstants.CanvasContainerDarkStyleClassName);
            element.RemoveFromClassList(BuilderConstants.CanvasContainerLightStyleClassName);

            switch (theme)
            {
                case BuilderDocument.CanvasTheme.Dark:
                    {
                        element.AddToClassList(BuilderConstants.CanvasContainerDarkStyleClassName);
                        themeStyleSheet = UIElementsEditorUtility.s_DefaultCommonDarkStyleSheet;
                        break;
                    }
                case BuilderDocument.CanvasTheme.Light:
                    {
                        element.AddToClassList(BuilderConstants.CanvasContainerLightStyleClassName);
                        themeStyleSheet = UIElementsEditorUtility.s_DefaultCommonLightStyleSheet;
                        break;
                    }
                case BuilderDocument.CanvasTheme.Default:
                    {
                        string defaultClass = EditorGUIUtility.isProSkin
                            ? BuilderConstants.CanvasContainerDarkStyleClassName
                            : BuilderConstants.CanvasContainerLightStyleClassName;
                        element.AddToClassList(defaultClass);
                        themeStyleSheet = null;
                        break;
                    }
            }

            if (themeStyleSheet != null)
                element.styleSheets.Add(themeStyleSheet);
        }

        private void UpdateCanvasThemeMenuStatus()
        {
            foreach (var item in m_CanvasThemeMenu.menu.MenuItems())
            {
                var action = item as DropdownMenuAction;
                action.UpdateActionStatus(null);

                var theme = document.currentCanvasTheme;

                if (action.status == DropdownMenuAction.Status.Checked)
                    m_CanvasThemeMenu.text = theme + " Theme  ";
            }
        }

        private void TogglePreviewMode(ChangeEvent<bool> evt)
        {
            m_Viewport.SetPreviewMode(evt.newValue);

            if (evt.newValue)
                m_Explorer.ClearHighlightOverlay();
            else
                m_Explorer.ResetHighlightOverlays();
        }
    }
}