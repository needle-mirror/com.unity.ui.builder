using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderViewportWindow : BuilderPaneWindow, IBuilderViewportWindow
    {
        BuilderSelection m_Selection;

        BuilderToolbar m_Toolbar;
        BuilderViewport m_Viewport;

        public BuilderSelection selection => m_Selection;

        public BuilderViewport viewport => m_Viewport;
        public VisualElement documentRootElement => m_Viewport.documentElement;
        public BuilderCanvas canvas => m_Viewport.canvas;

        //[MenuItem("Window/UI/UI Builder Viewport")]
        public static void ShowWindow()
        {
            GetWindowAndInit<BuilderViewportWindow>("UI Builder Viewport");
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            // Load assets.
            var saveDialogTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/BuilderSaveDialog.uxml");

            // Load template.
            saveDialogTemplate.CloneTree(root);

            // Fetch the save dialog.
            var dialog = root.Q<ModalPopup>("save-dialog");

            // Create selection.
            m_Selection = new BuilderSelection(root, this);

            // Create viewport first.
            m_Viewport = new BuilderViewport(this, selection, null);
            selection.documentElement = m_Viewport.documentElement;

            // Create the rest of the panes.
            m_Toolbar = new BuilderToolbar(this, selection, dialog, m_Viewport, null, null, null, null);
            root.Add(m_Viewport);
            m_Viewport.toolbar.Add(m_Toolbar);

            // Init selection.
            selection.AssignNotifiers(new IBuilderSelectionNotifier[]
            {
                document,
                m_Viewport,
                m_Viewport.parentTracker,
                m_Viewport.resizer,
                m_Viewport.mover,
                m_Viewport.anchorer
            });

            // Command Handler
            commandHandler.RegisterPane(m_Viewport);
            commandHandler.RegisterToolbar(m_Toolbar);

            dialog.BringToFront();

            OnEnableAfterAllSerialization();
        }

        public override void OnEnableAfterAllSerialization()
        {
            // Perform post-serialization functions.
            document.OnAfterBuilderDeserialize(m_Viewport.documentElement);
            m_Toolbar.OnAfterBuilderDeserialize();

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
    }
}
