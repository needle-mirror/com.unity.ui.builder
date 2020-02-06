using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.StyleSheets;
using System.Linq;
using System.Collections.Generic;

namespace Unity.UI.Builder
{
    internal class BuilderCommandHandler
    {
        BuilderPaneWindow m_PaneWindow;
        BuilderToolbar m_Toolbar;
        BuilderSelection m_Selection;

        VisualElement m_CutElement;

        List<BuilderPaneContent> m_Panes = new List<BuilderPaneContent>();

        bool m_ControlWasPressed;
        IVisualElementScheduledItem m_ControlUnpressScheduleItem;

#if UNITY_2019_2
        // TODO: Hack. We need this because of a bug on Mac where we
        // get double command events.
        // Case: https://fogbugz.unity3d.com/f/cases/1180090/
        long m_LastFrameCount;
#endif
        
        public BuilderCommandHandler(
            BuilderPaneWindow paneWindow,
            BuilderSelection selection)
        {
            m_PaneWindow = paneWindow;
            m_Toolbar = null;
            m_Selection = selection;
        }

        public void OnEnable()
        {
            var root = m_PaneWindow.rootVisualElement;
            root.focusable = true; // We want commands to work anywhere in the builder.

            foreach (var pane in m_Panes)
            {
                pane.primaryFocusable.RegisterCallback<ValidateCommandEvent>(OnCommandValidate);
                pane.primaryFocusable.RegisterCallback<ExecuteCommandEvent>(OnCommandExecute);

                // Make sure Delete key works on Mac keyboards.
                pane.primaryFocusable.RegisterCallback<KeyDownEvent>(OnDelete);
            }

            // Ctrl+S to save.
            m_PaneWindow.rootVisualElement.RegisterCallback<KeyUpEvent>(OnSaveDocument);
            m_ControlUnpressScheduleItem = m_PaneWindow.rootVisualElement.schedule.Execute(UnsetControlFlag);

            // Undo/Redo
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public void OnDisable()
        {
            foreach (var pane in m_Panes)
            {
                pane.primaryFocusable.UnregisterCallback<ValidateCommandEvent>(OnCommandValidate);
                pane.primaryFocusable.UnregisterCallback<ExecuteCommandEvent>(OnCommandExecute);

                pane.primaryFocusable.UnregisterCallback<KeyDownEvent>(OnDelete);
            }

            m_PaneWindow.rootVisualElement.UnregisterCallback<KeyUpEvent>(OnSaveDocument);

            // Undo/Redo
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public void RegisterPane(BuilderPaneContent paneContent)
        {
            m_Panes.Add(paneContent);
        }

        public void RegisterToolbar(BuilderToolbar toolbar)
        {
            m_Toolbar = toolbar;
        }

        public void OnCommandValidate(ValidateCommandEvent evt)
        {
#if UNITY_2019_2
            // TODO: Hack. We need this because of a bug on Mac where we
            // get double command events.

            if (m_LastFrameCount == Time.frameCount)
                return;
            m_LastFrameCount = Time.frameCount;
#endif

            switch (evt.commandName)
            {
                case EventCommandNames.Cut: evt.StopPropagation(); return;
                case EventCommandNames.Copy: evt.StopPropagation(); return;
                case EventCommandNames.SoftDelete:
                case EventCommandNames.Delete: evt.StopPropagation(); return;
                case EventCommandNames.Duplicate: evt.StopPropagation(); return;
                case EventCommandNames.Paste: evt.StopPropagation(); return;
            }
        }

        public void OnCommandExecute(ExecuteCommandEvent evt)
        {
            switch (evt.commandName)
            {
                case EventCommandNames.Cut: PerformActionOnSelection(CutElement, ClearCopyBuffer, JustNotify); return;
                case EventCommandNames.Copy: PerformActionOnSelection(CopyElement, ClearCopyBuffer); return;
                case EventCommandNames.SoftDelete:
                case EventCommandNames.Delete: PerformActionOnSelection(DeleteElement, null, ClearSelectionNotify); return;
                case EventCommandNames.Duplicate: PerformActionOnSelection(DuplicateElement, ClearCopyBuffer, Paste); return;
                case EventCommandNames.Paste: Paste(); return;
            }
        }

        void OnUndoRedo()
        {
            m_PaneWindow.OnEnableAfterAllSerialization();
        }

        void UnsetControlFlag()
        {
            m_ControlWasPressed = false;
            m_ControlUnpressScheduleItem.Pause();
        }

        void OnSaveDocument(KeyUpEvent evt)
        {
            if (m_Toolbar == null)
                return;

            if (evt.keyCode == KeyCode.LeftCommand ||
                evt.keyCode == KeyCode.RightCommand ||
                evt.keyCode == KeyCode.LeftControl ||
                evt.keyCode == KeyCode.RightControl)
            {
                m_ControlUnpressScheduleItem.ExecuteLater(100);
                m_ControlWasPressed = true;
                return;
            }

            if (evt.keyCode != KeyCode.S)
                return;

            if (!evt.modifiers.HasFlag(EventModifiers.Control) &&
                !evt.modifiers.HasFlag(EventModifiers.Command) &&
                !m_ControlWasPressed)
                return;

            m_ControlWasPressed = false;

            m_Toolbar.PromptSaveDocumentDialog();

            evt.StopPropagation();
        }

        void OnDelete(KeyDownEvent evt)
        {
            // HACK: This must be a bug. TextField leaks its key events to everyone!
            if (evt.leafTarget is ITextInputField)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    PerformActionOnSelection(DeleteElement, null, ClearSelectionNotify);
                    break;
                case KeyCode.Escape:
                    {
                        if (m_CutElement != null)
                        {
                            m_CutElement = null;
                            BuilderEditorUtility.SystemCopyBuffer = null;
                        }
                    }
                    break;
            }
        }

        public void PerformActionOnSelection(Action<VisualElement> preElementaction, Action preAction = null, Action postAction = null)
        {
            preAction?.Invoke();

            if (m_Selection.isEmpty)
                return;

            foreach (var element in m_Selection.selection)
                preElementaction(element);

            postAction?.Invoke();
        }

        public void DuplicateElement(VisualElement element)
        {
            CopyElement(element);
        }

        public void CutElement(VisualElement element)
        {
            CopyElement(element);
            m_CutElement = element;
        }

        public void CopyElement(VisualElement element)
        {
            var vea = element.GetVisualElementAsset();
            if (vea != null)
            {
                BuilderEditorUtility.SystemCopyBuffer =
                    VisualTreeAssetToUXML.GenerateUXML(m_PaneWindow.document.visualTreeAsset, null, vea);
                return;
            }

            var selector = element.GetStyleComplexSelector();
            if (selector != null)
            {
                BuilderEditorUtility.SystemCopyBuffer =
                    StyleSheetToUss.ToUssString(m_PaneWindow.document.mainStyleSheet, selector);
                return;
            }
        }

        void PasteUXML(string copyBuffer)
        {
            var importer = new BuilderVisualTreeAssetImporter(); // Cannot be cached because the StyleBuilder never gets reset.
            importer.ImportXmlFromString(copyBuffer, out var pasteVta);

            VisualElementAsset parent = null;
            if (!m_Selection.isEmpty)
                parent = m_Selection.selection.First().parent?.GetVisualElementAsset();

            BuilderAssetUtilities.TransferAssetToAsset(m_PaneWindow.document, parent, pasteVta);
            m_PaneWindow.document.AddStyleSheetToAllRootElements();

            var selectionParentId = parent?.id ?? m_PaneWindow.document.visualTreeAsset.GetRootUXMLElement().id;
            VisualElementAsset newSelectedItem = pasteVta.templateAssets.FirstOrDefault(tpl => tpl.parentId == selectionParentId);
            if (newSelectedItem == null)
                newSelectedItem = pasteVta.visualElementAssets.FirstOrDefault(asset => asset.parentId == selectionParentId);

            m_Selection.ClearSelection(null);
            newSelectedItem.Select();

            ScriptableObject.DestroyImmediate(pasteVta);
        }

        void PasteUSS(string copyBuffer)
        {
            var pasteStyleSheet = StyleSheetUtilities.CreateInstance();
            var importer = new BuilderStyleSheetImporter(); // Cannot be cached because the StyleBuilder never gets reset.
            importer.Import(pasteStyleSheet, copyBuffer);

            BuilderAssetUtilities.TransferAssetToAsset(m_PaneWindow.document, pasteStyleSheet);

            m_Selection.ClearSelection(null);
            var scs =  m_PaneWindow.document.mainStyleSheet.complexSelectors.Last();
            BuilderAssetUtilities.AddStyleComplexSelectorToSelection(m_PaneWindow.document, scs);

            ScriptableObject.DestroyImmediate(pasteStyleSheet);
        }

        public void Paste()
        {
            var copyBuffer = BuilderEditorUtility.SystemCopyBuffer;

            if (string.IsNullOrEmpty(copyBuffer))
                return;

            var trimmedBuffer = copyBuffer.Trim();
            if (trimmedBuffer.StartsWith("<") && trimmedBuffer.EndsWith(">"))
                PasteUXML(copyBuffer);
            else if (trimmedBuffer.EndsWith("}"))
                PasteUSS(copyBuffer);
            else // Unknown string.
                return;

            if (m_CutElement != null)
            {
                DeleteElement(m_CutElement);
                m_CutElement = null;
                BuilderEditorUtility.SystemCopyBuffer = null;
            }

            m_PaneWindow.OnEnableAfterAllSerialization();

            // TODO: ListView bug. Does not refresh selection pseudo states after a
            // call to Refresh().
            m_PaneWindow.rootVisualElement.schedule.Execute(() =>
            {
                m_Selection.Select(null, m_Selection.selection.First());
            }).ExecuteLater(200);

            m_PaneWindow.document.hasUnsavedChanges = true;
        }

        public void DeleteElement(VisualElement element)
        {
            if (BuilderSharedStyles.IsSelectorsContainerElement(element) ||
                BuilderSharedStyles.IsDocumentElement(element) ||
                !element.IsLinkedToAsset())
                return;

            if (BuilderSharedStyles.IsSelectorElement(element))
            {
                Undo.RegisterCompleteObjectUndo(
                    m_PaneWindow.document.mainStyleSheet, BuilderConstants.DeleteSelectorUndoMessage);

                var selectorStr = BuilderSharedStyles.GetSelectorString(element);
                m_PaneWindow.document.mainStyleSheet.RemoveSelector(selectorStr);
            }
            else
            {
                BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document, element);
            }

            element.RemoveFromHierarchy();

            m_PaneWindow.document.hasUnsavedChanges = true;
        }

        public void ClearCopyBuffer()
        {
            BuilderEditorUtility.SystemCopyBuffer = null;
        }

        public void ClearSelectionNotify()
        {
            m_Selection.ClearSelection(null);
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
        }

        public void JustNotify()
        {
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
        }
    }
}