using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderClassDragger : BuilderDragger
    {
        static readonly string s_DraggableStyleClassPillClassName = "unity-builder-class-pill--draggable";

        string m_ClassNameBeingDragged;

        public BuilderClassDragger(
            BuilderPaneWindow paneWindow,
            VisualElement root, BuilderSelection selection,
            BuilderViewport viewport, BuilderParentTracker parentTracker)
            : base(paneWindow, root, selection, viewport, parentTracker)
        {

        }

        protected override VisualElement CreateDraggedElement()
        {
            var classPillTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderClassPill.uxml");
            var pill = classPillTemplate.CloneTree();
            pill.AddToClassList(s_DraggableStyleClassPillClassName);
            return pill;
        }

        protected override bool StartDrag(VisualElement target, Vector2 mousePosition, VisualElement pill)
        {
            m_ClassNameBeingDragged = target.GetProperty(BuilderConstants.ExplorerStyleClassPillClassNameVEPropertyName) as string;
            pill.Q<Label>().text = m_ClassNameBeingDragged;
            return true;
        }

        protected override void PerformAction(VisualElement destination, int index = -1)
        {
            if (BuilderSharedStyles.IsDocumentElement(destination))
                return;

            var className = m_ClassNameBeingDragged.TrimStart('.');

            destination.AddToClassList(className);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.AddStyleClassToElementInAsset(
                paneWindow.document, destination, className);

            selection.NotifyOfHierarchyChange(null);
            selection.NotifyOfStylingChange(null);
        }

        protected override bool StopEventOnMouseDown()
        {
            return false;
        }

        protected override bool IsPickedElementValid(VisualElement element)
        {
            if (element == null)
                return false;

            if (element.GetVisualElementAsset() == null)
                return false;

            return true;
        }
    }
}