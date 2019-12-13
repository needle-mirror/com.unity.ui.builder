using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BuilderHierarchy : BuilderExplorer, IBuilderSelectionNotifier
    {
        static readonly string kToolbarPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderExplorerToolbar.uxml";

        ToolbarMenu m_HierarchyTypeClassVisibilityMenu;
        [SerializeField] BuilderElementInfoVisibilityState m_ElementInfoVisibilityState;

        public BuilderHierarchy(
            BuilderViewport viewport,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderHierarchyDragger hierarchyDragger,
            BuilderElementContextMenu contextMenuManipulator,
            HighlightOverlayPainter highlightOverlayPainter)
            : base(
                  viewport,
                  selection,
                  classDragger,
                  hierarchyDragger,
                  contextMenuManipulator,
                  viewport.documentElement,
                  highlightOverlayPainter,
                  kToolbarPath)
        {
            viewDataKey = "builder-hierarchy";

            m_HierarchyTypeClassVisibilityMenu = this.Q<ToolbarMenu>("hierarchy-visibility-toolbar-menu");
            SetUpHierarchyVisibilityMenu();
        }

        protected override bool IsSelectedItemValid(VisualElement element)
        {
            var isVEA = element.GetVisualElementAsset() != null;
            var isVTA = element.GetVisualTreeAsset() != null;

            return isVEA || isVTA;
        }

        void SetUpHierarchyVisibilityMenu()
        {
            m_HierarchyTypeClassVisibilityMenu.menu.AppendAction("Type",
                a => ChangeVisibilityState(BuilderElementInfoVisibilityState.TypeName),
                a => m_ElementInfoVisibilityState
                .HasFlag(BuilderElementInfoVisibilityState.TypeName)
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal);

            m_HierarchyTypeClassVisibilityMenu.menu.AppendAction("Class List",
                a => ChangeVisibilityState(BuilderElementInfoVisibilityState.ClassList),
                a => m_ElementInfoVisibilityState
                .HasFlag(BuilderElementInfoVisibilityState.ClassList)
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal);
        }

        void ChangeVisibilityState(BuilderElementInfoVisibilityState state)
        {
            m_ElementInfoVisibilityState ^= state;
            m_ElementHierarchyView.elementInfoVisibilityState = m_ElementInfoVisibilityState;
            SaveViewData();
            UpdateHierarchyAndSelection();
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            OverwriteFromViewData(this, viewDataKey);
            m_ElementHierarchyView.elementInfoVisibilityState = m_ElementInfoVisibilityState;
        }
    }
}
