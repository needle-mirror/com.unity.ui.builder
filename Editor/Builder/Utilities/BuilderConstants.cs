using System.Collections.Generic;

namespace Unity.UI.Builder
{
    internal static class BuilderConstants
    {
        // Builder
        public static readonly string BuilderWindowTitle = "UI Builder";
        public static readonly string BuilderPackageName = "com.unity.ui.builder";

        // Numbers
        public static readonly int VisualTreeAssetOrderIncrement = 10;
        public static readonly int VisualTreeAssetOrderHalfIncrement = 5;
        public static readonly float CanvasInitialWidth = 350;
        public static readonly float CanvasInitialHeight = 550;
        public static readonly float CanvasMinWidth = 100;
        public static readonly float CanvasMinHeight = 100;
        public static readonly int ClassNameInPillMaxLength = 25;

        // Paths
        public static readonly string UIBuilderPackagePath = "Packages/" + BuilderPackageName + "/Editor/UI";
        public static readonly string UtilitiesPath = "Packages/" + BuilderPackageName + "/Editor/Utilities";
        public static readonly string InspectorUssPathNoExt = UIBuilderPackagePath + "/Inspector/BuilderInspector";
        public static readonly string RuntimeThemeUSSPath = "Packages/com.unity.ui.runtime/USS/Default.uss.asset";

        // Global Style Class Names
        public static readonly string HiddenStyleClassName = "unity-builder-hidden";

        //
        // Elements
        //

        // Special Element Names
        public static readonly string StyleSelectorElementContainerName = "__unity-selector-container-element";
        public static readonly string StyleSelectorElementName = "__unity-selector-element";

        // Special Element Style Class Names
        public static readonly string PaneContentPleaseRefocusElementClassName = "unity-builder--please-refocus";

        // Element Linked VE Property Names
        public static readonly string ElementLinkedStyleSheetVEPropertyName = "__unity-ui-builder-linked-stylesheet";
        public static readonly string ElementLinkedStyleSelectorVEPropertyName = "__unity-ui-builder-linked-style-selector";
        public static readonly string ElementLinkedVisualTreeAssetVEPropertyName = "__unity-ui-builder-linked-visual-tree-asset";
        public static readonly string ElementLinkedVisualElementAssetVEPropertyName = "__unity-ui-builder-linked-visual-element-asset";
        public static readonly string ElementLinkedExplorerItemVEPropertyName = "__unity-ui-builder-linked-explorer-item-element";

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
        public static readonly string InspectorFlexColumnModeClassName = "unity-builder-inspector--flex-column";
        public static readonly string InspectorFlexColumnReverseModeClassName = "unity-builder-inspector--flex-column-reverse";
        public static readonly string InspectorFlexRowModeClassName = "unity-builder-inspector--flex-row";
        public static readonly string InspectorFlexRowReverseModeClassName = "unity-builder-inspector--flex-row-reverse";
        public static readonly string InspectorCategoryFoldoutOverrideClassName = "unity-builder-inspector__style-category-foldout--override";
        public static readonly string InspectorLocalStyleOverrideClassName = "unity-builder-inspector__style--override";
        public static readonly string InspectorLocalStyleResetClassName = "unity-builder-inspector__style--reset"; // used to reset font style of children
        public static readonly string InspectorEmptyFoldoutLabelClassName = "unity-builder-inspector__empty-foldout-label";
        public static readonly string InspectorClassPillNotInDocumentClassName = "unity-builder-class-pill--not-in-document";

        // Inspector Links VE Property Names
        public static readonly string InspectorLinkedStyleRowVEPropertyName = "__unity-ui-builder-style-row";
        public static readonly string InspectorLinkedAttributeDescriptionVEPropertyName = "__unity-ui-builder-attribute-description";

        // Inspector Messages
        public static readonly string AddStyleClassValidationStartsWithDot = "Class names cannot start with a dot as that is how you reference them in USS.";
        public static readonly string AddStyleClassValidationSpaces = "Class names cannot contain spaces.";
        public static readonly string AddStyleClassValidationSpacialCharacters = "Class names can only contain letters, numbers, underscores, and dashes.";
        public static readonly string ContextMenuUnsetMessage = "Unset";
        public static readonly string FontCannotBeNoneMessage = "UI Builder: Font cannot be set to none.";
        public static readonly string InspectorClassPillDoubleClickToCreate = "Double-click to create new USS selector.";
        public static readonly string InspectorClassPillDoubleClickToSelect = "Double-click to select and edit USS selector.";
        public static readonly string InspectorLocalStylesSectionTitleForSelector = "Styles";
        public static readonly string InspectorLocalStylesSectionTitleForElement = "Inlined Styles";

