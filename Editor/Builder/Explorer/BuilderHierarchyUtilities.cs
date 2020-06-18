using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class BuilderHierarchyUtilities
    {

        public static bool OpenAsSubDocument(BuilderPaneWindow paneWindow, VisualTreeAsset vta)
        {
            bool didSaveChanges = paneWindow.document.CheckForUnsavedChanges();
            if (!didSaveChanges)
                return false;
            paneWindow.document.AddSubDocument();
            paneWindow.LoadDocument(vta, false);

            return true;
        }
    }
}