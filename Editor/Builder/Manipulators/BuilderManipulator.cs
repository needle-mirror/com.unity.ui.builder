using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

#if UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements.StyleSheets;
#endif

namespace Unity.UI.Builder
{
    class BuilderManipulator : BuilderTracker
    {
        protected static readonly string s_WidthStyleName = "width";
        protected static readonly string s_HeightStyleName = "height";
        protected static readonly string s_LeftStyleName = "left";
        protected static readonly string s_TopStyleName = "top";
        protected static readonly string s_RightStyleName = "right";
        protected static readonly string s_BottomStyleName = "bottom";

        protected enum TrackedStyle
        {
            Width,
            Height,
            Left,
            Top,
            Right,
            Bottom
        }

        protected BuilderSelection m_Selection;
        protected VisualTreeAsset m_VisualTreeAsset;

        protected List<VisualElement> m_AbsoluteOnlyHandleElements;

        public BuilderManipulator()
        {
            m_AbsoluteOnlyHandleElements = new List<VisualElement>();
        }

        public virtual void Activate(BuilderSelection selection, VisualTreeAsset visualTreeAsset, VisualElement target)
        {
            base.Activate(target);

            if (target == null)
                return;

            m_Selection = selection;
            m_VisualTreeAsset = visualTreeAsset;
        }

        protected override void SetStylesFromTargetStyles()
        {
            if (m_Target == null)
                return;

            if (m_Target.resolvedStyle.display == DisplayStyle.None ||
                BuilderSharedStyles.IsDocumentElement(m_Target) ||
                m_Target.GetVisualElementAsset() == null)
            {
                this.RemoveFromClassList(s_ActiveClassName);
                return;
            }
            else
            {
                this.AddToClassList(s_ActiveClassName);
            }

            if (m_Target.resolvedStyle.position == Position.Relative)
            {
                foreach (var element in m_AbsoluteOnlyHandleElements)
                    element.style.display = DisplayStyle.None;
            }
            else
            {
                foreach (var element in m_AbsoluteOnlyHandleElements)
                    element.style.display = DisplayStyle.Flex;
            }
        }

        ///

        protected string GetStyleName(TrackedStyle trackedStyle)
        {
            switch (trackedStyle)
            {
                case TrackedStyle.Width: return s_WidthStyleName;
                case TrackedStyle.Height: return s_HeightStyleName;
                case TrackedStyle.Left: return s_LeftStyleName;
                case TrackedStyle.Top: return s_TopStyleName;
                case TrackedStyle.Right: return s_RightStyleName;
                case TrackedStyle.Bottom: return s_BottomStyleName;
            }
            return null;
        }

        protected float GetResolvedStyleValue(TrackedStyle trackedStyle)
        {
            return GetResolvedStyleFloat(trackedStyle, m_Target);
        }

        protected float GetResolvedStyleFloat(TrackedStyle trackedStyle, VisualElement target)
        {
            if (target == null)
                return 0;

            switch (trackedStyle)
            {
                case TrackedStyle.Width: return target.resolvedStyle.width;
                case TrackedStyle.Height: return target.resolvedStyle.height;
                case TrackedStyle.Left: return target.resolvedStyle.left;
                case TrackedStyle.Top: return target.resolvedStyle.top;
                case TrackedStyle.Right: return target.resolvedStyle.right;
                case TrackedStyle.Bottom: return target.resolvedStyle.bottom;
            }
            return 0;
        }

        protected bool IsNoneOrAuto(TrackedStyle trackedStyle)
        {
            if (m_Target == null)
                return false;

            switch (trackedStyle)
            {
                case TrackedStyle.Width: return m_Target.computedStyle.width == StyleKeyword.None || m_Target.computedStyle.width == StyleKeyword.Auto;
                case TrackedStyle.Height: return m_Target.computedStyle.height == StyleKeyword.None || m_Target.computedStyle.height == StyleKeyword.Auto;
                case TrackedStyle.Left: return m_Target.computedStyle.left == StyleKeyword.None || m_Target.computedStyle.left == StyleKeyword.Auto;
                case TrackedStyle.Top: return m_Target.computedStyle.top == StyleKeyword.None || m_Target.computedStyle.top == StyleKeyword.Auto;
                case TrackedStyle.Right: return m_Target.computedStyle.right == StyleKeyword.None || m_Target.computedStyle.right == StyleKeyword.Auto;
                case TrackedStyle.Bottom: return m_Target.computedStyle.bottom == StyleKeyword.None || m_Target.computedStyle.bottom == StyleKeyword.Auto;
            }

            return false;
        }

