using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderExplorerContextMenu
    {
        private Builder m_Builder;
        private BuilderSelection m_Selection;

        private bool m_WeStartedTheDrag;

        List<ManipulatorActivationFilter> activators { get; set; }
        ManipulatorActivationFilter m_CurrentActivator;

        public BuilderExplorerContextMenu(Builder builder, BuilderSelection selection)
        {
            m_Builder = builder;
            m_Selection = selection;

            m_WeStartedTheDrag = false;

            activators = new List<ManipulatorActivationFilter>();
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
        }

        public void RegisterCallbacksOnTarget(VisualElement target)
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<ContextualMenuPopulateEvent>(a => BuildElementContextualMenu(a, target));
            target.RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        private void UnregisterCallbacksFromTarget(DetachFromPanelEvent evt)
        {
            var target = evt.target as VisualElement;

            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<ContextualMenuPopulateEvent>(a => BuildElementContextualMenu(a, target));
            target.UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStartManipulation(evt))
                return;

            var target = evt.currentTarget as VisualElement;
            target.CaptureMouse();
            m_WeStartedTheDrag = true;
            evt.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            var target = evt.currentTarget as VisualElement;

            if (!target.HasMouseCapture() || !m_WeStartedTheDrag)
                return;

            if (!CanStopManipulation(evt))
                return;

            if (target.elementPanel != null && target.elementPanel.contextualMenuManager != null)
            {
                target.elementPanel.contextualMenuManager.DisplayMenu(evt, target);
                evt.PreventDefault();
            }

            target.ReleaseMouse();
            m_WeStartedTheDrag = false;
            evt.StopPropagation();
        }

        private bool CanStartManipulation(IMouseEvent evt)
        {
            foreach (var activator in activators)
            {
                if (activator.Matches(evt))
                {
                    m_CurrentActivator = activator;
                    return true;
                }
            }

            return false;
        }

        private bool CanStopManipulation(IMouseEvent evt)
        {
            if (evt == null)
            {
                return false;
            }

            return ((MouseButton)evt.button == m_CurrentActivator.button);
        }

        public void BuildElementContextualMenu(ContextualMenuPopulateEvent evt, VisualElement target)
        {
            var item = target.userData as VisualElement;

            var isValidTarget = item != null && (item.IsPartOfCurrentDocument() || item.GetStyleComplexSelector() != null);
            if (isValidTarget)
                evt.StopImmediatePropagation();

            evt.menu.AppendAction(
                "Copy",
                a =>
                {
                    m_Selection.Select(null, item);
                    if (item.IsPartOfCurrentDocument() || item.GetStyleComplexSelector() != null)
                        m_Builder.commandHandler.PerformActionOnSelection(
                            m_Builder.commandHandler.CopyElement,
                            m_Builder.commandHandler.ClearCopyBuffer);
                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(
                "Paste",
                a =>
                {
                    m_Selection.Select(null, item);
                    m_Builder.commandHandler.Paste();
                },
                string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer)
                    ? DropdownMenuAction.Status.Disabled
                    : DropdownMenuAction.Status.Normal);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                "Rename",
                a =>
                {
                    m_Selection.Select(null, item);
                    var itemElement = item.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;
                    if (itemElement == null)
                        return;

                    itemElement.ActivateRenameElementMode();
                },
                item != null && item.IsPartOfCurrentDocument()
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(
                "Duplicate",
                a =>
                {
                    m_Selection.Select(null, item);
                    if (item.IsPartOfCurrentDocument() || item.GetStyleComplexSelector() != null)
                        m_Builder.commandHandler.PerformActionOnSelection(
                            m_Builder.commandHandler.DuplicateElement,
                            m_Builder.commandHandler.ClearCopyBuffer,
                            m_Builder.commandHandler.Paste);
                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                "Delete",
                a =>
                {   m_Selection.Select(null, item);
                    m_Builder.commandHandler.DeleteElement(item);
                    m_Builder.commandHandler.ClearSelectionNotify();
                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
        }
    }
}