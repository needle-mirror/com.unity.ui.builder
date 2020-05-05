using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheetsWindow : BuilderPaneWindow
    {
        BuilderStyleSheets m_StyleSheetsPane;

        //[MenuItem(BuilderConstants.BuilderMenuEntry + " StyleSheets")]
        public static void ShowWindow()
        {
            GetWindowAndInit<BuilderStyleSheetsWindow>("UI Builder StyleSheets");
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

            m_StyleSheetsPane = new BuilderStyleSheets(this, viewport, selection, classDragger, hierarchyDragger, null, null);

            selection.AddNotifier(m_StyleSheetsPane);

            root.Add(m_StyleSheetsPane);

            // Command Handler
            commandHandler.RegisterPane(m_StyleSheetsPane);
        }

        public override void ClearUI()
        {
            if (m_StyleSheetsPane == null)
                return;

            var selection = document.primaryViewportWindow?.selection;
            if (selection != null)
                selection.RemoveNotifier(m_StyleSheetsPane);

            m_StyleSheetsPane.RemoveFromHierarchy();
            m_StyleSheetsPane = null;
        }
    }
}