        protected float GetMargineResolvedStyleFloat(TrackedStyle trackedStyle)
        {
            var target = m_Target;
            if (target == null)
                return 0;

            switch (trackedStyle)
            {
                case TrackedStyle.Left: return target.resolvedStyle.marginLeft;
                case TrackedStyle.Top: return target.resolvedStyle.marginTop;
                case TrackedStyle.Right: return target.resolvedStyle.marginRight;
                case TrackedStyle.Bottom: return target.resolvedStyle.marginBottom;
            }

            return 0;
        }

        protected float GetBorderResolvedStyleFloat(TrackedStyle trackedStyle, VisualElement target)
        {
            if (target == null)
                return 0;

            switch (trackedStyle)
            {
                case TrackedStyle.Left: return target.resolvedStyle.borderLeftWidth;
                case TrackedStyle.Top: return target.resolvedStyle.borderTopWidth;
                case TrackedStyle.Right: return target.resolvedStyle.borderRightWidth;
                case TrackedStyle.Bottom: return target.resolvedStyle.borderBottomWidth;
            }

            return 0;
        }

        protected TrackedStyle GetOppositeStyle(TrackedStyle trackedStyle)
        {
            switch (trackedStyle)
            {
                case TrackedStyle.Width: return TrackedStyle.Height;
                case TrackedStyle.Height: return TrackedStyle.Width;
                case TrackedStyle.Left: return TrackedStyle.Right;
                case TrackedStyle.Top: return TrackedStyle.Bottom;
                case TrackedStyle.Right: return TrackedStyle.Left;
                case TrackedStyle.Bottom: return TrackedStyle.Top;
            }

            throw new Exception("Invalid tracked style.");
        }

        protected TrackedStyle GetLengthStyle(TrackedStyle trackedStyle)
        {
            switch (trackedStyle)
            {
                case TrackedStyle.Width: return TrackedStyle.Width;
                case TrackedStyle.Height: return TrackedStyle.Height;
                case TrackedStyle.Left: return TrackedStyle.Width;
                case TrackedStyle.Top: return TrackedStyle.Height;
                case TrackedStyle.Right: return TrackedStyle.Width;
                case TrackedStyle.Bottom: return TrackedStyle.Height;
            }

            throw new Exception("Invalid tracked style.");
        }

        ///

        protected float GetStyleSheetFloat(TrackedStyle trackedStyle)
        {
            var name = GetStyleName(trackedStyle);

            if (IsNoneOrAuto(trackedStyle))
                return GetResolvedStyleFloat(trackedStyle, m_Target);
            else
                return GetStyleSheetFloat(name);
        }

        protected float GetStyleSheetFloat(string styleName)
        {
            var vea = m_Target.GetVisualElementAsset();
            var rule = m_VisualTreeAsset.GetOrCreateInlineStyleRule(vea);
            var styleSheet = m_VisualTreeAsset.inlineSheet;

            var styleProperty = styleSheet.FindProperty(rule, styleName);
            if (styleProperty == null)
                styleProperty = styleSheet.AddProperty(rule, styleName);

            if (styleProperty.values.Length == 0)
                return 0;
            else // TODO: Assume only one value.
                return styleSheet.GetFloat(styleProperty.values[0]);
        }

        protected void SetStyleSheetValue(TrackedStyle trackedStyle, float value)
        {
            var name = GetStyleName(trackedStyle);
            SetStyleSheetValue(name, value);
        }

        protected void SetStyleSheetValue(string styleName, float value)
        {
            // Remove temporary min-size element.
            m_Target.RemoveMinSizeSpecialElement();

            var vea = m_Target.GetVisualElementAsset();
            var rule = m_VisualTreeAsset.GetOrCreateInlineStyleRule(vea);
            var styleSheet = m_VisualTreeAsset.inlineSheet;

            var styleProperty = styleSheet.FindProperty(rule, styleName);
            if (styleProperty == null)
                styleProperty = styleSheet.AddProperty(rule, styleName);

            var isNewValue = styleProperty.values.Length == 0;

#if UNITY_2019_3_OR_NEWER
            // If the current style property is saved as a float instead of a dimension,
            // it means it's a user file where they left out the unit. We need to resave
            // it here as a dimension to create final proper uss.
            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Dimension)
            {
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            var dimension = new Dimension();
            dimension.unit = Dimension.Unit.Pixel;
            dimension.value = value;
#else
            var dimension = value;
#endif

            if (isNewValue)
                styleSheet.AddValue(styleProperty, dimension);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], dimension);
        }

        protected void RemoveStyleSheetValue(string styleName)
        {
            var vea = m_Target.GetVisualElementAsset();
            var rule = m_VisualTreeAsset.GetOrCreateInlineStyleRule(vea);
            var styleSheet = m_VisualTreeAsset.inlineSheet;

            var styleProperty = styleSheet.FindProperty(rule, styleName);
            if (styleProperty == null)
                return;

            // TODO: Assume only one value.
            styleSheet.RemoveProperty(rule, styleProperty);
        }
    }
}
