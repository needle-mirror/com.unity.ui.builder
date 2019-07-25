using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderExplorer : BuilderPaneContent, IBuilderSelectionNotifier
    {
        private static readonly string s_UssClassName = "unity-builder-explorer";

        // TODO: Transfer to own USS.
        const string k_DefaultStyleSheetPath = "StyleSheets/UIElementsDebugger/UIElementsDebugger.uss";
        const string k_DefaultDarkStyleSheetPath = "StyleSheets/UIElementsDebugger/UIElementsDebuggerDark.uss";
        const string k_DefaultLightStyleSheetPath = "StyleSheets/UIElementsDebugger/UIElementsDebuggerLight.uss";

        VisualElement m_SharedStylesAndDocumentElement;
        VisualElement m_DocumentElement;
        ElementHierarchyView m_ElementHierarchyView;
        BuilderSelection m_Selection;
        bool m_SelectionMadeExternally;

        BuilderClassDragger m_ClassDragger;
        BuilderHierarchyDragger m_HierarchyDragger;

        public VisualElement container
        {
            get { return m_ElementHierarchyView.container; }
        }

        public BuilderExplorer(BuilderViewport viewport, BuilderSelection selection,
            BuilderClassDragger classDragger, BuilderHierarchyDragger hierarchyDragger)
        {
            m_SharedStylesAndDocumentElement = viewport.sharedStylesAndDocumentElement;
            m_DocumentElement = viewport.documentElement;
            AddToClassList(s_UssClassName);

            m_ClassDragger = classDragger;
            m_HierarchyDragger = hierarchyDragger;

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

            // Get the overlay helper.
            var overlayHelper = viewport.Q<OverlayPainterHelperElement>();

            // Create the Hierarchy View.
            m_ElementHierarchyView = new ElementHierarchyView(
                classDragger, hierarchyDragger, ElementSelected, overlayHelper);
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

        private void ElementSelected(VisualElement element)
        {
            if (m_SelectionMadeExternally)
                return;

            if (element == null)
            {
                m_Selection.ClearSelection(this);
                return;
            }

            if (!element.IsLinkedToAsset())
            {
                element = element.GetClosestElementPartOfCurrentDocument();

                m_SelectionMadeExternally = true;
                m_ElementHierarchyView.SelectElement(element);
                m_SelectionMadeExternally = false;
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
    }
}
