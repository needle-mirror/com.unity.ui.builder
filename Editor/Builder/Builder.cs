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
        BuilderLibrary m_Library;
        BuilderViewport m_Viewport;
        BuilderInspector m_Inspector;
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

        public VisualElement documentRootElement => m_Viewport.documentElement;
        public BuilderCanvas canvas => m_Viewport.canvas;

        public BuilderCommandHandler commandHandler
        {
            get { return m_CommandHandler; }
        }

        public static Builder GetWindowAndInit()
        {
            var window = GetWindow<Builder>();
            window.titleContent = new GUIContent(BuilderConstants.BuilderWindowTitle);
            window.Show();
            return window;
        }

        [MenuItem("Window/UI/UI Builder")]
        public static void ShowWindow()
        {
            GetWindowAndInit();
        }

        void OnEnable()
        {
            var root = rootVisualElement;

            // Load assets.
            var mainSS = AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/Builder.uss");
            var darkSS = AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/BuilderDark.uss");
            var lightSS = AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/BuilderLight.uss");
            var builderTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/Builder.uxml");

            // HACK: Check for null assets.
            // See: https://fogbugz.unity3d.com/f/cases/1180330/
            if (mainSS == null || darkSS == null || lightSS == null || builderTemplate == null)
            {
                EditorApplication.delayCall += () =>
                {
                    this.m_Parent.Reload(this);
                };
                return;
            }

            // Load styles.
            root.styleSheets.Add(mainSS);
            if (EditorGUIUtility.isProSkin)
                root.styleSheets.Add(darkSS);
            else
                root.styleSheets.Add(lightSS);

            // Load template.
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
            m_Library = new BuilderLibrary(this, m_Viewport, m_Selection, libraryDragger, tooltipPreview);
            m_Inspector = new BuilderInspector(this, m_Selection);
            m_Toolbar = new BuilderToolbar(this, m_Selection, dialog, m_Viewport, explorer, m_Library, m_Inspector, tooltipPreview);
            m_UxmlPreview = new BuilderUxmlPreview(this, m_Viewport, m_Selection);
            m_UssPreview = new BuilderUssPreview(this);
            root.Q("viewport").Add(m_Viewport);
            m_Viewport.toolbar.Add(m_Toolbar);
            root.Q("library").Add(m_Library);
            root.Q("explorer").Add(explorer);
            root.Q("uxml-preview").Add(m_UxmlPreview);
            root.Q("uss-preview").Add(m_UssPreview);
            root.Q("inspector").Add(m_Inspector);

            // Init selection.
            m_Selection.AssignNotifiers(new IBuilderSelectionNotifier[]
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
            m_CommandHandler = new BuilderCommandHandler(this, explorer, m_Viewport, m_Toolbar, m_Selection);
            m_CommandHandler.OnEnable();

            OnEnableAfterAllSerialization();
        }

        public void OnEnableAfterAllSerialization()
        {
            // Perform post-serialization functions.
            m_Document.OnAfterBuilderDeserialize(m_Viewport.documentElement);
            m_Toolbar.OnAfterBuilderDeserialize();
            m_Library.OnAfterBuilderDeserialize();
            m_Inspector.OnAfterBuilderDeserialize();

            // Restore selection.
            m_Selection.RestoreSelectionFromDocument(m_Viewport.sharedStylesAndDocumentElement);

            // We claim the change is coming from the Document because we don't
            // want the document hasUnsavedChanges flag to be set at this time.
            m_Selection.NotifyOfStylingChange(m_Document);
            m_Selection.NotifyOfHierarchyChange(m_Document);
        }

        public void LoadDocument(VisualTreeAsset asset)
        {
            m_Toolbar.LoadDocument(asset);
        }

        void OnDisable()
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

            builder.LoadDocument(asset);

            return true;
        }
    }
}
