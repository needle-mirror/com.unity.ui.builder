using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheetsContextMenu : BuilderElementContextMenu
    {
        public BuilderStyleSheetsContextMenu(BuilderPaneWindow paneWindow, BuilderSelection selection)
            : base(paneWindow, selection)
        {}

        public override void BuildElementContextualMenu(ContextualMenuPopulateEvent evt, VisualElement target)
        {
            base.BuildElementContextualMenu(evt, target);

            var documentElement = target.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;

            var selectedStyleSheet = documentElement?.GetStyleSheet();
            int selectedStyleSheetIndex = selectedStyleSheet == null ? -1 : (int)documentElement.GetProperty(BuilderConstants.ElementLinkedStyleSheetIndexVEPropertyName);
            var isStyleSheet = documentElement != null && BuilderSharedStyles.IsStyleSheetElement(documentElement);
            var styleSheetBelongsToParent = !string.IsNullOrEmpty(documentElement?.GetProperty(BuilderConstants.ExplorerItemLinkedUXMLFileName) as string);
            if (isStyleSheet)
                evt.StopImmediatePropagation();

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                BuilderConstants.ExplorerStyleSheetsPaneCreateNewUSSMenu,
                a =>
                {
                    BuilderStyleSheetsUtilities.CreateNewUSSAsset(paneWindow);
                },
#if UNITY_2019_4
                // Cannot add USS to an empty UXML because there's no root element to
                // containe the <Style> tag. This will problem will go away once
                // we support the root <Style> tag but...one problem at a time.
                !document.visualTreeAsset.IsEmpty()
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled);
#else
                DropdownMenuAction.Status.Normal);
#endif

            evt.menu.AppendAction(
                BuilderConstants.ExplorerStyleSheetsPaneAddExistingUSSMenu,
                a =>
                {
                    BuilderStyleSheetsUtilities.AddExistingUSSToAsset(paneWindow);
                },
#if UNITY_2019_4
                // Cannot add USS to an empty UXML because there's no root element to
                // containe the <Style> tag. This will problem will go away once
                // we support the root <Style> tag but...one problem at a time.
                !document.visualTreeAsset.IsEmpty()
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled);
#else
                DropdownMenuAction.Status.Normal);
#endif

            evt.menu.AppendAction(
                BuilderConstants.ExplorerStyleSheetsPaneRemoveUSSMenu,
                a =>
                {
                    BuilderStyleSheetsUtilities.RemoveUSSFromAsset(paneWindow, selection, documentElement);
                },
                isStyleSheet && !styleSheetBelongsToParent
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                BuilderConstants.ExplorerStyleSheetsPaneSetActiveUSS,
                a =>
                {
                    selection.Select(null, documentElement);
                    BuilderStyleSheetsUtilities.SetActiveUSS(selection, paneWindow, selectedStyleSheet);
                },
                isStyleSheet && !styleSheetBelongsToParent
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled);
        }
    }
}
