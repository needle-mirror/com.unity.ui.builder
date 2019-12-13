﻿using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class Builder : BuilderPaneWindow, IBuilderViewportWindow
    {
        BuilderSelection m_Selection;

        BuilderToolbar m_Toolbar;
        BuilderLibrary m_Library;
        BuilderViewport m_Viewport;
        BuilderInspector m_Inspector;
        BuilderUxmlPreview m_UxmlPreview;
        BuilderUssPreview m_UssPreview;

        HighlightOverlayPainter m_HighlightOverlayPainter;

        public BuilderSelection selection => m_Selection;
        public BuilderViewport viewport => m_Viewport;
        public BuilderToolbar toolbar => m_Toolbar;
        public VisualElement documentRootElement => m_Viewport.documentElement;
        public BuilderCanvas canvas => m_Viewport.canvas;

        public HighlightOverlayPainter highlightOverlayPainter => m_HighlightOverlayPainter;

        [MenuItem("Window/UI/UI Builder")]
        public static Builder ShowWindow()
        {
            return GetWindowAndInit<Builder>(BuilderConstants.BuilderWindowTitle);
        }

        public static Builder ActiveWindow
        {
            get
            {
                var builderWindows =  Resources.FindObjectsOfTypeAll<Builder>();
                if (builderWindows.Length > 0)
                {
                    return builderWindows.First();
                }

                return null;
            }
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            // Load assets.
            var builderTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/Builder.uxml");
            var saveDialogTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/BuilderSaveDialog.uxml");

            // Load templates.
            builderTemplate.CloneTree(root);
            saveDialogTemplate.CloneTree(root);

            // Create overlay painter.
            m_HighlightOverlayPainter = new HighlightOverlayPainter();

            // Fetch the save dialog.
            var dialog = root.Q<ModalPopup>("save-dialog");

            // Fetch the tooltip previews.
            var styleSheetsPaneTooltipPreview = root.Q<BuilderTooltipPreview>("stylesheets-pane-tooltip-preview");
            var libraryTooltipPreview = root.Q<BuilderTooltipPreview>("library-tooltip-preview");

            // Create selection.
            m_Selection = new BuilderSelection(root, this);

            // Create Element Context Menu Manipulator 
            var contextMenuManipulator = new BuilderElementContextMenu(this, selection);

            // Create viewport first.
            m_Viewport = new BuilderViewport(this, selection, contextMenuManipulator);
            selection.documentElement = m_Viewport.documentElement;
            var overlayHelper = viewport.Q<OverlayPainterHelperElement>();
            overlayHelper.painter = m_HighlightOverlayPainter;

            // Create the rest of the panes.
            var classDragger = new BuilderClassDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker);
            var hierarchyDragger = new BuilderHierarchyDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker);
            var styleSheetsPane = new BuilderStyleSheets(m_Viewport, selection, classDragger, hierarchyDragger, contextMenuManipulator, m_HighlightOverlayPainter, styleSheetsPaneTooltipPreview);
            var hierarchy = new BuilderHierarchy(m_Viewport, selection, classDragger, hierarchyDragger, contextMenuManipulator, m_HighlightOverlayPainter);
            var libraryDragger = new BuilderLibraryDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker, hierarchy.container, libraryTooltipPreview);
            m_Library = new BuilderLibrary(this, m_Viewport, selection, libraryDragger, libraryTooltipPreview);
            m_Inspector = new BuilderInspector(this, selection);
            m_Toolbar = new BuilderToolbar(this, selection, dialog, m_Viewport, hierarchy, m_Library, m_Inspector, libraryTooltipPreview);
            m_UxmlPreview = new BuilderUxmlPreview(this);
            m_UssPreview = new BuilderUssPreview(this);
            root.Q("viewport").Add(m_Viewport);
            m_Viewport.toolbar.Add(m_Toolbar);
            root.Q("library").Add(m_Library);
            root.Q("style-sheets").Add(styleSheetsPane);
            root.Q("hierarchy").Add(hierarchy);
            root.Q("uxml-preview").Add(m_UxmlPreview);
            root.Q("uss-preview").Add(m_UssPreview);
            root.Q("inspector").Add(m_Inspector);

            // Init selection.
            selection.AssignNotifiers(new IBuilderSelectionNotifier[]
            {
                document,
                m_Viewport,
                styleSheetsPane,
                hierarchy,
                m_Inspector,
                m_UxmlPreview,
                m_UssPreview,
                m_Viewport.parentTracker,
                m_Viewport.resizer,
                m_Viewport.mover,
                m_Viewport.anchorer
            });

            // Command Handler
            commandHandler.RegisterPane(styleSheetsPane);
            commandHandler.RegisterPane(hierarchy);
            commandHandler.RegisterPane(m_Viewport);
            commandHandler.RegisterToolbar(m_Toolbar);

            OnEnableAfterAllSerialization();
        }

        public override void OnEnableAfterAllSerialization()
        {
            // Perform post-serialization functions.
            document.OnAfterBuilderDeserialize(m_Viewport.documentElement);
            m_Toolbar.OnAfterBuilderDeserialize();
            m_Library.OnAfterBuilderDeserialize();
            m_Inspector.OnAfterBuilderDeserialize();

            // Restore selection.
            selection.RestoreSelectionFromDocument(m_Viewport.sharedStylesAndDocumentElement);

            // We claim the change is coming from the Document because we don't
            // want the document hasUnsavedChanges flag to be set at this time.
            m_Selection.NotifyOfStylingChange(document);
            m_Selection.NotifyOfHierarchyChange(document);
        }

        public override void LoadDocument(VisualTreeAsset asset)
        {
            m_Toolbar.LoadDocument(asset);
        }

        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as VisualTreeAsset;
            if (asset == null)
                return false;

            // Already open uxml document will be opened by the default editor.
            var builderWindow = ActiveWindow;
            if (builderWindow != null)
            {
                if (builderWindow.document.visualTreeAsset == asset)
                    return false;
            }

            var builder = GetWindowAndInit<Builder>(BuilderConstants.BuilderWindowTitle);
            builder.LoadDocument(asset);

            return true;
        }
    }
}
