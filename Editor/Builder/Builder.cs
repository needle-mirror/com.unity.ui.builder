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

        public BuilderSelection selection => m_Selection;

        public BuilderViewport viewport => m_Viewport;
        public VisualElement documentRootElement => m_Viewport.documentElement;
        public BuilderCanvas canvas => m_Viewport.canvas;

        [MenuItem("Window/UI/UI Builder")]
        public static void ShowWindow()
        {
            GetWindowAndInit<Builder>(BuilderConstants.BuilderWindowTitle);
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

            // Fetch the save dialog.
            var dialog = root.Q<ModalPopup>("save-dialog");

            // Fetch the tooltip preview.
            var tooltipPreview = root.Q<BuilderTooltipPreview>("tooltip-preview");

            // Create selection.
            m_Selection = new BuilderSelection(root, this);

            // Create viewport first.
            m_Viewport = new BuilderViewport(this, selection);
            selection.documentElement = m_Viewport.documentElement;

            // Create the rest of the panes.
            var classDragger = new BuilderClassDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker);
            var hierarchyDragger = new BuilderHierarchyDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker);
            var contextMenuManipulator = new BuilderExplorerContextMenu(this, selection);
            var explorer = new BuilderExplorer(m_Viewport, selection, classDragger, hierarchyDragger, contextMenuManipulator);
            var libraryDragger = new BuilderLibraryDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker, explorer.container, tooltipPreview);
            m_Library = new BuilderLibrary(this, m_Viewport, selection, libraryDragger, tooltipPreview);
            m_Inspector = new BuilderInspector(this, selection);
            m_Toolbar = new BuilderToolbar(this, selection, dialog, m_Viewport, explorer, m_Library, m_Inspector, tooltipPreview);
            m_UxmlPreview = new BuilderUxmlPreview(this);
            m_UssPreview = new BuilderUssPreview(this);
            root.Q("viewport").Add(m_Viewport);
            m_Viewport.toolbar.Add(m_Toolbar);
            root.Q("library").Add(m_Library);
            root.Q("explorer").Add(explorer);
            root.Q("uxml-preview").Add(m_UxmlPreview);
            root.Q("uss-preview").Add(m_UssPreview);
            root.Q("inspector").Add(m_Inspector);

            // Init selection.
            selection.AssignNotifiers(new IBuilderSelectionNotifier[]
            {
                document,
                m_Viewport,
                explorer,
                m_Inspector,
                m_UxmlPreview,
                m_UssPreview,
                m_Viewport.parentTracker,
                m_Viewport.resizer,
                m_Viewport.mover,
                m_Viewport.anchorer
            });

            // Command Handler
            commandHandler.RegisterPane(explorer);
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

            var builder = GetWindowAndInit<Builder>(BuilderConstants.BuilderWindowTitle);

            builder.LoadDocument(asset);

            return true;
        }
    }
}
