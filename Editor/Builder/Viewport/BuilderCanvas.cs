using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal enum BuilderCanvasBackgroundMode
    {
        None,
        Color,
        Image,
        Camera
    };

    internal class BuilderCanvas : VisualElement
    {
        static readonly string s_ActiveHandleClassName = "unity-builder-canvas--active";

        VisualElement m_Container;
        Rect m_ThisRectOnStartDrag;
        VisualElement m_DragHoverCoverLayer;

        VisualElement m_DefaultBackgroundElement;
        VisualElement m_CustomBackgroundElement;

        Dictionary<string, VisualElement> m_HandleElements;

        public new class UxmlFactory : UxmlFactory<BuilderCanvas, UxmlTraits> { }

        public override VisualElement contentContainer => m_Container;

        public VisualElement defaultBackgroundElement => m_DefaultBackgroundElement;
        public VisualElement customBackgroundElement => m_CustomBackgroundElement;

        BuilderDocument m_Document;
        public BuilderDocument document
        {
            get { return m_Document; }
            set
            {
                if (value == m_Document)
                    return;

                m_Document = value;
                SetSizeFromDocumentSettings();
            }
        }

        public float width
        {
            get { return resolvedStyle.width; }
            set
            {
                style.width = value;

                if (document != null)
                {
                    document.settings.CanvasWidth = (int)value;
                    document.SaveSettingsToDisk();
                }
            }
        }

        public float height
        {
            get { return resolvedStyle.height; }
            set
            {
                style.height = value;

                if (document != null)
                {
                    document.settings.CanvasHeight = (int)value;
                    document.SaveSettingsToDisk();
                }
            }
        }

        public BuilderCanvas()
        {
            var builderTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/BuilderCanvas.uxml");
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

            SetSizeFromDocumentSettings();

            m_DefaultBackgroundElement = this.Q("default-background-element");
            m_CustomBackgroundElement = this.Q("custom-background-element");
        }

        public void SetSizeFromDocumentSettings()
        {
            if (document == null || document.settings.CanvasWidth < BuilderConstants.CanvasMinWidth)
            {
                width = BuilderConstants.CanvasInitialWidth;
                height = BuilderConstants.CanvasInitialHeight;
                return;
            }

            style.width = document.settings.CanvasWidth;
            style.height = document.settings.CanvasHeight;
        }

        void OnStartDrag(VisualElement handle)
        {
            m_ThisRectOnStartDrag = this.layout;

            m_DragHoverCoverLayer.style.display = DisplayStyle.Flex;
            m_DragHoverCoverLayer.style.cursor = handle.computedStyle.cursor;
        }

        void OnEndDrag()
        {
            m_DragHoverCoverLayer.style.display = DisplayStyle.None;
            m_DragHoverCoverLayer.RemoveFromClassList(s_ActiveHandleClassName);
        }

        void OnDragLeft(Vector2 diff)
        {
            var newWidth = m_ThisRectOnStartDrag.width - (diff.x * 2);
            newWidth = Mathf.Max(newWidth, BuilderConstants.CanvasMinWidth);
            width = newWidth;
        }

        void OnDragBottom(Vector2 diff)
        {
            var newHeight = m_ThisRectOnStartDrag.height + diff.y;
            newHeight = Mathf.Max(newHeight, BuilderConstants.CanvasMinHeight);
            height = newHeight;
        }

        void OnDragRight(Vector2 diff)
        {
            var newWidth = m_ThisRectOnStartDrag.width + (diff.x * 2);
            newWidth = Mathf.Max(newWidth, BuilderConstants.CanvasMinWidth);
            width = newWidth;
        }

        void OnDragBottomLeft(Vector2 diff)
        {
            OnDragBottom(diff);
            OnDragLeft(diff);
        }

        void OnDragBottomRight(Vector2 diff)
        {
            OnDragBottom(diff);
            OnDragRight(diff);
        }

        class Manipulator : MouseManipulator
        {
            Vector2 m_Start;
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