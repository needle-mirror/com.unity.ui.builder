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
        VisualElement m_Surface;
        BuilderCanvas m_Canvas;
        VisualElement m_SharedStylesAndDocumentElement;
        VisualElement m_StyleSelectorElementContainer;
        VisualElement m_DocumentElement;
        VisualElement m_PickOverlay;
        VisualElement m_HighlightOverlay;
        BuilderParentTracker m_BuilderParentTracker;
        BuilderResizer m_BuilderResizer;
        BuilderMover m_BuilderMover;
        BuilderZoomer m_BuilderZoomer;
        BuilderPanner m_BuilderPanner;
        BuilderAnchorer m_BuilderAnchorer;

        BuilderSelection m_Selection;
        BuilderElementContextMenu m_ContextMenuManipulator;

        List<VisualElement> m_MatchingExplorerItems = new List<VisualElement>();

        public VisualElement toolbar => m_Toolbar;
        public VisualElement viewportWrapper => m_ViewportWrapper;
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

        private float m_ZoomScale = 1.0f;
        public float zoomScale
        {
            get { return m_ZoomScale; }
            set
            {
                if (m_ZoomScale == value)
                    return;

                m_ZoomScale = value;
                if (m_PaneWindow.document)
                    m_PaneWindow.document.viewportZoomScale = value;
                m_Canvas.zoomScale = value;
                m_PaneWindow.document.RefreshStyle(m_DocumentElement);
            }
        }

        private Vector2 m_ContentOffset = Vector2.zero;

        public Vector2 contentOffset
        {
            get { return m_ContentOffset; }
            set
            {
                if (m_ContentOffset == value)
                    return;

                m_ContentOffset = value;
                if (m_PaneWindow.document)
                    m_PaneWindow.document.viewportContentOffset = value;
                UpdateSurface();
            }
        }

        void UpdateSurface()
        {
            m_Surface.style.left = m_ContentOffset.x;
            m_Surface.style.top = m_ContentOffset.y;
        }

        public BuilderParentTracker parentTracker => m_BuilderParentTracker;
        public BuilderResizer resizer => m_BuilderResizer;
        public BuilderMover mover => m_BuilderMover;
        public BuilderAnchorer anchorer => m_BuilderAnchorer;
        public BuilderZoomer zoomer => m_BuilderZoomer;

        public VisualElement sharedStylesAndDocumentElement => m_SharedStylesAndDocumentElement;
        public VisualElement styleSelectorElementContainer => m_StyleSelectorElementContainer;
        public VisualElement documentElement => m_DocumentElement;
        public VisualElement pickOverlay => m_PickOverlay;
        public VisualElement highlightOverlay => m_HighlightOverlay;

        public BuilderViewport(BuilderPaneWindow paneWindow, BuilderSelection selection, BuilderElementContextMenu contextMenuManipulator)
        {
            m_PaneWindow = paneWindow;
            m_Selection = selection;
            m_ContextMenuManipulator = contextMenuManipulator;

            AddToClassList("unity-builder-viewport");

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/BuilderViewport.uxml");
            template.CloneTree(this);

            m_Toolbar = this.Q("toolbar");
            m_ViewportWrapper = this.Q("viewport-wrapper");
            m_Viewport = this.Q("viewport");
            m_Surface = this.Q("viewport-surface");
            m_Surface.pickingMode = PickingMode.Ignore;
            m_Canvas = this.Q<BuilderCanvas>("canvas");
            m_Canvas.document = paneWindow.document;
            m_SharedStylesAndDocumentElement = this.Q("shared-styles-and-document");
            m_StyleSelectorElementContainer = this.Q(BuilderConstants.StyleSelectorElementContainerName);
            m_DocumentElement = this.Q("document");
            m_Canvas.documentElement = m_DocumentElement;
            m_PickOverlay = this.Q("pick-overlay");
            m_HighlightOverlay = this.Q("highlight-overlay");
            m_BuilderParentTracker = this.Q<BuilderParentTracker>("parent-tracker");
            m_BuilderResizer = this.Q<BuilderResizer>("resizer");
            m_BuilderMover = this.Q<BuilderMover>("mover");
            m_BuilderAnchorer = this.Q<BuilderAnchorer>("anchorer");
            m_BuilderZoomer = new BuilderZoomer(this);
            m_BuilderPanner = new BuilderPanner(this);

            m_BuilderMover.parentTracker = m_BuilderParentTracker;

            m_PickOverlay.RegisterCallback<MouseDownEvent>(OnPick);
            m_PickOverlay.RegisterCallback<MouseMoveEvent>(OnHover);
            m_PickOverlay.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            m_Viewport.RegisterCallback<MouseDownEvent>(OnMissPick);
            
            m_ContextMenuManipulator?.RegisterCallbacksOnTarget(m_Viewport);

            // Make sure this gets focus when the pane gets focused.
            primaryFocusable = this;
            focusable = true;

            // Restore the zoom scale
            zoomScale = paneWindow.document.viewportZoomScale;
            contentOffset = paneWindow.document.viewportContentOffset;
        }

        private void ResetViewTransform()
        {
            contentOffset = BuilderConstants.ViewportInitialContentOffset;
            zoomScale = BuilderConstants.ViewportInitialZoom;
        }

        public void SetViewFromDocumentSetting()
        {
            contentOffset = m_PaneWindow.document.viewportContentOffset;
            zoomScale = m_PaneWindow.document.viewportZoomScale;
            canvas.SetSizeFromDocumentSettings();
        }

        public void ResetView()
        {
            ResetViewTransform();
            canvas.ResetSize();
            CenterCanvas();
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (pane == null)
                return;

            pane.subTitle = m_SubTitle;
        }

        public void FitCanvas()
        {
            const float kMargin = 8f;

            ResetViewTransform();
            m_Canvas.x = m_Canvas.y = 0.0f;

            var maxCanvasWidth = m_Viewport.resolvedStyle.width - s_CanvasViewportMinWidthDiff - 2 * kMargin;
            var maxCanvasHeight = m_Viewport.resolvedStyle.height - s_CanvasViewportMinHeightDiff - 2 * kMargin;

            var currentWidth = m_Canvas.width;
            var currentHeight = m_Canvas.height;

            if (currentWidth > maxCanvasWidth)
                m_Canvas.width = maxCanvasWidth;

            if (currentHeight > maxCanvasHeight)
                m_Canvas.height = maxCanvasHeight;

            CenterCanvas();
        }

        void CenterCanvas()
        {
            contentOffset = new Vector2((m_Viewport.resolvedStyle.width - m_Canvas.width) / 2, (m_Viewport.resolvedStyle.height - m_Canvas.height) / 2);
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
            // Do not prevent zoom and pan
            if (evt.button == 2 || (evt.ctrlKey && evt.altKey || (evt.button == (int)MouseButton.RightMouse && evt.altKey)))
                return;

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

            if (evt.button == (int) MouseButton.RightMouse)
            {
                if (pickedElement != null && m_ContextMenuManipulator != null)
                {
                    pickedElement.SetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName, pickedElement);
                    m_ContextMenuManipulator.RegisterCallbacksOnTarget(pickedElement);
                    m_ContextMenuManipulator.DisplayContextMenu(evt, pickedElement);
                    evt.StopPropagation();
                }
            }
            else
            {
                evt.StopPropagation();
            }
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
            if (evt.button == 2)
                return;

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