        //
        // Explorer
        //

        // Explorer Links VE Property Names
        public static readonly string ExplorerItemElementLinkVEPropertyName = "__unity-ui-builder-explorer-item-link";
        public static readonly string ExplorerItemFillItemCallbackVEPropertyName = "__unity-ui-builder-explorer-item-override-template";
        public static readonly string ExplorerStyleClassPillClassNameVEPropertyName = "__unity-ui-builder-explorer-style-class-pill-name";

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
        public static readonly string ExplorerItemRenameTextfieldClassNamePre2019_3 = "unity-builder-explorer__rename-textfield__pre-2019-3";
        public static readonly string ExplorerItemRenameTextfieldClassName = "unity-builder-explorer__rename-textfield";
        public static readonly string ExplorerItemNameLabelClassName = "unity-builder-explorer__name-label";
        public static readonly string ExplorerItemTypeLabelClassName = "unity-builder-explorer__type-label";

        // Explorer Messages
        public static readonly string ExplorerInExplorerNewClassSelectorInfoMessage = "Add new selector...";

        //
        // Library
        //

        // Library Item Links VE Property Names
        public static readonly string LibraryItemLinkedManipulatorVEPropertyName = "__unity-ui-builder-dragger";
        public static readonly string LibraryItemLinkedTemplateContainerPathVEPropertyName = "__unity-ui-builder-template-container-path";

        // Library Style Class Names
        public static readonly string LibraryCurrentlyOpenFileItemClassName = "unity-builder-library__currently-open-file";

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
        public static readonly string ToolbarUnsavedFileDisplayMessage = "<unsaved file>";

        //
        // VisualTreeAsset/StyleSheet
        //

        // VisualTreeAsset
        public static readonly string VisualTreeAssetStyleSheetPathAsInstanceIdSchemeName = "instanceId:";

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
        public static readonly string VisualTreeAssetUnsavedUssFileMessage = "*unsaved in-memory StyleSheet with ";
        public static readonly string SaveAsNewDocumentsDialogMessage = "Save As New UI Documents";
        public static readonly string NewDocumentsDialogMessage = "New UI Documents";

        //
        // Save Dialog
        //

        // Save Dialog Messages
        public static readonly string SaveDialogChooseUxmlPathDialogTitle = "Choose UXML File Location";
        public static readonly string SaveDialogChooseUssPathDialogTitle = "Choose USS File Location";
        public static readonly string SaveDialogDiscardChangesPromptTitle = "UI Builder: Discard unsaved changes?";
        public static readonly string SaveDialogDiscardChangesPromptMessage = "Are you sure you want to discard unsaved changes on the current UI documents?";
        public static readonly string SaveDialogDiscardChangesPromptDiscardOption = "Discard";
        public static readonly string SaveDialogDiscardChangesPromptGoBackOption = "Go Back";
        public static readonly string SaveDialogInvalidPathMessage = "Can only save in the 'Assets/' or 'Packages/' folders.";

        //
        // UXML/USS
        //

        // UXML/USS Trivials
        public static readonly string Uxml = "uxml";
        public static readonly string Uss = "uss";
        public static readonly string UxmlExtension = ".uxml";
        public static readonly string UssExtension = ".uss";

        // UXML
        public static readonly string UxmlHeader = "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\">\n";
        public static readonly string UxmlFooter = "</ui:UXML>\n";
        public static readonly string UxmlEngineNamespace = "UnityEngine.UIElements.";
        public static readonly string UxmlEngineNamespaceReplace = "ui:";
        public static readonly string UxmlEditorNamespace = "UnityEditor.UIElements.";
        public static readonly string UxmlEditorNamespaceReplace = "uie:";
#if UNITY_2020_1_OR_NEWER
        public static readonly string UxmlTagTypeName = "UnityEngine.UIElements.UXML";
        public static readonly string UxmlInstanceTypeName = "UnityEngine.UIElements.Instance";
#endif

        // Styles
        public static readonly List<string> SpecialSnowflakeLengthSytles = new List<string>()
        {
            "border-left-width",
            "border-right-width",
            "border-top-width",
            "border-bottom-width"
        };

        // Version Style Classes
        public static readonly string Version_2019_2 = "unity-builder-2019-2";
        public static readonly string Version_2019_3_OrNewer = "unity-builder-2019-3-or-newer";
    }
}
