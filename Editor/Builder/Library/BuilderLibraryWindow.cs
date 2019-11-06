using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderLibraryWindow : BuilderPaneWindow
    {
        BuilderLibrary m_Library;

        //[MenuItem("Window/UI/UI Builder Library")]
        public static void ShowWindow()
        {
            GetWindowAndInit<BuilderLibraryWindow>("UI Builder Library");
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            var viewportWindow = document.primaryViewportWindow;
            if (viewportWindow == null)
                return;

            var selection = viewportWindow.selection;
            var viewport = viewportWindow.viewport;

            m_Library = new BuilderLibrary(this, viewport, selection, null, null);

            root.Add(m_Library);
        }

        public override void ClearUI()
        {
            if (m_Library == null)
                return;

            m_Library.RemoveFromHierarchy();
            m_Library = null;
        }

        public override void OnEnableAfterAllSerialization()
        {
            // Perform post-serialization functions.
            m_Library.OnAfterBuilderDeserialize();
        }
    }
}