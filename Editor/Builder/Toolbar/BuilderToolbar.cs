using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.UI.Builder
{
    internal class BuilderToolbar : VisualElement, IBuilderAssetModificationProcessor
    {
        BuilderPaneWindow m_PaneWindow;
        BuilderSelection m_Selection;
        ModalPopup m_SaveDialog;
        BuilderViewport m_Viewport;
        BuilderExplorer m_Explorer;
        BuilderLibrary m_Library;
        BuilderInspector m_Inspector;
        BuilderTooltipPreview m_TooltipPreview;

        ToolbarMenu m_FileMenu;
        ToolbarMenu m_ZoomMenu;
        ToolbarButton m_ResetButton;
        ToolbarButton m_FitCanvasButton;
        ToolbarMenu m_CanvasThemeMenu;

        TextField m_SaveDialogUxmlPathField;
        Button m_SaveDialogUxmlLocationButton;
        TextField m_SaveDialogUssPathField;
        Button m_SaveDialogUssLocationButton;
        Button m_SaveDialogSaveButton;
        Button m_SaveDialogCancelButton;
        Label m_SaveDialogTitleLabel;

        string m_SaveDialogValidationBoxMessage;
        IMGUIContainer m_SaveDialogValidationBox;

        bool m_IsDialogSaveAs;
        bool m_HasModifiedUssPathManually = false;

        string m_LastSavePath = "Assets";

        string m_BuilderPackageVersion;

        BuilderDocument document
        {
            get { return m_PaneWindow.document; }
        }

        public BuilderToolbar(
            BuilderPaneWindow paneWindow,
            BuilderSelection selection,
            ModalPopup saveDialog,
            BuilderViewport viewport,
            BuilderExplorer explorer,
            BuilderLibrary library,
            BuilderInspector inspector,
            BuilderTooltipPreview tooltipPreview)
        {
            m_PaneWindow = paneWindow;
            m_Selection = selection;
            m_SaveDialog = saveDialog;
            m_Viewport = viewport;
            m_Explorer = explorer;
            m_Library = library;
            m_Inspector = inspector;
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
            m_SaveDialogUxmlPathField.RegisterCallback<KeyUpEvent>(OnSaveDialogEnterPress);
            m_SaveDialogUssPathField.RegisterCallback<KeyUpEvent>(OnSaveDialogEnterPress);

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

            // File Menu
            m_FileMenu = this.Q<ToolbarMenu>("file-menu");
            SetUpFileMenu();

            // Zoom Menu
            m_ZoomMenu = this.Q<ToolbarMenu>("zoom-menu");
            SetUpZoomMenu();

            // Fit canvas
            m_FitCanvasButton = this.Q<ToolbarButton>("fit-canvas-button");
            m_FitCanvasButton.clickable.clicked += () => m_Viewport.FitCanvas();

            // Preview Button
            var previewButton = this.Q<ToolbarToggle>("preview-button");
            previewButton.RegisterValueChangedCallback(TogglePreviewMode);

            m_CanvasThemeMenu = this.Q<ToolbarMenu>("canvas-theme-menu");
            SetUpCanvasThemeMenu();
            ChangeCanvasTheme(document.currentCanvasTheme);
            UpdateCanvasThemeMenuStatus();

            // Track unsaved changes state change.
            SetViewportSubTitle();

            // Get Builder package version.
            var packageInfo = PackageInfo.FindForAssetPath("Packages/" + BuilderConstants.BuilderPackageName);
            if (packageInfo == null)
                m_BuilderPackageVersion = null;
            else
                m_BuilderPackageVersion = packageInfo.version;

            RegisterCallback<AttachToPanelEvent>(RegisterCallbacks);
        }

        void RegisterCallbacks(AttachToPanelEvent evt)
        {
            RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacks);
            BuilderAssetModificationProcessor.Register(this);
        }

        void UnregisterCallbacks(DetachFromPanelEvent evt)
        {
            UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacks);
            BuilderAssetModificationProcessor.Unregister(this);
        }

        public void OnAssetChange() { }

        public AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            if (IsFileActionCompatible(assetPath, "delete"))
                return AssetDeleteResult.DidNotDelete;
            else
                return AssetDeleteResult.FailedDelete;
        }

        public AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            var sourcePathDirectory = Path.GetDirectoryName(sourcePath);
            var destinationPathDirectory =  Path.GetDirectoryName(destinationPath);

            var actionName = sourcePathDirectory.Equals(destinationPathDirectory) ? "rename" : "move";
            if (IsFileActionCompatible(sourcePath, actionName))
                return AssetMoveResult.DidNotMove;
            else
                return AssetMoveResult.FailedMove;
        }

        bool IsFileActionCompatible(string assetPath, string actionName)
        {
            if (assetPath.Equals(document.ussPath) || assetPath.Equals(document.uxmlPath))
            {
                var fileName = Path.GetFileName(assetPath);
                var acceptAction = BuilderDialogsUtility.DisplayDialog(BuilderConstants.ErrorDialogNotice,
                    string.Format(BuilderConstants.ErrorIncompatibleFileActionMessage, actionName, fileName),
                    BuilderConstants.DialogDiscardOption,
                    string.Format(BuilderConstants.DialogAbortActionOption, actionName.ToPascalCase()));

                if (acceptAction)
                    ForceNewDocument();

                return acceptAction;
            }

            return true;
        }

        void DrawSaveDialogValidationMessage()
        {
            EditorGUILayout.HelpBox(m_SaveDialogValidationBoxMessage, MessageType.Error, true);
        }

        void DisplaySaveDialogValidationMessage(string message)
        {
            m_SaveDialogValidationBoxMessage = message;
            m_SaveDialogValidationBox.style.display = DisplayStyle.Flex;
            m_SaveDialogSaveButton.SetEnabled(false);
        }

        void ClearSaveDialogValidationMessage()
        {
            m_SaveDialogValidationBox.style.display = DisplayStyle.None;
            m_SaveDialogValidationBoxMessage = string.Empty;
            m_SaveDialogSaveButton.SetEnabled(true);
        }

        void ValidateSaveDialogPath(string value)
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

        void OnSaveDialogEnterPress(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Return)
                return;

            SaveDocument();
        }

        void OnUxmlPathFieldChange(ChangeEvent<string> evt)
        {
            if (m_HasModifiedUssPathManually)
                return;

            var newUxmlPath = evt.newValue;
            var lastDot = newUxmlPath.LastIndexOf('.');

            string newUssPath;
            if (lastDot < 0 || newUxmlPath.Substring(lastDot) != BuilderConstants.UxmlExtension)
                newUssPath = newUxmlPath + BuilderConstants.UssExtension;
            else
                newUssPath = newUxmlPath.Substring(0, lastDot) + BuilderConstants.UssExtension;

            m_SaveDialogUssPathField.SetValueWithoutNotify(newUssPath);
            ValidateSaveDialogPath(evt.newValue);
        }

        void OnUssPathFieldChange(ChangeEvent<string> evt)
        {
            m_HasModifiedUssPathManually = true;
            ValidateSaveDialogPath(evt.newValue);
        }

        void OpenSaveFileDialog(string title, TextField field, string extension)
        {
            var newPath = EditorUtility.SaveFilePanel(
                title,
                Path.GetDirectoryName(field.value),
                Path.GetFileName(field.value),
                extension);

            if (string.IsNullOrWhiteSpace(newPath))
                return;

            var appPathLength = Application.dataPath.Length - 6; // - "Assets".Length
            newPath = newPath.Substring(appPathLength);

            field.value = newPath;
        }

        string OpenLoadFileDialog(string title, string extension)
        {
            var loadPath = EditorUtility.OpenFilePanel(
                title,
                Path.GetDirectoryName(m_LastSavePath),
                extension);

            return loadPath;
        }

        void OnUxmlLocationButtonClick()
        {
            OpenSaveFileDialog(BuilderConstants.SaveDialogChooseUxmlPathDialogTitle, m_SaveDialogUxmlPathField, BuilderConstants.Uxml);
        }

        void OnUssLocationButtonClick()
        {
            OpenSaveFileDialog(BuilderConstants.SaveDialogChooseUssPathDialogTitle, m_SaveDialogUssPathField, BuilderConstants.Uss);
        }

        internal void ForceNewDocument()
        {
            document.hasUnsavedChanges = false;
            NewDocument();
        }

        void NewDocument()
        {
            if (!document.CheckForUnsavedChanges())
                return;

            m_Selection.ClearSelection(null);

            document.NewDocument(m_Viewport.documentElement);

            m_Viewport.ResetView();
            m_Inspector?.canvasInspector.Refresh();

            m_Selection.NotifyOfHierarchyChange(document);
            m_Selection.NotifyOfStylingChange(document);

            m_Library?.ResetCurrentlyLoadedUxmlStyles();

            SetViewportSubTitle();
        }

        void NewTestDocument()
        {
            if (!document.CheckForUnsavedChanges())
                return;

            var testAsset = BuilderConstants.UIBuilderPackagePath +
                "/SampleDocument/BuilderSampleCanvas.uxml";
            var originalAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(testAsset);
            LoadDocumentInternal(originalAsset);
        }

        string GenerateNewDocumentName(string ext, string currentPath)
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
                        name = Path.GetFileNameWithoutExtension(document.uxmlPath) + BuilderConstants.UssExtension;
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
        void PromptSaveAsDocumentDialog()
        {
            PromptSaveDocumentDialog(true);
        }

        void PromptSaveDocumentDialog(bool isSaveAs)
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
                    currentPath + GenerateNewDocumentName(BuilderConstants.Uxml, document.uxmlPath));
                m_SaveDialogUxmlPathField.SetEnabled(true);
                m_SaveDialogUxmlPathField.visualInput.Focus();
            }
            else
            {
                m_SaveDialogUxmlPathField.SetValueWithoutNotify(document.uxmlPath);
                m_SaveDialogUxmlPathField.SetEnabled(false);
            }

            if (string.IsNullOrEmpty(document.ussPath) || isSaveAs)
            {
                m_SaveDialogUssPathField.SetValueWithoutNotify(
                    currentPath + GenerateNewDocumentName(BuilderConstants.Uss, document.ussPath));
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

        void SaveDocument()
        {
            var uxmlPath = m_SaveDialogUxmlPathField.value;
            var ussPath = m_SaveDialogUssPathField.value;

            if (!uxmlPath.EndsWith(BuilderConstants.UxmlExtension))
                uxmlPath = uxmlPath + BuilderConstants.UxmlExtension;
            if (!ussPath.EndsWith(BuilderConstants.UssExtension))
                ussPath = ussPath + BuilderConstants.UssExtension;

            SaveDocument(uxmlPath, ussPath);
        }

        void SaveDocument(string uxmlPath, string ussPath)
        {
            var viewportWindow = m_PaneWindow as IBuilderViewportWindow;
            if (viewportWindow == null)
                return;

            // Set asset.
            var needFullRefresh = document.SaveNewDocument(
                uxmlPath, ussPath, viewportWindow.documentRootElement, m_IsDialogSaveAs);

            // Update any uses out there of the currently edited and saved USS.
            RetainedMode.FlagStyleSheetChange();

            // Save last save path.
            m_LastSavePath = Path.GetDirectoryName(uxmlPath);

            // Set doc field value.
            SetViewportSubTitle();

            m_SaveDialog.Hide();

            if (needFullRefresh)
                m_PaneWindow.OnEnableAfterAllSerialization();
            else
                m_Selection.NotifyOfHierarchyChange(document);
        }

        public void OnAfterBuilderDeserialize()
        {
            VisualTreeAsset docFieldValue = null;
            if (!string.IsNullOrEmpty(m_PaneWindow.document.uxmlPath))
                docFieldValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_PaneWindow.document.uxmlPath);
            SetViewportSubTitle();

            ChangeCanvasTheme(document.currentCanvasTheme);
        }

        void LoadDocument(ChangeEvent<Object> evt)
        {
            var visualTreeAsset = evt.newValue as VisualTreeAsset;

            if (!document.CheckForUnsavedChanges())
                return;

            LoadDocumentInternal(visualTreeAsset);
        }

        public bool LoadDocument(VisualTreeAsset visualTreeAsset, bool assetModifiedExternally = false)
        {
            if (!document.CheckForUnsavedChanges(assetModifiedExternally))
                return false;

            LoadDocumentInternal(visualTreeAsset);

            return true;
        }

        void LoadDocumentInternal(VisualTreeAsset visualTreeAsset)
        {
            m_Selection.ClearSelection(null);

            document.LoadDocument(visualTreeAsset, m_Viewport.documentElement);

            m_Viewport.SetViewFromDocumentSetting();
            m_Inspector?.canvasInspector.Refresh();

            m_Selection.NotifyOfStylingChange(document);
            m_Selection.NotifyOfHierarchyChange(document);

            m_Library?.ResetCurrentlyLoadedUxmlStyles();

            m_LastSavePath = Path.GetDirectoryName(document.uxmlPath);

            SetViewportSubTitle();

            ChangeCanvasTheme(document.currentCanvasTheme);
        }

        void SetUpFileMenu()
        {
            m_FileMenu.menu.AppendAction("New", a =>
            {
                NewDocument();
            });

            if (Unsupported.IsDeveloperMode())
            {
                m_FileMenu.menu.AppendAction("New (Test)", a =>
                {
                    NewTestDocument();
                });
            }

            m_FileMenu.menu.AppendAction("Open...", a =>
            {
                var path = OpenLoadFileDialog(BuilderConstants.ToolbarLoadUxmlDialogTitle, BuilderConstants.Uxml);
                if (string.IsNullOrEmpty(path))
                    return;

                var appPath = Application.dataPath;
                if (path.StartsWith(appPath))
                {
                    path = "Assets/" + path.Substring(appPath.Length);
                }
                else
                {
                    Debug.LogError(BuilderConstants.ToolbarCannotLoadUxmlOutsideProjectMessage);
                    return;
                }

                var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                if (asset == null)
                {
                    Debug.LogError(BuilderConstants.ToolbarSelectedAssetIsInvalidMessage);
                    return;
                }

                LoadDocument(asset);
            });

            m_FileMenu.menu.AppendSeparator();

            m_FileMenu.menu.AppendAction("Save", a =>
            {
                PromptSaveDocumentDialog();
            });
            m_FileMenu.menu.AppendAction("Save As...", a =>
            {
                PromptSaveAsDocumentDialog();
            });
        }

        static string GetTextForZoomScale(float scale)
        {
            return (int) (scale * 100) + "%";
        }

        void UpdateZoomMenuText()
        {
            m_ZoomMenu.text = GetTextForZoomScale(m_Viewport.zoomScale);
        }

        void SetUpZoomMenu()
        {
            foreach (var zoomScale in m_Viewport.zoomer.zoomScaleValues)
            {
                m_ZoomMenu.menu.AppendAction(GetTextForZoomScale(zoomScale), a => { m_Viewport.zoomScale = zoomScale; }
                    , a => (m_Viewport.zoomScale == zoomScale) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
            m_Viewport.canvas.RegisterCallback<GeometryChangedEvent>(e => UpdateZoomMenuText());
            UpdateZoomMenuText();
        }

        void SetUpCanvasThemeMenu()
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

            m_CanvasThemeMenu.menu.AppendAction("Runtime", a =>
            {
                ChangeCanvasTheme(BuilderDocument.CanvasTheme.Runtime);
                UpdateCanvasThemeMenuStatus();
            },
                a => document.currentCanvasTheme == BuilderDocument.CanvasTheme.Runtime
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);

        }

        void ChangeCanvasTheme(BuilderDocument.CanvasTheme theme)
        {
            ApplyCanvasTheme(m_Viewport.documentElement, theme);
            ApplyCanvasBackground(m_Viewport.canvas.defaultBackgroundElement, theme);
            ApplyCanvasTheme(m_TooltipPreview, theme);
            ApplyCanvasBackground(m_TooltipPreview, theme);

            document.ChangeDocumentTheme(m_Viewport.documentElement, theme);
        }

        void ApplyCanvasTheme(VisualElement element, BuilderDocument.CanvasTheme theme)
        {
            if (element == null)
                return;

            // Find the runtime stylesheet.
            var runtimeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.RuntimeThemeUSSPath);
            if (runtimeStyleSheet == null)
                runtimeStyleSheet = UIElementsEditorUtility.s_DefaultCommonLightStyleSheet;

            element.styleSheets.Remove(UIElementsEditorUtility.s_DefaultCommonDarkStyleSheet);
            element.styleSheets.Remove(UIElementsEditorUtility.s_DefaultCommonLightStyleSheet);
            element.styleSheets.Remove(runtimeStyleSheet);

            StyleSheet themeStyleSheet = null;

            switch (theme)
            {
                case BuilderDocument.CanvasTheme.Dark:
                    themeStyleSheet = UIElementsEditorUtility.s_DefaultCommonDarkStyleSheet;
                    break;
                case BuilderDocument.CanvasTheme.Light:
                    themeStyleSheet = UIElementsEditorUtility.s_DefaultCommonLightStyleSheet;
                    break;
                case BuilderDocument.CanvasTheme.Runtime:
                    themeStyleSheet = runtimeStyleSheet;
                    break;
                case BuilderDocument.CanvasTheme.Default:
                    themeStyleSheet = null;
                    break;
            }

            if (themeStyleSheet != null)
                element.styleSheets.Add(themeStyleSheet);
        }

        void ApplyCanvasBackground(VisualElement element, BuilderDocument.CanvasTheme theme)
        {
            if (element == null)
                return;

            element.RemoveFromClassList(BuilderConstants.CanvasContainerDarkStyleClassName);
            element.RemoveFromClassList(BuilderConstants.CanvasContainerLightStyleClassName);
            element.RemoveFromClassList(BuilderConstants.CanvasContainerRuntimeStyleClassName);

            switch (theme)
            {
                case BuilderDocument.CanvasTheme.Dark:
                    element.AddToClassList(BuilderConstants.CanvasContainerDarkStyleClassName);
                    break;
                case BuilderDocument.CanvasTheme.Light:
                    element.AddToClassList(BuilderConstants.CanvasContainerLightStyleClassName);
                    break;
                case BuilderDocument.CanvasTheme.Runtime:
                    element.AddToClassList(BuilderConstants.CanvasContainerRuntimeStyleClassName);
                    break;
                case BuilderDocument.CanvasTheme.Default:
                    string defaultClass = EditorGUIUtility.isProSkin
                        ? BuilderConstants.CanvasContainerDarkStyleClassName
                        : BuilderConstants.CanvasContainerLightStyleClassName;
                    element.AddToClassList(defaultClass);
                    break;
            }
        }

        void UpdateCanvasThemeMenuStatus()
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

        void TogglePreviewMode(ChangeEvent<bool> evt)
        {
            m_Viewport.SetPreviewMode(evt.newValue);

            if (evt.newValue)
                m_Explorer?.ClearHighlightOverlay();
            else
                m_Explorer?.ResetHighlightOverlays();
        }

        void SetViewportSubTitle()
        {
            // Have to keep refreshing this because it's a weak pointer.
            document.unsavedChangesStateChanged = SetViewportSubTitle;

            var newFileName = document.uxmlFileName;

            if (string.IsNullOrEmpty(newFileName))
                newFileName = BuilderConstants.ToolbarUnsavedFileDisplayMessage;

            if (document.hasUnsavedChanges)
                newFileName = newFileName + "*";

            var subTitle = newFileName;

            if (!string.IsNullOrEmpty(m_BuilderPackageVersion))
                subTitle += $"{BuilderConstants.SubtitlePrefix}UI Builder {m_BuilderPackageVersion}";

            m_Viewport.subTitle = subTitle;
        }
    }
}
