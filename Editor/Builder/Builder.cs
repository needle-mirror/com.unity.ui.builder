using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class Builder : EditorWindow
    {
        BuilderSelection m_Selection;
        BuilderDocument m_Document;

        BuilderToolbar m_Toolbar;
        BuilderViewport m_Viewport;
        BuilderUxmlPreview m_UxmlPreview;
        BuilderUssPreview m_UssPreview;
        BuilderCommandHandler m_CommandHandler;

        public BuilderDocument document
        {
            get
            {
                // Find or create document.
                if (m_Document == null)
                {
                    var allDocuments = Resources.FindObjectsOfTypeAll(typeof(BuilderDocument));
                    if (allDocuments.Length > 1)
                        Debug.LogError("UIBuilder: More than one BuilderDocument was somehow created!");
                    if (allDocuments.Length == 0)
                        m_Document = BuilderDocument.CreateInstance();
                    else
                        m_Document = allDocuments[0] as BuilderDocument;
                }

                return m_Document;
            }
        }

        public BuilderCommandHandler commandHandler
        {
            get { return m_CommandHandler; }
        }

        public static Builder GetWindowAndInit()
        {
            var window = GetWindow<Builder>();
            window.titleContent = new GUIContent("Builder");
            window.Show();
            return window;
        }

        [MenuItem("Window/UI/UI Builder")]
        public static void ShowWindow()
        {
            GetWindowAndInit();
        }

        private void OnEnable()
        {
            var root = rootVisualElement;

            // Load styles.
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/Builder/Builder.uss"));
            if (EditorGUIUtility.isProSkin)
                root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/Builder/BuilderDark.uss"));
            else
                root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/Builder/BuilderLight.uss"));

            // Load template.
            var builderTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/Builder/Builder.uxml");
            builderTemplate.CloneTree(root);

            // Fetch the save dialog.
            var dialog = root.Q<ModalPopup>("save-dialog");

            // Fetch the tooltip preview.
            var tooltipPreview = root.Q<BuilderTooltipPreview>("tooltip-preview");

            // Create selection.
            m_Selection = new BuilderSelection(root, this);

            // Create viewport first.
            m_Viewport = new BuilderViewport(this, m_Selection);
            m_Selection.documentElement = m_Viewport.documentElement;

            // Create the rest of the panes.
            var classDragger = new BuilderClassDragger(this, root, m_Selection, m_Viewport, m_Viewport.parentTracker);
            var hierarchyDragger = new BuilderHierarchyDragger(this, root, m_Selection, m_Viewport, m_Viewport.parentTracker);
            var contextMenuManipulator = new BuilderExplorerContextMenu(this, m_Selection);
            var explorer = new BuilderExplorer(m_Viewport, m_Selection, classDragger, hierarchyDragger, contextMenuManipulator);
            var libraryDragger = new BuilderLibraryDragger(this, root, m_Selection, m_Viewport, m_Viewport.parentTracker, explorer.container, tooltipPreview);
            m_Toolbar = new BuilderToolbar(this, m_Selection, dialog, m_Viewport, explorer, tooltipPreview);
            var library = new BuilderLibrary(this, m_Viewport, m_Toolbar, m_Selection, libraryDragger, tooltipPreview);
            m_UxmlPreview = new BuilderUxmlPreview(this, m_Viewport, m_Selection);
            m_UssPreview = new BuilderUssPreview(this);
            var inspector = new BuilderInspector(this, m_Selection);
            root.Q("viewport").Add(m_Viewport);
            m_Viewport.toolbar.Add(m_Toolbar);
            root.Q("library").Add(library);
            root.Q("explorer").Add(explorer);
            root.Q("uxml-preview").Add(m_UxmlPreview);
            root.Q("uss-preview").Add(m_UssPreview);
            root.Q("inspector").Add(inspector);

            // Init selection.
            m_Selection.AssignNotifiers(new IBuilderSelectionNotifier[]
            {
                document,
                m_Viewport,
                explorer,
                inspector,
                m_UxmlPreview,
                m_UssPreview,
                m_Viewport.parentTracker,
                m_Viewport.resizer,
                m_Viewport.mover,
                m_Viewport.anchorer
            });

            // Command Handler
            m_CommandHandler = new BuilderCommandHandler(this, explorer, m_Viewport, m_Toolbar, m_Selection);
            m_CommandHandler.OnEnable();

            OnEnableAfterAllSerialization();
        }

        public void OnEnableAfterAllSerialization()
        {
            // Perform post-serialization functions.
            m_Document.OnAfterBuilderDeserialize(m_Viewport.documentElement);
            m_Toolbar.OnAfterBuilderDeserialize();

            // Restore selection.
            m_Selection.RestoreSelectionFromDocument(m_Viewport.sharedStylesAndDocumentElement);

            // We claim the change is coming from the Document because we don't
            // want the document hasUnsavedChanges flag to be set at this time.
            m_Selection.NotifyOfStylingChange(m_Document);
            m_Selection.NotifyOfHierarchyChange(m_Document);
        }

        private void OnDisable()
        {
            // Commands
            if (m_CommandHandler != null)
                m_CommandHandler.OnDisable();
        }

        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as VisualTreeAsset;
            if (asset == null)
                return false;

            var builder = GetWindowAndInit();

            builder.m_Toolbar.LoadDocument(asset);

            return true;
        }
    }
}
