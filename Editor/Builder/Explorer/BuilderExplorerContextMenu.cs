using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderExplorerContextMenu
    {
        BuilderPaneWindow m_PaneWindow;
        BuilderSelection m_Selection;

        bool m_WeStartedTheDrag;

        List<ManipulatorActivationFilter> activators { get; set; }
        ManipulatorActivationFilter m_CurrentActivator;

        public BuilderExplorerContextMenu(BuilderPaneWindow paneWindow, BuilderSelection selection)
        {
            m_PaneWindow = paneWindow;
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

        void UnregisterCallbacksFromTarget(DetachFromPanelEvent evt)
        {
            var target = evt.target as VisualElement;

            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<ContextualMenuPopulateEvent>(a => BuildElementContextualMenu(a, target));
            target.UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStartManipulation(evt))
                return;

            var target = evt.currentTarget as VisualElement;
            target.CaptureMouse();
            m_WeStartedTheDrag = true;
            evt.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent evt)
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

        bool CanStartManipulation(IMouseEvent evt)
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

        bool CanStopManipulation(IMouseEvent evt)
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
                        m_PaneWindow.commandHandler.PerformActionOnSelection(
                            m_PaneWindow.commandHandler.CopyElement,
                            m_PaneWindow.commandHandler.ClearCopyBuffer);
                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(
                "Paste",
                a =>
                {
                    m_Selection.Select(null, item);
                    m_PaneWindow.commandHandler.Paste();
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
                        m_PaneWindow.commandHandler.PerformActionOnSelection(
                            m_PaneWindow.commandHandler.DuplicateElement,
                            m_PaneWindow.commandHandler.ClearCopyBuffer,
                            m_PaneWindow.commandHandler.Paste);
                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                "Delete",
                a =>
                {   m_Selection.Select(null, item);
                    m_PaneWindow.commandHandler.DeleteElement(item);
                    m_PaneWindow.commandHandler.ClearSelectionNotify();
                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
        }
    }
}