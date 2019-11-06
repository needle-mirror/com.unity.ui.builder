using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderViewport : BuilderPaneContent, IBuilderSelectionNotifier
    {
        static readonly string s_PreviewModeClassName = "unity-builder-viewport--preview";
        static readonly float s_CanvasViewportMinWidthDiff = 30;
        static readonly float s_CanvasViewportMinHeightDiff = 36;

        BuilderPaneWindow m_PaneWindow;

        VisualElement m_Toolbar;
        VisualElement m_ViewportWrapper;
        VisualElement m_Viewport;
        BuilderCanvas m_Canvas;
        VisualElement m_SharedStylesAndDocumentElement;
        VisualElement m_DocumentElement;
        VisualElement m_PickOverlay;
        VisualElement m_HighlightOverlay;
        BuilderParentTracker m_BuilderParentTracker;
        BuilderResizer m_BuilderResizer;
        BuilderMover m_BuilderMover;
        BuilderAnchorer m_BuilderAnchorer;
        Button m_FitCanvasButton;

        BuilderSelection m_Selection;

        List<VisualElement> m_MatchingExplorerItems = new List<VisualElement>();

        public VisualElement toolbar
        {
            get { return m_Toolbar; }
        }

        public BuilderCanvas canvas => m_Canvas;

        string m_SubTitle;
        public string subTitle
        {
            get
            {
                if (pane == null)
                    return m_SubTitle;
                else
                    return pane.subTitle;
            }
            set
            {
                m_SubTitle = value;
                if (pane != null)
                    pane.subTitle = value;
            }
        }

        public BuilderParentTracker parentTracker
        {
            get { return m_BuilderParentTracker; }
        }

        public BuilderResizer resizer
        {
            get { return m_BuilderResizer; }
        }

        public BuilderMover mover
        {
            get { return m_BuilderMover; }
        }

        public BuilderAnchorer anchorer
        {
            get { return m_BuilderAnchorer; }
        }

        public VisualElement sharedStylesAndDocumentElement => m_SharedStylesAndDocumentElement;
        public VisualElement documentElement => m_DocumentElement;
        public VisualElement pickOverlay => m_PickOverlay;
        public VisualElement highlightOverlay => m_HighlightOverlay;

        public BuilderViewport(BuilderPaneWindow paneWindow, BuilderSelection selection)
        {
            m_PaneWindow = paneWindow;
            m_Selection = selection;

            AddToClassList("unity-builder-viewport");

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/BuilderViewport.uxml");
            template.CloneTree(this);

            m_Toolbar = this.Q("toolbar");
            m_ViewportWrapper = this.Q("viewport-wrapper");
            m_Viewport = this.Q("viewport");
            m_Canvas = this.Q<BuilderCanvas>("canvas");
            m_Canvas.document = paneWindow.document;
            m_SharedStylesAndDocumentElement = this.Q("shared-styles-and-document");
            m_DocumentElement = this.Q("document");
            m_PickOverlay = this.Q("pick-overlay");
            m_HighlightOverlay = this.Q("highlight-overlay");
            m_BuilderParentTracker = this.Q<BuilderParentTracker>("parent-tracker");
            m_BuilderResizer = this.Q<BuilderResizer>("resizer");
            m_BuilderMover = this.Q<BuilderMover>("mover");
            m_BuilderAnchorer = this.Q<BuilderAnchorer>("anchorer");
            m_FitCanvasButton = this.Q<Button>("fit-canvas-button");

            m_FitCanvasButton.clickable.clicked += FitCanvas;
            m_Canvas.RegisterCallback<GeometryChangedEvent>(VerifyCanvasStillFitsViewport);
            m_Viewport.RegisterCallback<GeometryChangedEvent>(VerifyCanvasStillFitsViewport);

            m_BuilderMover.parentTracker = m_BuilderParentTracker;

            m_PickOverlay.RegisterCallback<MouseDownEvent>(OnPick);
            m_PickOverlay.RegisterCallback<MouseMoveEvent>(OnHover);
            m_PickOverlay.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            m_Viewport.RegisterCallback<MouseDownEvent>(OnMissPick);

            // Make sure this gets focus when the pane gets focused.
            primaryFocusable = this;
            this.focusable = true;
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (pane == null)
                return;

            pane.subTitle = m_SubTitle;
        }

        void FitCanvas()
        {
            var maxCanvasWidth = m_Viewport.resolvedStyle.width - s_CanvasViewportMinWidthDiff;
            var maxCanvasHeight = m_Viewport.resolvedStyle.height - s_CanvasViewportMinHeightDiff;

            var currentWidth = m_Canvas.resolvedStyle.width;
            var currentHeight = m_Canvas.resolvedStyle.height;

            if (currentWidth > maxCanvasWidth)
                m_Canvas.width = maxCanvasWidth;

            if (currentHeight > maxCanvasHeight)
                m_Canvas.height = maxCanvasHeight;
        }

        void VerifyCanvasStillFitsViewport(GeometryChangedEvent evt)
        {
            float viewportWidth;
            float viewportHeight;

            float canvasWidth;
            float canvasHeight;

            if (evt.target == m_Viewport)
            {
                viewportWidth = evt.newRect.width;
                viewportHeight = evt.newRect.height;

                canvasWidth = m_Canvas.resolvedStyle.width;
                canvasHeight = m_Canvas.resolvedStyle.height;
            }
            else
            {
                viewportWidth = m_Viewport.resolvedStyle.width;
                viewportHeight = m_Viewport.resolvedStyle.height;

                canvasWidth = evt.newRect.width;
                canvasHeight = evt.newRect.height;
            }

            var maxCanvasWidth = viewportWidth - s_CanvasViewportMinWidthDiff;
            var maxCanvasHeight = viewportHeight - s_CanvasViewportMinHeightDiff;

            if (canvasWidth > maxCanvasWidth || canvasHeight > maxCanvasHeight)
                m_FitCanvasButton.style.display = DisplayStyle.Flex;
            else
                m_FitCanvasButton.style.display = DisplayStyle.None;
        }

        VisualElement PickElement(Vector2 mousePosition)
        {
            var pickedElement = Panel.PickAllWithoutValidatingLayout(m_DocumentElement, mousePosition);

            if (pickedElement == null)
                return null;

            if (pickedElement == m_DocumentElement)
                return null;

            // Don't allow selection of elements inside template instances.
            pickedElement = pickedElement.GetClosestElementPartOfCurrentDocument();

            return pickedElement;
        }

        void OnPick(MouseDownEvent evt)
        {
            var pickedElement = PickElement(evt.mousePosition);

            if (pickedElement != null)
            {
                SetInnerSelection(pickedElement);
                m_Selection.Select(this, pickedElement);
            }
            else
            {
                ClearInnerSelection();
                m_Selection.ClearSelection(this);
            }

            evt.StopPropagation();
        }

        void ClearMatchingExplorerItems()
        {
            foreach (var item in m_MatchingExplorerItems)
                item.RemoveFromClassList(BuilderConstants.ExplorerItemHoverClassName);

            m_MatchingExplorerItems.Clear();
        }

        void HighlightMatchingExplorerItems()
        {
            foreach (var item in m_MatchingExplorerItems)
                item.AddToClassList(BuilderConstants.ExplorerItemHoverClassName);
        }

        void OnHover(MouseMoveEvent evt)
        {
            var pickedElement = PickElement(evt.mousePosition);

            if (pickedElement != null)
            {
                // Don't allow selection of elements inside template instances.
                pickedElement = pickedElement.GetClosestElementPartOfCurrentDocument();

                parentTracker.Activate(pickedElement);

                ClearMatchingExplorerItems();

                // Highlight corresponding element in Explorer (if visible).
                var explorerItem = pickedElement.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;
                var explorerItemRow = explorerItem?.row();
                if (explorerItemRow != null)
                    m_MatchingExplorerItems.Add(explorerItemRow);

                // Highlight matching selectors in the Explorer (if visible).
                var matchingSelectors = BuilderSharedStyles.GetMatchingSelectorsOnElement(pickedElement);
                if (matchingSelectors != null)
                {
                    foreach (var selectorStr in matchingSelectors)
                    {
                        var selectorElement = BuilderSharedStyles.FindSelectorElement(m_DocumentElement, selectorStr);
                        if (selectorElement == null)
                            continue;

                        var selectorItem = selectorElement.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;
                        var selectorItemRow = selectorItem?.row();
                        if (selectorItemRow == null)
                            continue;

                        m_MatchingExplorerItems.Add(selectorItemRow);
                    }
                }

                HighlightMatchingExplorerItems();
            }
            else
            {
                parentTracker.Deactivate();

                ClearMatchingExplorerItems();
            }

            evt.StopPropagation();
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            parentTracker.Deactivate();

            ClearMatchingExplorerItems();
        }

        void OnMissPick(MouseDownEvent evt)
        {
            ClearInnerSelection();
            m_Selection.ClearSelection(this);
        }

        public void SetPreviewMode(bool mode)
        {
            if (mode)
            {
                m_ViewportWrapper.AddToClassList(s_PreviewModeClassName);
                m_Viewport.AddToClassList(s_PreviewModeClassName);
                m_PickOverlay.AddToClassList(s_PreviewModeClassName);
            }
            else
            {
                m_ViewportWrapper.RemoveFromClassList(s_PreviewModeClassName);
                m_Viewport.RemoveFromClassList(s_PreviewModeClassName);
                m_PickOverlay.RemoveFromClassList(s_PreviewModeClassName);
            }
        }

        void SetInnerSelection(VisualElement selectedElement)
        {
            m_BuilderResizer.Activate(m_Selection, m_PaneWindow.document.visualTreeAsset, selectedElement);
            m_BuilderMover.Activate(m_Selection, m_PaneWindow.document.visualTreeAsset, selectedElement);
            m_BuilderAnchorer.Activate(m_Selection, m_PaneWindow.document.visualTreeAsset, selectedElement);
        }

        void ClearInnerSelection()
        {
            m_BuilderResizer.Deactivate();
            m_BuilderMover.Deactivate();
            m_BuilderAnchorer.Deactivate();
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {

        }

        public void SelectionChanged()
        {
            if (m_Selection.isEmpty)
                ClearInnerSelection();
            else
                SetInnerSelection(m_Selection.selection.First());
        }

        public void StylingChanged(List<string> styles)
        {

        }
    }
}
