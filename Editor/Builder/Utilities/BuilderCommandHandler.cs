using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.StyleSheets;
using System.Linq;

namespace Unity.UI.Builder
{
    internal class BuilderCommandHandler
    {
        private Builder m_Builder;
        private BuilderExplorer m_Explorer;
        private BuilderViewport m_Viewport;
        private BuilderToolbar m_Toolbar;
        private BuilderSelection m_Selection;

        private VisualElement m_CutElement;

        private bool m_ControlWasPressed;
        private IVisualElementScheduledItem m_ControlUnpressScheduleItem;

        // TODO: Hack. We need this because of a bug on Mac where we
        // get double command events.
        // Case: https://fogbugz.unity3d.com/f/cases/1180090/
        private long m_LastFrameCount;

        public BuilderCommandHandler(
            Builder builder,
            BuilderExplorer explorer,
            BuilderViewport viewport,
            BuilderToolbar toolbar,
            BuilderSelection selection)
        {
            m_Builder = builder;
            m_Explorer = explorer;
            m_Viewport = viewport;
            m_Toolbar = toolbar;
            m_Selection = selection;
        }

        public void OnEnable()
        {
            var root = m_Builder.rootVisualElement;
            root.focusable = true; // We want commands to work anywhere in the builder.

            m_Explorer.primaryFocusable.RegisterCallback<ValidateCommandEvent>(OnCommandValidate);
            m_Explorer.primaryFocusable.RegisterCallback<ExecuteCommandEvent>(OnCommandExecute);
            m_Viewport.primaryFocusable.RegisterCallback<ValidateCommandEvent>(OnCommandValidate);
            m_Viewport.primaryFocusable.RegisterCallback<ExecuteCommandEvent>(OnCommandExecute);

            // Make sure Delete key works on Mac keyboards.
            m_Explorer.primaryFocusable.RegisterCallback<KeyDownEvent>(OnDelete);
            m_Viewport.primaryFocusable.RegisterCallback<KeyDownEvent>(OnDelete);

            // Ctrl+S to save.
            m_Builder.rootVisualElement.RegisterCallback<KeyUpEvent>(OnSaveDocument);
            m_ControlUnpressScheduleItem = m_Builder.rootVisualElement.schedule.Execute(UnsetControlFlag);

            // Undo/Redo
            Undo.undoRedoPerformed += OnUndoRedo;

            // Quitting
            EditorApplication.wantsToQuit += UnityWantsToQuit;
        }

        public void OnDisable()
        {
            m_Explorer.primaryFocusable.UnregisterCallback<ValidateCommandEvent>(OnCommandValidate);
            m_Explorer.primaryFocusable.UnregisterCallback<ExecuteCommandEvent>(OnCommandExecute);
            m_Viewport.primaryFocusable.UnregisterCallback<ValidateCommandEvent>(OnCommandValidate);
            m_Viewport.primaryFocusable.UnregisterCallback<ExecuteCommandEvent>(OnCommandExecute);

            m_Explorer.primaryFocusable.UnregisterCallback<KeyDownEvent>(OnDelete);
            m_Viewport.primaryFocusable.UnregisterCallback<KeyDownEvent>(OnDelete);

            m_Builder.rootVisualElement.UnregisterCallback<KeyUpEvent>(OnSaveDocument);

            // Undo/Redo
            Undo.undoRedoPerformed -= OnUndoRedo;

            // Quitting
            EditorApplication.wantsToQuit -= UnityWantsToQuit;
        }

        bool UnityWantsToQuit()
        {
            var allowQuitting = m_Toolbar.CheckForUnsavedChanges();
            return allowQuitting;
        }

        public void OnCommandValidate(ValidateCommandEvent evt)
        {
            // TODO: Hack. We need this because of a bug on Mac where we
            // get double command events.
            if (m_LastFrameCount == Time.frameCount)
                return;
            m_LastFrameCount = Time.frameCount;

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

        private void OnUndoRedo()
        {
            m_Builder.OnEnableAfterAllSerialization();
        }

        private void UnsetControlFlag()
        {
            m_ControlWasPressed = false;
            m_ControlUnpressScheduleItem.Pause();
        }

        private void OnSaveDocument(KeyUpEvent evt)
        {
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

        private void OnDelete(KeyDownEvent evt)
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
                            EditorGUIUtility.systemCopyBuffer = null;
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
                EditorGUIUtility.systemCopyBuffer =
                    VisualTreeAssetToUXML.GenerateUXML(m_Builder.document.visualTreeAsset, null, vea);
                return;
            }

            var selector = element.GetStyleComplexSelector();
            if (selector != null)
            {
                EditorGUIUtility.systemCopyBuffer =
                    StyleSheetToUss.ToUssString(m_Builder.document.mainStyleSheet, selector);
                return;
            }
        }

        private void PasteUXML(string copyBuffer)
        {
            VisualTreeAsset pasteVta = null;
            var importer = new UXMLImporterImpl(); // Cannot be cached because the StyleBuilder never gets reset.
            importer.ImportXmlFromString(copyBuffer, out pasteVta);

            VisualElementAsset parent = null;
            if (!m_Selection.isEmpty)
                parent = m_Selection.selection.First().parent?.GetVisualElementAsset();

            BuilderAssetUtilities.TransferAssetToAsset(m_Builder.document, parent, pasteVta);

            ScriptableObject.DestroyImmediate(pasteVta);
        }

        private void PasteUSS(string copyBuffer)
        {
            var pasteStyleSheet = StyleSheetUtilities.CreateInstance();
            var importer = new StyleSheetImporterImpl(); // Cannot be cached because the StyleBuilder never gets reset.
            importer.Import(pasteStyleSheet, copyBuffer);

            BuilderAssetUtilities.TransferAssetToAsset(m_Builder.document, pasteStyleSheet);

            ScriptableObject.DestroyImmediate(pasteStyleSheet);
        }

        public void Paste()
        {
            var copyBuffer = EditorGUIUtility.systemCopyBuffer;

            if (string.IsNullOrEmpty(copyBuffer))
                return;

            if (copyBuffer.TrimStart().StartsWith("<UXML"))
                PasteUXML(copyBuffer);
            else if (copyBuffer.TrimEnd().EndsWith("}"))
                PasteUSS(copyBuffer);
            else // Unknown string.
                return;

            if (m_CutElement != null)
            {
                DeleteElement(m_CutElement);
                m_CutElement = null;
                EditorGUIUtility.systemCopyBuffer = null;
            }

            m_Builder.OnEnableAfterAllSerialization();

            m_Builder.document.hasUnsavedChanges = true;
        }

        public void DeleteElement(VisualElement element)
        {
            if (BuilderSharedStyles.IsSelectorsContainerElement(element) ||
                BuilderSharedStyles.IsDocumentElement(element))
                return;

            if (BuilderSharedStyles.IsSelectorElement(element))
            {
                Undo.RegisterCompleteObjectUndo(
                    m_Builder.document.mainStyleSheet, BuilderConstants.DeleteSelectorUndoMessage);

                var selectorStr = BuilderSharedStyles.GetSelectorString(element);
                m_Builder.document.mainStyleSheet.RemoveSelector(selectorStr);
            }
            else
            {
                BuilderAssetUtilities.DeleteElementFromAsset(m_Builder.document, element);
            }

            element.RemoveFromHierarchy();

            m_Builder.document.hasUnsavedChanges = true;
        }

        public void ClearCopyBuffer()
        {
            EditorGUIUtility.systemCopyBuffer = null;
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