using System;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BuilderExplorer : BuilderPaneContent, IBuilderSelectionNotifier
    {
        static readonly string s_UssClassName = "unity-builder-explorer";

        // TODO: Transfer to own USS.
        const string k_DefaultStyleSheetPath = "StyleSheets/UIElementsDebugger/UIElementsDebugger.uss";
        const string k_DefaultDarkStyleSheetPath = "StyleSheets/UIElementsDebugger/UIElementsDebuggerDark.uss";
        const string k_DefaultLightStyleSheetPath = "StyleSheets/UIElementsDebugger/UIElementsDebuggerLight.uss";

        [Flags]
        internal enum BuilderElementInfoVisibilityState
        {
            TypeName = 1 << 0,
            ClassList = 1 << 1,

            All = ~0
        }

        VisualElement m_SharedStylesAndDocumentElement;
        VisualElement m_DocumentElement;
        ElementHierarchyView m_ElementHierarchyView;
        BuilderSelection m_Selection;
        bool m_SelectionMadeExternally;

        BuilderClassDragger m_ClassDragger;
        BuilderHierarchyDragger m_HierarchyDragger;
        BuilderExplorerContextMenu m_ContextMenuManipulator;

        ToolbarMenu m_HierarchyTypeClassVisibilityMenu;
        [SerializeField] BuilderElementInfoVisibilityState m_ElementInfoVisibilityState;

        public VisualElement container
        {
            get { return m_ElementHierarchyView.container; }
        }

        public BuilderExplorer(BuilderViewport viewport, BuilderSelection selection,
            BuilderClassDragger classDragger, BuilderHierarchyDragger hierarchyDragger,
            BuilderExplorerContextMenu contextMenuManipulator)
        {
            m_SharedStylesAndDocumentElement = viewport.sharedStylesAndDocumentElement;
            m_DocumentElement = viewport.documentElement;
            AddToClassList(s_UssClassName);

            m_ClassDragger = classDragger;
            m_HierarchyDragger = hierarchyDragger;
            m_ContextMenuManipulator = contextMenuManipulator;

            m_SelectionMadeExternally = false;

            m_Selection = selection;

            // TODO: Transfer to own USS.
            var sheet = EditorGUIUtility.Load(k_DefaultStyleSheetPath) as StyleSheet;
            styleSheets.Add(sheet);
            StyleSheet colorSheet;
            if (EditorGUIUtility.isProSkin)
                colorSheet = EditorGUIUtility.Load(k_DefaultDarkStyleSheetPath) as StyleSheet;
            else
                colorSheet = EditorGUIUtility.Load(k_DefaultLightStyleSheetPath) as StyleSheet;
            styleSheets.Add(colorSheet);

            // Query the UI
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderExplorerToolbar.uxml");
            template.CloneTree(this);

            viewDataKey = "builder-explorer";
            m_HierarchyTypeClassVisibilityMenu = this.Q<ToolbarMenu>("hierarchy-visibility-toolbar-menu");
            SetUpHierarchyVisibilityMenu();

            // Get the overlay helper.
            var overlayHelper = viewport.Q<OverlayPainterHelperElement>();

            // Create the Hierarchy View.
            m_ElementHierarchyView = new ElementHierarchyView(
                m_DocumentElement,
                selection, classDragger, hierarchyDragger,
                contextMenuManipulator, ElementSelected, overlayHelper);
            m_ElementHierarchyView.style.flexGrow = 1;
            Add(m_ElementHierarchyView);

            // Make sure the Hierarchy View gets focus when the pane gets focused.
            primaryFocusable = m_ElementHierarchyView.Q<ListView>();

            UpdateHierarchyAndSelection();
        }

        public void ClearHighlightOverlay()
        {
            m_ElementHierarchyView.ClearHighlightOverlay();
        }

        public void ResetHighlightOverlays()
        {
            m_ElementHierarchyView.ResetHighlightOverlays();
        }

        void ElementSelected(VisualElement element)
        {
            if (m_SelectionMadeExternally)
                return;

            if (element == null)
            {
                m_Selection.ClearSelection(this);
                return;
            }
            else if (element.ClassListContains(BuilderConstants.ExplorerItemUnselectableClassName))
            {
                m_SelectionMadeExternally = true;
                m_ElementHierarchyView.ClearSelection();
                m_SelectionMadeExternally = false;
                m_Selection.ClearSelection(this);
                return;
            }

            m_Selection.Select(this, element);
        }

        public void UpdateHierarchyAndSelection()
        {
            m_SelectionMadeExternally = true;

            m_ElementHierarchyView.hierarchyHasChanged = true;
            m_ElementHierarchyView.RebuildTree(m_SharedStylesAndDocumentElement);

            if (!m_Selection.isEmpty)
            {
                m_ElementHierarchyView.SelectElement(m_Selection.selection.First());
                m_ElementHierarchyView.IncrementVersion(VersionChangeType.Styles);
            }

            m_SelectionMadeExternally = false;
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            if (element == null ||
                changeType.HasFlag(BuilderHierarchyChangeType.ChildrenAdded) ||
                changeType.HasFlag(BuilderHierarchyChangeType.ChildrenRemoved) ||
                changeType.HasFlag(BuilderHierarchyChangeType.Name) ||
                changeType.HasFlag(BuilderHierarchyChangeType.ClassList))
            {
                UpdateHierarchyAndSelection();
            }
        }

        public void SelectionChanged()
        {
            if (m_Selection.selection.Count() > 0)
            {
                m_SelectionMadeExternally = true;
                m_ElementHierarchyView.SelectElement(m_Selection.selection.First());
                m_SelectionMadeExternally = false;
            }
            else
                m_ElementHierarchyView.ClearSelection();
        }

        public void StylingChanged(List<string> styles)
        {

        }

        public void SetUpHierarchyVisibilityMenu()
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
