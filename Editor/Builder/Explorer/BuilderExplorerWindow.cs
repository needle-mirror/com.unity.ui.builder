using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderExplorerWindow : BuilderPaneWindow
    {
        BuilderExplorer m_Explorer;

        //[MenuItem("Window/UI/UI Builder Explorer")]
        public static void ShowWindow()
        {
            GetWindowAndInit<BuilderExplorerWindow>("UI Builder Explorer");
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            var viewportWindow = document.primaryViewportWindow;
            if (viewportWindow == null)
                return;

            var selection = viewportWindow.selection;
            var viewport = viewportWindow.viewport;

            var classDragger = new BuilderClassDragger(this, root, selection, viewport, viewport.parentTracker);
            var hierarchyDragger = new BuilderHierarchyDragger(this, root, selection, viewport, viewport.parentTracker);
            var contextMenuManipulator = new BuilderExplorerContextMenu(this, selection);

            m_Explorer = new BuilderExplorer(viewport, selection, classDragger, hierarchyDragger, contextMenuManipulator);

            selection.AddNotifier(m_Explorer);

            root.Add(m_Explorer);

            // Command Handler
            commandHandler.RegisterPane(m_Explorer);
        }

        public override void ClearUI()
        {
            if (m_Explorer == null)
                return;

            var selection = document.primaryViewportWindow?.selection;
            if (selection != null)
                selection.RemoveNotifier(m_Explorer);

            m_Explorer.RemoveFromHierarchy();
            m_Explorer = null;
        }
    }
}
