using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderTracker : VisualElement, IBuilderSelectionNotifier
    {
        private static readonly string s_UssClassName = "unity-builder-tracker";
        protected static readonly string s_ActiveClassName = "unity-builder-tracker--active";

        protected VisualElement m_Target;

        public BuilderTracker()
        {
            m_Target = null;

            AddToClassList(s_UssClassName);
        }

        public virtual void Activate(VisualElement target)
        {
            if (m_Target == target)
                return;

            if (m_Target != null)
                Deactivate();

            if (target == null)
                return;

            m_Target = target;

            AddToClassList(s_ActiveClassName);

            m_Target.RegisterCallback<GeometryChangedEvent>(OnExternalTargetResize);
            m_Target.RegisterCallback<DetachFromPanelEvent>(OnTargetDeletion);

            if (float.IsNaN(m_Target.layout.width))
            {
                m_Target.RegisterCallback<GeometryChangedEvent>(OnInitialStylesResolved);
            }
            else
            {
                SetStylesFromTargetStyles();
                ResizeSelfFromTarget(m_Target.layout);
            }
        }

        public virtual void Deactivate()
        {
            if (m_Target == null)
                return;

            m_Target.UnregisterCallback<GeometryChangedEvent>(OnExternalTargetResize);
            m_Target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDeletion);

            m_Target = null;

            RemoveFromClassList(s_ActiveClassName);
        }

        private void OnInitialStylesResolved(GeometryChangedEvent evt)
        {
            SetStylesFromTargetStyles();
            m_Target.UnregisterCallback<GeometryChangedEvent>(OnInitialStylesResolved);
        }

        protected virtual void SetStylesFromTargetStyles()
        {}

        private void OnExternalTargetResize(GeometryChangedEvent evt)
        {
            ResizeSelfFromTarget(m_Target.layout);
        }

        private void OnTargetDeletion(DetachFromPanelEvent evt)
        {
            Deactivate();
        }

        protected void ResizeSelfFromTarget(Rect targetRect)
        {
            var targetMarginTop = m_Target.resolvedStyle.marginTop;
            var targetMarginLeft = m_Target.resolvedStyle.marginLeft;
            var targetMarginRight = m_Target.resolvedStyle.marginRight;
            var targetMarginBottom = m_Target.resolvedStyle.marginBottom;

            targetRect.y -= targetMarginTop;
            targetRect.x -= targetMarginLeft;
            targetRect.width = targetRect.width + (targetMarginLeft + targetMarginRight);
            targetRect.height = targetRect.height + (targetMarginTop + targetMarginBottom);

            var selfRect = m_Target.hierarchy.parent.ChangeCoordinatesTo(this.hierarchy.parent, targetRect);

            var top = selfRect.y;
            var left = selfRect.x;
            var width = selfRect.width;
            var height = selfRect.height;

            style.top = top;
            style.left = left;
            style.width = width;
            style.height = height;
        }

        public void SelectionChanged()
        {

        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            if (m_Target == null)
                return;

            if (!changeType.HasFlag(BuilderHierarchyChangeType.InlineStyle))
                return;

            SetStylesFromTargetStyles();
            ResizeSelfFromTarget(m_Target.layout);
        }

        public void StylingChanged(List<string> styles)
        {
            if (m_Target == null)
                return;

            SetStylesFromTargetStyles();
            ResizeSelfFromTarget(m_Target.layout);
        }
    }
}
