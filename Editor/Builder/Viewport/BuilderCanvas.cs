using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderCanvas : VisualElement, IResetableViewData
    {
        private static readonly string s_ActiveHandleClassName = "unity-builder-canvas--active";

        [SerializeField]
        private float m_SavedWidth;
        [SerializeField]
        private float m_SavedHeight;

        private VisualElement m_Container;
        private Rect m_ThisRectOnStartDrag;
        private VisualElement m_DragHoverCoverLayer;

        private Dictionary<string, VisualElement> m_HandleElements;

        public new class UxmlFactory : UxmlFactory<BuilderCanvas, UxmlTraits> { }

        public override VisualElement contentContainer => m_Container;

        public BuilderCanvas()
        {
            var builderTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/Builder/BuilderCanvas.uxml");
            builderTemplate.CloneTree(this);

            m_Container = this.Q("content-container");

            m_HandleElements = new Dictionary<string, VisualElement>();

            m_HandleElements.Add("left-handle", this.Q("left-handle"));
            m_HandleElements.Add("bottom-handle", this.Q("bottom-handle"));
            m_HandleElements.Add("right-handle", this.Q("right-handle"));

            m_HandleElements.Add("bottom-left-handle", this.Q("bottom-left-handle"));
            m_HandleElements.Add("bottom-right-handle", this.Q("bottom-right-handle"));

            m_HandleElements["left-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragLeft));
            m_HandleElements["bottom-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragBottom));
            m_HandleElements["right-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragRight));

            m_HandleElements["bottom-left-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragBottomLeft));
            m_HandleElements["bottom-right-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragBottomRight));

            m_DragHoverCoverLayer = this.Q("drag-hover-cover-layer");

            ResetViewData();
        }

        public void ResetViewData()
        {
            m_SavedWidth = BuilderConstants.CanvasInitialWidth;
            m_SavedHeight = BuilderConstants.CanvasInitialHeight;
            style.width = m_SavedWidth;
            style.height = m_SavedHeight;
            SaveViewData();
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);

            style.width = m_SavedWidth;
            style.height = m_SavedHeight;
        }

        private void OnStartDrag(VisualElement handle)
        {
            m_ThisRectOnStartDrag = this.layout;

            m_DragHoverCoverLayer.style.display = DisplayStyle.Flex;
            m_DragHoverCoverLayer.style.cursor = handle.computedStyle.cursor;
        }

        private void OnEndDrag()
        {
            m_DragHoverCoverLayer.style.display = DisplayStyle.None;
            m_DragHoverCoverLayer.RemoveFromClassList(s_ActiveHandleClassName);
        }

        private void OnDragLeft(Vector2 diff)
        {
            m_SavedWidth = m_ThisRectOnStartDrag.width - (diff.x * 2);
            style.width = m_SavedWidth;
            SaveViewData();
        }

        private void OnDragBottom(Vector2 diff)
        {
            m_SavedHeight = m_ThisRectOnStartDrag.height + diff.y;
            style.height = m_SavedHeight;
            SaveViewData();
        }

        private void OnDragRight(Vector2 diff)
        {
            m_SavedWidth = m_ThisRectOnStartDrag.width + (diff.x * 2);
            style.width = m_SavedWidth;
            SaveViewData();
        }

        private void OnDragBottomLeft(Vector2 diff)
        {
            OnDragBottom(diff);
            OnDragLeft(diff);
        }

        private void OnDragBottomRight(Vector2 diff)
        {
            OnDragBottom(diff);
            OnDragRight(diff);
        }

        private class Manipulator : MouseManipulator
        {
            private Vector2 m_Start;
            protected bool m_Active;

            Action<VisualElement> m_StartDrag;
            Action m_EndDrag;
            Action<Vector2> m_DragAction;

            public Manipulator(Action<VisualElement> startDrag, Action endDrag, Action<Vector2> dragAction)
            {
                m_StartDrag = startDrag;
                m_EndDrag = endDrag;
                m_DragAction = dragAction;
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                m_Active = false;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }

            protected void OnMouseDown(MouseDownEvent e)
            {
                if (m_Active)
                {
                    e.StopImmediatePropagation();
                    return;
                }

                if (CanStartManipulation(e))
                {
                    m_StartDrag(target);
                    m_Start = e.mousePosition;

                    m_Active = true;
                    target.CaptureMouse();
                    e.StopPropagation();

                    target.AddToClassList(s_ActiveHandleClassName);
                }
            }

            protected void OnMouseMove(MouseMoveEvent e)
            {
                if (!m_Active || !target.HasMouseCapture())
                    return;

                Vector2 diff = e.mousePosition - m_Start;

                m_DragAction(diff);

                e.StopPropagation();
            }

            protected void OnMouseUp(MouseUpEvent e)
            {
                if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
                    return;

                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
                m_EndDrag();

                target.RemoveFromClassList(s_ActiveHandleClassName);
            }
        }
    }
}