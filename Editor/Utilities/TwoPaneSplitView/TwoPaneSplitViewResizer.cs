using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class TwoPaneSplitViewResizer : MouseManipulator
    {
        private Vector2 m_Start;
        protected bool m_Active;
        private TwoPaneSplitView m_SplitView;
        private VisualElement m_Pane;
        private int m_Direction;
        private float m_MinWidth;
        private TwoPaneSplitView.Orientation m_Orientation;

        public TwoPaneSplitViewResizer(TwoPaneSplitView splitView, int dir, float minWidth, TwoPaneSplitView.Orientation orientation)
        {
            m_Orientation = orientation;
            m_MinWidth = minWidth;
            m_SplitView = splitView;
            m_Pane = splitView.fixedPane;
            m_Direction = dir;
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

        public void ApplyDelta(float delta)
        {
            float oldDimension = m_Orientation == TwoPaneSplitView.Orientation.Horizontal
                ? m_Pane.resolvedStyle.width
                : m_Pane.resolvedStyle.height;
            float newDimension = oldDimension + delta;

            if (newDimension < oldDimension && newDimension < m_MinWidth)
                newDimension = m_MinWidth;

            float maxLength = m_Orientation == TwoPaneSplitView.Orientation.Horizontal
                ? m_SplitView.resolvedStyle.width
                : m_SplitView.resolvedStyle.height;
            if (newDimension > oldDimension && newDimension > maxLength)
                newDimension = maxLength;

            if (m_Orientation == TwoPaneSplitView.Orientation.Horizontal)
            {
                m_Pane.style.width = newDimension;
                if (m_SplitView.fixedPaneIndex == 0)
                    target.style.left = newDimension;
                else
                    target.style.left = m_SplitView.resolvedStyle.width - newDimension;
            }
            else
            {
                m_Pane.style.height = newDimension;
                if (m_SplitView.fixedPaneIndex == 0)
                    target.style.top = newDimension;
                else
                    target.style.top = m_SplitView.resolvedStyle.height - newDimension;
            }
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
                m_Start = e.localMousePosition;

                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active || !target.HasMouseCapture())
                return;

            Vector2 diff = e.localMousePosition - m_Start;
            float mouseDiff = diff.x;
            if (m_Orientation == TwoPaneSplitView.Orientation.Vertical)
                mouseDiff = diff.y;

            float delta = m_Direction * mouseDiff;

            ApplyDelta(delta);

            e.StopPropagation();
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
                return;

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }
    }
}