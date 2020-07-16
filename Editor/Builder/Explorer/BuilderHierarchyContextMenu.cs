using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderHierarchyContextMenu : BuilderElementContextMenu
    {
        public BuilderHierarchyContextMenu(BuilderPaneWindow paneWindow, BuilderSelection selection)
            : base(paneWindow, selection)
        {}

        public override void BuildElementContextualMenu(ContextualMenuPopulateEvent evt, VisualElement target)
        {
            base.BuildElementContextualMenu(evt, target);
            var documentElement = target.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
            var vta = documentElement?.GetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName) as VisualTreeAsset;

            if (vta != null)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction(
                    BuilderConstants.ExplorerHierarchyOpenInBuilder,
                    action =>
                    {
                        paneWindow.LoadDocument(vta);
                    });

                evt.menu.AppendAction(
                    BuilderConstants.ExplorerHierarchyPaneOpenSubDocument,
                    action =>
                    {
                        BuilderHierarchyUtilities.OpenAsSubDocument(paneWindow, vta);
                    });
            }

            var activeDocumentsParentIndex = document.activeOpenUXMLFile.openSubDocumentParentIndex;
            if (documentElement == null && activeDocumentsParentIndex > -1)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction(
                    BuilderConstants.ExplorerHierarchyReturnToParentDocument,
                    action =>
                    {
                        var parentDocument = document.openUXMLFiles[activeDocumentsParentIndex];
                        document.GoToSubdocument(documentElement, paneWindow, parentDocument);
                    });
            }
        }
    }
}
