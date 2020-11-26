using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal static class BuilderConstants
    {
        // Builder
        public const string BuilderWindowTitle = "UI Builder";
        public const string BuilderWindowIcon = IconsResourcesPath + "/Generic/UIBuilder";
        public const string BuilderPackageName = "com.unity.ui.builder";
#if UNITY_2019_4
        public const string BuilderMenuEntry = "Window/UI/UI Builder";
#else
        public const string BuilderMenuEntry = "Window/UI Toolkit/UI Builder";
        public const string UIToolkitPackageName = "com.unity.ui";
#endif
        public static readonly Rect BuilderWindowDefaultRect = new Rect(50, 50, 1000, 700);
        // These sizes are copied from EditorWindow.cs. See the default values of EditorWindow.m_MinSize and EditorWindow.m_MaxSize.
        public static readonly Vector2 BuilderWindowDefaultMinSize = new Vector2(100, 100);
        public static readonly Vector2 BuilderWindowDefaultMaxSize = new Vector2(4000, 4000);

        // Numbers
        public static readonly int VisualTreeAssetOrderIncrement = 10;
        public static readonly int VisualTreeAssetOrderHalfIncrement = 5;
        public static readonly float CanvasInitialWidth = 350;
        public static readonly float CanvasInitialHeight = 450; // Making this too large might break tests.
        public static readonly float CanvasMinWidth = 100;
        public static readonly float CanvasMinHeight = 100;
        public static readonly int ClassNameInPillMaxLength = 25;
        public static readonly float TooltipPreviewYOffset = 20;
        public static readonly float ViewportInitialZoom = 1.0f;
        public static readonly Vector2 ViewportInitialContentOffset = new Vector2(20.0f, 20.0f);
        public static readonly int DoubleClickDelay = 50;
        public static readonly int CanvasGameViewSyncInterval = 100;
        public static readonly float OpacityFadeOutFactor = 0.5f;
        public const int MaxTextMeshVertices = 48 * 1024; // Max 48k vertices. We leave room for masking, borders, background, etc. see UIRMeshBuilder.cs
        public const int MaxTextPrintableCharCount = (int)((2 / 3.0) * MaxTextMeshVertices / 4 /* = vertices per quad*/);
        public static readonly float PickSelectionRepeatRectSize = 2f;
        public static readonly float PickSelectionRepeatRectHalfSize = PickSelectionRepeatRectSize / 2;
        public static readonly double PickSelectionRepeatMinTimeDelay = 0.5;

        // Paths
#if UI_BUILDER_PACKAGE
        public const string UIBuilderPackageRootPath = "Packages/" + BuilderPackageName;
        public const string UIBuilderPackagePath = UIBuilderPackageRootPath + "/Editor/UI";
        public const string UIBuilderPackageResourcesPath = UIBuilderPackageRootPath + "/Editor/Resources/";
        public const string UtilitiesPath = UIBuilderPackageRootPath + "/Editor/Utilities";
        public const string IconsResourcesPath = BuilderPackageName + "/Icons";
        public const string UIBuilderTestsRootPath = UIBuilderPackageRootPath + "/Tests/Editor";
#else
        public const string UIBuilderPackageRootPath = "UIBuilderPackageResources";
        public const string UIBuilderPackagePath = UIBuilderPackageRootPath + "/UI";
        public const string UtilitiesPath = UIBuilderPackageRootPath + "/Utilities";
        public const string IconsResourcesPath = UIBuilderPackageRootPath + "/Icons";
        public const string UIBuilderTestsRootPath = "Assets/Editor";
#endif
        public const string LibraryUIPath = UIBuilderPackagePath + "/Library";
        public const string SettingsUIPath = UIBuilderPackagePath + "/Settings";
        public const string LibraryUssPathNoExt = UIBuilderPackagePath + "/Library/BuilderLibrary";
        public const string InspectorUssPathNoExt = UIBuilderPackagePath + "/Inspector/BuilderInspector";
        public const string RuntimeThemeUSSPath = "Packages/com.unity.ui.runtime/USS/Default.uss.asset";
        public const string UIBuilderTestsTestFilesPath = UIBuilderTestsRootPath + "/TestFiles";
        const string BuilderDocumentDiskJsonFileName = "UIBuilderDocument.json";
        const string BuilderDocumentDiskJsonFolderPath = "Library/UIBuilder";
        const string BuilderDocumentDiskSettingsJsonFolderPath = "Library/UIBuilder/DocumentSettings";

        // Global Style Class Names
        public static readonly string HiddenStyleClassName = "unity-builder-hidden";
        public static readonly string ReadOnlyStyleClassName = "unity-builder--readonly";
        public static readonly string ElementTypeClassName = "unity-builder-code-label--element-type";
        public static readonly string ElementNameClassName = "unity-builder-code-label--element-name";
        public static readonly string ElementClassNameClassName = "unity-builder-code-label--element-class-name";
        public static readonly string ElementAttachedStyleSheetClassName = "unity-builder-code-label--element-attached-stylesheet";
        public static readonly string ElementPseudoStateClassName = "unity-builder-code-label--element-pseudo-state";
        public static readonly string TagPillClassName = "unity-builder-tag-pill";
        public static readonly string StyleSelectorBelongsParent = "unity-selector-parent-subdocument";

        // Random Symbols
        public static readonly string SingleSpace = " ";
        public static readonly string Underscore = "_";
        public static readonly string TripleSpace = "   "; // Don't ask.
        public static readonly string SubtitlePrefix = " - ";
        public const string WindowsNewlineChar = "\r\n";
        public const string UnixNewlineChar = "\n";
        public const string NewlineChar = UnixNewlineChar;
        public const string OpenBracket = "(";
        public const string CloseBracket = ")";
        public const string EllipsisText = "...";

        //
        // Elements
        //

        // Special Element Names
        public static readonly string StyleSelectorElementContainerName = "__unity-selector-container-element";
        public static readonly string StyleSelectorElementName = "__unity-selector-element";

        // Element Linked VE Property Names
        public static readonly string ElementLinkedStyleSheetVEPropertyName = "__unity-ui-builder-linked-stylesheet";
        public static readonly string ElementLinkedStyleSheetIndexVEPropertyName = "__unity-ui-builder-linked-stylesheet-index";
        public static readonly string ElementLinkedStyleSelectorVEPropertyName = "__unity-ui-builder-linked-style-selector";
        public static readonly string ElementLinkedFakeStyleSelectorVEPropertyName = "__unity-ui-builder-linked-fake-style-selector";
        public static readonly string ElementLinkedVisualTreeAssetVEPropertyName = "__unity-ui-builder-linked-visual-tree-asset";
        public static readonly string ElementLinkedVisualElementAssetVEPropertyName = "__unity-ui-builder-linked-visual-element-asset";
        public static readonly string ElementLinkedInstancedVisualTreeAssetVEPropertyName = "__unity-ui-builder-instanced-visual-tree-asset";
        public static readonly string ElementLinkedBelongingVisualTreeAssetVEPropertyName = "__unity-ui-builder-belonging-visual-tree-asset";
        public static readonly string ElementLinkedExplorerItemVEPropertyName = "__unity-ui-builder-linked-explorer-item-element";
        public static readonly string ElementLinkedDocumentVisualElementVEPropertyName = "__unity-ui-builder-linked-document-visual-element";
        public static readonly string ElementLinkedVariableHandlerVEPropertyName = "__unity-ui-builder-linked-variable-handler";
        public static readonly string ElementLinkedVariableTooltipVEPropertyName = "__unity-ui-builder-linked-variable-tooltip";

        //
        // Inspector
        //

        // Inspector Style VE Property Names
        public static readonly string InspectorStylePropertyNameVEPropertyName = "__unity-ui-builder-style-property-name";
        public static readonly string InspectorComputedStylePropertyInfoVEPropertyName = "__unity-ui-builder-computed-style-property-info";
        public static readonly string InspectorClassPillLinkedSelectorElementVEPropertyName = "__unity-ui-builder-class-linked-pill-selector-element";

        // Inspector Style Property and Class Names
        public static readonly string BuilderStyleRowBlueOverrideBoxClassName = "unity-builder-inspector-blue-override-box";
        public static readonly string FoldoutFieldPropertyName = "unity-foldout-field";
        public static readonly string FoldoutFieldHeaderClassName = FoldoutFieldPropertyName + "__header";
        public static readonly string InspectorMultiLineTextFieldClassName = "unity-builder-inspector__multi-line-text-field";
        public static readonly string InspectorFlexColumnModeClassName = "unity-builder-inspector--flex-column";
        public static readonly string InspectorFlexColumnReverseModeClassName = "unity-builder-inspector--flex-column-reverse";
        public static readonly string InspectorFlexRowModeClassName = "unity-builder-inspector--flex-row";
        public static readonly string InspectorFlexRowReverseModeClassName = "unity-builder-inspector--flex-row-reverse";
        public static readonly string InspectorCategoryFoldoutOverrideClassName = "unity-builder-inspector__style-category-foldout--override";
        public static readonly string InspectorLocalStyleOverrideClassName = "unity-builder-inspector__style--override";
        public static readonly string InspectorLocalStyleResetClassName = "unity-builder-inspector__style--reset"; // used to reset font style of children
        public static readonly string InspectorLocalStyleVariableClassName = "unity-builder-inspector__style--variable";
        public static readonly string InspectorLocalStyleVariableEditingClassName = "unity-builder-inspector__style--variable-editing";
        public static readonly string InspectorEmptyFoldoutLabelClassName = "unity-builder-inspector__empty-foldout-label";
        public static readonly string InspectorClassPillNotInDocumentClassName = "unity-builder-class-pill--not-in-document";

        // Inspector Links VE Property Names
        public static readonly string InspectorLinkedStyleRowVEPropertyName = "__unity-ui-builder-style-row";
        public static readonly string InspectorLinkedAttributeDescriptionVEPropertyName = "__unity-ui-builder-attribute-description";

        // Inspector Messages
        public static readonly string AddStyleClassValidationSpaces = "Class names cannot contain spaces.";
        public static readonly string AddStyleClassValidationSpacialCharacters = "Class names can only contain letters, numbers, underscores, and dashes.";
        public static readonly string ContextMenuSetMessage = "Set";
        public static readonly string ContextMenuUnsetMessage = "Unset";
        public static readonly string ContextMenuUnsetAllMessage = "Unset All";
        public static readonly string ContextMenuViewVariableMessage = "View Variable";
        public static readonly string ContextMenuSetVariableMessage = "Set Variable";
#if PACKAGE_TEXT_CORE && !UNITY_2019_4 && !UNITY_2020_1 && !UNITY_2020_2 && !UNITY_2020_3
        public static readonly string FontCannotBeNoneMessage = "UI Builder: Font and FontAsset cannot both be set to none.";
#else
        public static readonly string FontCannotBeNoneMessage = "UI Builder: Font cannot be set to none.";
#endif
        public static readonly string InspectorClassPillDoubleClickToCreate = "Double-click to create new USS selector.";
        public static readonly string InspectorClassPillDoubleClickToSelect = "Double-click to select and edit USS selector.";
        public static readonly string InspectorLocalStylesSectionTitleForSelector = "Styles";
        public static readonly string InspectorLocalStylesSectionTitleForElement = "Inlined Styles";
        public static readonly string MultiSelectionNotSupportedMessage = "Multi-selection editing is not supported.";
        public static readonly string InspectorEditorExtensionAuthoringActivated = "You can now use Editor-only controls in this document.";
        public static readonly string VariableNotSupportedInInlineStyleMessage = "Setting variables in inline style is not yet supported.";
        public static readonly string VariableDescriptionsCouldNotBeLoadedMessage = "Could not load the variable descriptions file.";

        //
        // Explorer
        //

        // Explorer Links VE Property Names
        public static readonly string ExplorerItemElementLinkVEPropertyName = "__unity-ui-builder-explorer-item-link";
        public static readonly string ExplorerItemFillItemCallbackVEPropertyName = "__unity-ui-builder-explorer-item-override-template";
        public static readonly string ExplorerStyleClassPillClassNameVEPropertyName = "__unity-ui-builder-explorer-style-class-pill-name";
        public static readonly string ExplorerItemLinkedUXMLFileName = "__unity-ui-builder-linked-uxml-file-name";

        // Explorer Names
        public static readonly string ExplorerItemRenameTextfieldName = "unity-builder-explorer__rename-textfield";

        // Explorer Style Class Names
        public static readonly string ExplorerHeaderRowClassName = "unity-builder-explorer__header";
        public static readonly string ExplorerItemUnselectableClassName = "unity-builder-explorer--unselectable";
        public static readonly string ExplorerItemHiddenClassName = "unity-builder-explorer--hidden";
        public static readonly string ExplorerItemHoverClassName = "unity-builder-explorer__item--hover";
        public static readonly string ExplorerItemReorderZoneClassName = "unity-builder-explorer__reorder-zone";
        public static readonly string ExplorerItemReorderZoneAboveClassName = "unity-builder-explorer__reorder-zone-above";
        public static readonly string ExplorerItemReorderZoneBelowClassName = "unity-builder-explorer__reorder-zone-below";
        public static readonly string ExplorerItemRenameTextfieldClassName = "unity-builder-explorer__rename-textfield";
        public static readonly string ExplorerItemNameLabelClassName = "unity-builder-explorer__name-label";
        public static readonly string ExplorerItemTypeLabelClassName = "unity-builder-explorer__type-label";
        public static readonly string ExplorerItemLabelContClassName = "unity-builder-explorer-tree-item-label-cont";
        public static readonly string ExplorerItemLabelClassName = "unity-builder-explorer-tree-item-label";
        public static readonly string ExplorerItemIconClassName = "unity-builder-explorer-tree-item-icon";
        public static readonly string ExplorerStyleSheetsPaneClassName = "unity-builder-stylesheets-pane";
        public static readonly string ExplorerActiveStyleSheetClassName = "unity-builder-stylesheets-pane--active-stylesheet";
        public static readonly string ExplorerItemBelongsToOpenDocument = "unity-builder-explorer-excluded";

        // StyleSheets Pane Menu
        public static readonly string ExplorerStyleSheetsPanePlusMenuNoElementsMessage = "Need at least one element in UXML to add StyleSheets.";
        public static readonly string ExplorerStyleSheetsPaneSetActiveUSS = "Set as Active USS";
        public static readonly string ExplorerStyleSheetsPaneCreateNewUSSMenu = "Create New USS";
        public static readonly string ExplorerStyleSheetsPaneAddExistingUSSMenu = "Add Existing USS";
        public static readonly string ExplorerStyleSheetsPaneRemoveUSSMenu = "Remove USS";
        public static readonly string ExplorerStyleSheetsPaneAddToNewUSSMenu = "Add to New USS";
        public static readonly string ExplorerStyleSheetsPaneAddToExistingUSSMenu = "Add to Existing USS";

        // Hierarchy Pane Menu
        public static readonly string ExplorerHierarchyPaneOpenSubDocument = "Open as Sub-Document";
        public static readonly string ExplorerHierarchyPaneOpenSubDocumentInPlace = "Open as Sub-Document In-Place";
        public static readonly string ExplorerHierarchyReturnToParentDocument = "Return to Parent Document";
        public static readonly string ExplorerHierarchyOpenInBuilder = "Open in UI Builder";

        // Explorer Messages
        public static readonly string ExplorerInExplorerNewClassSelectorInfoMessage = "Add new selector...";

        // Code Preview Messages
        public static readonly string CodePreviewTruncatedTextMessage = "The content is truncated because it is too long.";

        //
        // Library
        //

        // Library Item Links VE Property Names
        public static readonly string LibraryItemLinkedManipulatorVEPropertyName = "__unity-ui-builder-dragger";
        public static readonly string LibraryItemLinkedTemplateContainerPathVEPropertyName = "__unity-ui-builder-template-container-path";

        // Library Style Class Names
        public static readonly string LibraryCurrentlyOpenFileItemClassName = "unity-builder-library__currently-open-file";

        // Library Menu
        public const string LibraryShowPackageFiles = "Show Package Files";
        public const string LibraryViewModeToggle = "Tree View";
        public const string LibraryEditorExtensionsAuthoring = "Editor Extension Authoring";
        public const string LibraryProjectTabName = "Project";
        public const string LibraryStandardControlsTabName = "Standard";

        // Library Content
        public const string LibraryContainersSectionHeaderName = "Containers";
        public const string LibraryControlsSectionHeaderName = "Controls";
        public const string LibraryAssetsSectionHeaderName = "UI Documents (UXML)";
        public const string LibraryCustomControlsSectionHeaderName = "Custom Controls (C#)";
        public const string EditorOnlyTag = "Editor Only";

        //
        // Selection
        //

        // Special Selection Asset Marker Names
        public static readonly string SelectedVisualElementAssetAttributeName = "__unity-builder-selected-element";
        public static readonly string SelectedVisualElementAssetAttributeValue = "selected";
        public static readonly string SelectedStyleRulePropertyName = "--ui-builder-selected-style-property";
        public static readonly string SelectedStyleSheetSelectorName = "__unity_ui_builder_selected_stylesheet";
        public static readonly string SelectedVisualTreeAssetSpecialElementTypeName = typeof(UnityUIBuilderSelectionMarker).FullName;

        //
        // Canvas
        //

        // Canvas Container Style Class Names
        public static readonly string CanvasContainerDarkStyleClassName = "unity-builder-canvas__container--dark";
        public static readonly string CanvasContainerLightStyleClassName = "unity-builder-canvas__container--light";
        public static readonly string CanvasContainerRuntimeStyleClassName = "unity-builder-canvas__container--runtime";
        public static readonly string SpecialVisualElementInitialMinSizeName = "__unity-builder-canvas__special-visual-element-initial-size";
        public static readonly string SpecialVisualElementInitialMinSizeClassName = "unity-builder-canvas__special-visual-element-initial-size";

        //
        // Toolbar
        //

        // Toolbar Messages
        public static readonly string ToolbarLoadUxmlDialogTitle = "Load UXML File";
        public static readonly string ToolbarCannotLoadUxmlOutsideProjectMessage = "UI Builder: Cannot load .uxml files outside the Project.";
        public static readonly string ToolbarSelectedAssetIsInvalidMessage = "UI Builder: The asset selected was not a valid UXML asset.";
        public static readonly string ToolbarUnsavedFileSuffix = "*";
        public static readonly string ToolbarUnsavedFileDisplayMessage = "<unsaved file>" + ToolbarUnsavedFileSuffix;

        //
        // Undo/Redo
        //

        // User Undo/Redo Messages
        public static readonly string ChangeAttributeValueUndoMessage = "Change UI Attribute Value";
        public static readonly string ChangeUIStyleValueUndoMessage = "Change UI Style Value";
        public static readonly string ChangeSelectionUndoMessage = "Change UI Builder Selection";
        public static readonly string CreateUIElementUndoMessage = "Create UI Element";
        public static readonly string DeleteUIElementUndoMessage = "Delete UI Element";
        public static readonly string ReparentUIElementUndoMessage = "Reparent UI Element";
        public static readonly string AddStyleClassUndoMessage = "Add UI Style Class";
        public static readonly string CreateStyleClassUndoMessage = "Extract Local Style to New Class";
        public static readonly string RemoveStyleClassUndoMessage = "Remove UI Style Class";
        public static readonly string AddNewSelectorUndoMessage = "Create USS Selector";
        public static readonly string RenameSelectorUndoMessage = "Rename USS Selector";
        public static readonly string DeleteSelectorUndoMessage = "Delete USS Selector";
        public static readonly string MoveUSSSelectorUndoMessage = "Move USS Selector";
        public static readonly string SaveAsNewDocumentsDialogMessage = "Save As New UI Documents";
        public static readonly string NewDocumentsDialogMessage = "New UI Documents";

        //
        // Dialogs
        //

        // Generic Dialog Messages
        public static readonly string DialogOkOption = "Ok";
        public static readonly string DialogCancelOption = "Cancel";
        public static readonly string DialogDiscardOption = "Discard";
        public static readonly string DialogAbortActionOption = "Abort {0}";
        public static readonly string DialogSaveActionOption = "Save";
        public static readonly string DialogDontSaveActionOption = "Don't Save";

        // Save Dialog Messages
        public static readonly string SaveDialogChooseUxmlPathDialogTitle = "Choose UXML File Location";
        public static readonly string SaveDialogChooseUssPathDialogTitle = "Choose USS File Location";
        public static readonly string SaveDialogSaveChangesPromptTitle = "UI Builder: Document has unsaved changes.";
        public static readonly string SaveDialogSaveChangesPromptMessage = "Do you want to save changes you made?";
        public static readonly string SaveDialogExternalChangesPromptTitle = "UI Builder: Document has been modified in an external editor.";
        public static readonly string SaveDialogExternalChangesPromptMessage = "Unsaved changes in the UI Builder will be lost.\nPlease avoid changing files externally while they are open in the Builder.";
        public static readonly string SaveDialogInvalidPathMessage = "Can only save in the 'Assets/' or 'Packages/' folders.";

        // Error Dialog Messages
        public static readonly string ErrorDialogNotice = "UI Builder: Notice";
        public static readonly string ErrorIncompatibleFileActionMessage =
            "You are about to {0}:\n\n{1}\n\nwhich is currently open in the UI Builder. " +
            "If you {0} the file, the UI Builder document will close, " +
            "and you will lose any unsaved changes. Would you like to {0} the file anyway?";
        public static readonly string InvalidUXMLOrUSSAssetNameSuffix = "[UNSUPPORTED_IN_UI_BUILDER]";
        public static readonly string InvalidUSSDialogTitle = "UI Builder: Unable to parse USS file.";
        public static readonly string InvalidUSSDialogMessage = "UI Builder Failed to open {0}.uss asset. This may be due to invalid USS syntax or USS syntax the UI Builder does not yet support (ie. Variables). Check console for details.";
        public static readonly string InvalidUXMLDialogTitle = "UI Builder: Unable to parse UXML file.";
        public static readonly string InvalidUXMLDialogMessage = "UI Builder Failed to open {0}.uxml asset. This may be due to invalid UXML syntax or UXML syntax the UI Builder does not yet support. Check console for details.";

        // StyleSheets Dialogs
        public static readonly string ExtractInlineStylesNoUSSDialogTitle = "UI Builder: No USS in current document.";
        public static readonly string ExtractInlineStylesNoUSSDialogMessage = "There is no StyleSheet (USS) added to this UXML document. Where would you like to add this new USS rule?";
        public static readonly string ExtractInlineStylesNoUSSDialogNewUSSOption = "Add to New USS";
        public static readonly string ExtractInlineStylesNoUSSDialogExistingUSSOption = "Add to Existing USS";
        public static readonly string DeleteLastElementDialogTitle = "UI Builder: Deleting last element.";
        public static readonly string DeleteLastElementDialogMessage = "You are about to delete the last element. Since USS files are attached to root elements, with no elements in the document, no USS files can be attached. Any existing USS files attached will be removed. You can always undo this operation and get everything back. Continue?";
        public static readonly string InvalidWouldCauseCircularDependencyMessage = "Invalid operation.";
        public static readonly string InvalidWouldCauseCircularDependencyMessageDescription = "Can not add as TemplateContainer, as would create a circular dependency.";

        //
        // Messages
        //

        // Warnings
        public static readonly string ClassNameValidationSpacialCharacters = "Class name can only contain letters, numbers, underscores, and dashes.";
        public static readonly string AttributeValidationSpacialCharacters = "{0} attribute can only contain letters, numbers, underscores, and dashes.";
        public static readonly string BindingPathAttributeValidationSpacialCharacters = "{0} attribute can only contain letters, numbers, underscores, dots and dashes.";
        public static readonly string StyleSelectorValidationSpacialCharacters = "Style Selector can only contain *_-.#>, letters, and numbers.";
        public static readonly string TypeAttributeInvalidTypeMessage = "{0} attribute is an invalid type. Make sure to include assembly name.";
        public static readonly string TypeAttributeMustDeriveFromMessage = "{0} attribute type must derive from {1}";
        public static readonly string BuiltInAssetPathsNotSupportedMessage = "Built-in resource paths are not supported in USS.";
        public static readonly string DocumentMatchGameViewModeDisabled = "Match Game View mode disabled.";

        // Settings
        public const string BuilderEditorExtensionModeToggleLabel = "Enable Editor Extension by default";

        // Notifications
        public const string NoUIToolkitPackageInstalledNotification = "Your Project is not configured to support UI Toolkit runtime UI. To enable runtime support, install the UI Toolkit package.";

        //
        // UXML/USS
        //

        // UXML/USS Trivials
        public static readonly string Uxml = "uxml";
        public static readonly string Uss = "uss";
        public static readonly string UxmlExtension = ".uxml";
        public static readonly string UssExtension = ".uss";
        public static readonly string TssExtension = ".tss";

        // UXML
        public static readonly string UxmlHeader = "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\"";
        public static readonly string UxmlFooter = "</ui:UXML>";
        public static readonly string UxmlEngineNamespace = "UnityEngine.UIElements.";
        public static readonly string UxmlEngineNamespaceReplace = "ui:";
        public static readonly string UxmlEditorNamespace = "UnityEditor.UIElements.";
        public static readonly string UxmlEditorNamespaceReplace = "uie:";
        public static readonly string UxmlTagTypeName = "UnityEngine.UIElements.UXML";
        public static readonly string UxmlInstanceTypeName = "UnityEngine.UIElements.Instance";

        // USS
        public static readonly string UssSelectorNameSymbol = "#";
        public static readonly string UssSelectorClassNameSymbol = ".";
        public static readonly string UssSelectorPseudoStateSymbol = ":";
        public static readonly string UssVariablePrefix = "--";
        public static readonly string USSVariablePattern = @"[^a-z0-9A-Z_-]";
        public static readonly string USSVariableInvalidCharFiller = "-";

        // Styles
        public static readonly List<string> SpecialSnowflakeLengthStyles = new List<string>()
        {
            "border-left-width",
            "border-right-width",
            "border-top-width",
            "border-bottom-width"
        };

        internal static readonly List<string> ViewportOverlayEnablingStyleProperties = new List<string>()
        {
            "width",
            "height",
            "margin-left",
            "margin-right",
            "margin-top",
            "margin-bottom",
            "padding-left",
            "padding-right",
            "padding-top",
            "padding-bottom",
            "border-left-width",
            "border-right-width",
            "border-top-width",
            "border-bottom-width"
        };

        public static readonly Dictionary<string, string> SpecialEnumNamesCases = new Dictionary<string, string>
        {
            {"nowrap", "no-wrap"},
            {"tabindex", "tab-index"}
        };

        //
        // Complex Getters
        //

        public static string newlineCharFromEditorSettings
        {
            get
            {
                string preferredLineEndings;
                switch (EditorSettings.lineEndingsForNewScripts)
                {
                    case LineEndingsMode.OSNative:
                        if (Application.platform == RuntimePlatform.WindowsEditor)
                            preferredLineEndings = WindowsNewlineChar;
                        else
                            preferredLineEndings = UnixNewlineChar;
                        break;
                    case LineEndingsMode.Unix:
                        preferredLineEndings = UnixNewlineChar;
                        break;
                    case LineEndingsMode.Windows:
                        preferredLineEndings = WindowsNewlineChar;
                        break;
                    default:
                        preferredLineEndings = UnixNewlineChar;
                        break;
                }
                return preferredLineEndings;
            }
        }

        public static string builderDocumentDiskJsonFolderAbsolutePath
        {
            get
            {
                var path = Application.dataPath + "/../" + BuilderDocumentDiskJsonFolderPath;
                path = Path.GetFullPath(path);
                return path;
            }
        }

        public static string builderDocumentDiskJsonFileAbsolutePath
        {
            get
            {
                var path = builderDocumentDiskJsonFolderAbsolutePath + "/" + BuilderDocumentDiskJsonFileName;
                return path;
            }
        }

        public static string builderDocumentDiskSettingsJsonFolderAbsolutePath
        {
            get
            {
                var path = Application.dataPath + "/../" + BuilderDocumentDiskSettingsJsonFolderPath;
                path = Path.GetFullPath(path);
                return path;
            }
        }
    }
}
