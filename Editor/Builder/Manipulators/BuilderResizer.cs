using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderResizer : BuilderTransformer
    {
        private static readonly string s_UssClassName = "unity-builder-resizer";

        private Dictionary<string, VisualElement> m_HandleElements;

        public new class UxmlFactory : UxmlFactory<BuilderResizer, UxmlTraits> { }

        public BuilderResizer()
        {
            var builderTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Builder/Manipulators/BuilderResizer.uxml");
            builderTemplate.CloneTree(this);

            AddToClassList(s_UssClassName);

            m_HandleElements = new Dictionary<string, VisualElement>();

            m_HandleElements.Add("top-handle", this.Q("top-handle"));
            m_HandleElements.Add("left-handle", this.Q("left-handle"));
            m_HandleElements.Add("bottom-handle", this.Q("bottom-handle"));
            m_HandleElements.Add("right-handle", this.Q("right-handle"));

            m_HandleElements.Add("top-left-handle", this.Q("top-left-handle"));
            m_HandleElements.Add("top-right-handle", this.Q("top-right-handle"));

            m_HandleElements.Add("bottom-left-handle", this.Q("bottom-left-handle"));
            m_HandleElements.Add("bottom-right-handle", this.Q("bottom-right-handle"));

            m_HandleElements["top-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragTop));
            m_HandleElements["left-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragLeft));
            m_HandleElements["bottom-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragBottom));
            m_HandleElements["right-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragRight));

            m_HandleElements["top-left-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragTopLeft));
            m_HandleElements["top-right-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragTopRight));

            m_HandleElements["bottom-left-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragBottomLeft));
            m_HandleElements["bottom-right-handle"].AddManipulator(new Manipulator(base.OnStartDrag, base.OnEndDrag, OnDragBottomRight));

            base.m_AbsoluteOnlyHandleElements.Add(m_HandleElements["top-handle"]);
            base.m_AbsoluteOnlyHandleElements.Add(m_HandleElements["left-handle"]);
            base.m_AbsoluteOnlyHandleElements.Add(m_HandleElements["top-left-handle"]);
            base.m_AbsoluteOnlyHandleElements.Add(m_HandleElements["top-right-handle"]);
            base.m_AbsoluteOnlyHandleElements.Add(m_HandleElements["bottom-left-handle"]);
        }

        private void OnDrag(
            TrackedStyle primaryStyle,
            float onStartDragLength,
            float onStartDragPrimary,
            float delta,
            List<string> changeList)
        {
            var oppositeStyle = GetOppositeStyle(primaryStyle);
            var lengthStyle = GetLengthStyle(primaryStyle);

            if (!IsNoneOrAuto(oppositeStyle) && !IsNoneOrAuto(primaryStyle))
            {
                SetStyleSheetValue(primaryStyle, onStartDragPrimary - delta);
                changeList.Add(GetStyleName(primaryStyle));
            }
            else if (IsNoneOrAuto(oppositeStyle) && !IsNoneOrAuto(primaryStyle))
            {
                SetStyleSheetValue(lengthStyle, onStartDragLength + delta);
                SetStyleSheetValue(primaryStyle, onStartDragPrimary - delta);
                changeList.Add(GetStyleName(primaryStyle));
                changeList.Add(GetStyleName(lengthStyle));
            }
            else if (!IsNoneOrAuto(oppositeStyle) && IsNoneOrAuto(primaryStyle))
            {
                SetStyleSheetValue(lengthStyle, onStartDragLength + delta);
                changeList.Add(GetStyleName(lengthStyle));
            }
            else
            {
                if (primaryStyle == TrackedStyle.Top || primaryStyle == TrackedStyle.Left)
                {
                    SetStyleSheetValue(lengthStyle, onStartDragLength + delta);
                    SetStyleSheetValue(primaryStyle, onStartDragPrimary - delta);
                    changeList.Add(GetStyleName(primaryStyle));
                    changeList.Add(GetStyleName(lengthStyle));
                }
                else
                {
                    SetStyleSheetValue(lengthStyle, onStartDragLength + delta);
                    changeList.Add(GetStyleName(lengthStyle));
                }
            }
        }

        private void OnDragTop(Vector2 diff, List<string> changeList)
        {
            OnDrag(
                TrackedStyle.Top,
                m_TargetRectOnStartDrag.height,
                m_TargetRectOnStartDrag.y,
                -diff.y,
                changeList);

            style.height = m_ThisRectOnStartDrag.height - diff.y;
            style.top = m_ThisRectOnStartDrag.y + diff.y;
        }

        private void OnDragLeft(Vector2 diff, List<string> changeList)
        {
            OnDrag(
                TrackedStyle.Left,
                m_TargetRectOnStartDrag.width,
                m_TargetRectOnStartDrag.x,
                -diff.x,
                changeList);

            style.width = m_ThisRectOnStartDrag.width - diff.x;
            style.left = m_ThisRectOnStartDrag.x + diff.x;
        }

        private void OnDragBottom(Vector2 diff, List<string> changeList)
        {
            OnDrag(
                TrackedStyle.Bottom,
                m_TargetRectOnStartDrag.height,
                m_TargetCorrectedBottomOnStartDrag,
                diff.y,
                changeList);

            style.height = m_ThisRectOnStartDrag.height + diff.y;
        }

        private void OnDragRight(Vector2 diff, List<string> changeList)
        {
            OnDrag(
                TrackedStyle.Right,
                m_TargetRectOnStartDrag.width,
                m_TargetCorrectedRightOnStartDrag,
                diff.x,
                changeList);

            style.width = m_ThisRectOnStartDrag.width + diff.x;
        }

        private void NotifySelection()
        {
            m_Selection.NotifyOfStylingChange(this, m_ScratchChangeList);
            m_Selection.NotifyOfHierarchyChange(this, m_Target, BuilderHierarchyChangeType.InlineStyle);
        }

        private void OnDragTop(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragTop(diff, m_ScratchChangeList);
            NotifySelection();
        }

        private void OnDragLeft(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragLeft(diff, m_ScratchChangeList);
            NotifySelection();
        }

        private void OnDragBottom(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragBottom(diff, m_ScratchChangeList);
            NotifySelection();
        }

        private void OnDragRight(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragRight(diff, m_ScratchChangeList);
            NotifySelection();
        }

        private void OnDragTopLeft(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragTop(diff, m_ScratchChangeList);
            OnDragLeft(diff, m_ScratchChangeList);
            NotifySelection();
        }

        private void OnDragTopRight(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragTop(diff, m_ScratchChangeList);
            OnDragRight(diff, m_ScratchChangeList);
            NotifySelection();
        }

        private void OnDragBottomLeft(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragBottom(diff, m_ScratchChangeList);
            OnDragLeft(diff, m_ScratchChangeList);
            NotifySelection();
        }

        private void OnDragBottomRight(Vector2 diff)
        {
            m_ScratchChangeList.Clear();
            OnDragBottom(diff, m_ScratchChangeList);
            OnDragRight(diff, m_ScratchChangeList);
            NotifySelection();
        }
    }
}