using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class BuilderAssetUtilities
    {
        public static VisualElementAsset AddElementToAsset(
            BuilderDocument document, VisualElement ve, int index = -1)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);

            var veParent = ve.parent;
            var veaParent = veParent == null ? null : veParent.GetVisualElementAsset();
            var vea = document.visualTreeAsset.AddElement(veaParent, ve);

            if (index >= 0)
                document.visualTreeAsset.ReparentElement(vea, veaParent, index);

            return vea;
        }

        public static VisualElementAsset AddElementToAsset(
            BuilderDocument document, VisualElement ve,
            Func<VisualTreeAsset, VisualElementAsset, VisualElementAsset> makeVisualElementAsset,
            int index = -1)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);

            var veParent = ve.parent;
            var veaParent = veParent == null ? null : veParent.GetVisualElementAsset();

            var vea = makeVisualElementAsset(document.visualTreeAsset, veaParent);
            ve.SetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName, vea);

            if (index >= 0)
                document.visualTreeAsset.ReparentElement(vea, veaParent, index);

            return vea;
        }

        public static void ReparentElementInAsset(
            BuilderDocument document, VisualElement veToReparent, VisualElement newParent, int index = -1)
        {
            var veaToReparent = veToReparent.GetVisualElementAsset();
            if (veaToReparent == null)
                return;

            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.ReparentUIElementUndoMessage);

            var veaNewParent = newParent == null ? null : newParent.GetVisualElementAsset();
            document.visualTreeAsset.ReparentElement(veaToReparent, veaNewParent, index);
        }

        public static void DeleteElementFromAsset(BuilderDocument document, VisualElement ve)
        {
            var vea = ve.GetVisualElementAsset();
            if (vea == null)
                return;

            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.DeleteUIElementUndoMessage);

            document.visualTreeAsset.RemoveElement(vea);
        }

        public static void TransferAssetToAsset(
            BuilderDocument document, VisualElementAsset parent, VisualTreeAsset otherVta)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);

            document.visualTreeAsset.Swallow(parent, otherVta);
        }

        public static void AddStyleClassToElementInAsset(BuilderDocument document, VisualElement ve, string className)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.AddStyleClassUndoMessage);

            var vea = ve.GetVisualElementAsset();
            vea.AddStyleClass(className);
        }

        public static void RemoveStyleClassToElementInAsset(BuilderDocument document, VisualElement ve, string className)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.RemoveStyleClassUndoMessage);

            var vea = ve.GetVisualElementAsset();
            vea.RemoveStyleClass(className);
        }

        public static void AddElementToSelectionInAsset(BuilderDocument document, VisualElement ve)
        {
            if (BuilderSharedStyles.IsSelectorsContainerElement(ve))
            {
                document.mainStyleSheet.AddSelector(
                    BuilderConstants.SelectedStyleSheetSelectorName,
                    BuilderConstants.ChangeSelectionUndoMessage);
            }
            else if (BuilderSharedStyles.IsSelectorElement(ve))
            {
                var scs = ve.GetStyleComplexSelector();
                var selectionProp = document.mainStyleSheet.AddProperty(
                    scs,
                    BuilderConstants.SelectedStyleRulePropertyName,
                    BuilderConstants.ChangeSelectionUndoMessage);

                // Need to add at least one dummy value because lots of code will die
                // if it encounters a style property with no values.
                document.mainStyleSheet.AddValue(
                    selectionProp, 42.0f, BuilderConstants.ChangeSelectionUndoMessage);
            }
            else if (BuilderSharedStyles.IsDocumentElement(ve))
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vta = ve.GetVisualTreeAsset();
                vta.AddElement(null, BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName);
            }
            else if (ve.GetVisualElementAsset() != null)
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vea = ve.GetVisualElementAsset();
                vea.Select();
            }
        }

        public static void RemoveElementFromSelectionInAsset(BuilderDocument document, VisualElement ve)
        {
            if (BuilderSharedStyles.IsSelectorsContainerElement(ve))
            {
                document.mainStyleSheet.RemoveSelector(
                    BuilderConstants.SelectedStyleSheetSelectorName,
                    BuilderConstants.ChangeSelectionUndoMessage);
            }
            else if (BuilderSharedStyles.IsSelectorElement(ve))
            {
                var scs = ve.GetStyleComplexSelector();
                document.mainStyleSheet.RemoveProperty(
                    scs,
                    BuilderConstants.SelectedStyleRulePropertyName,
                    BuilderConstants.ChangeSelectionUndoMessage);
            }
            else if (BuilderSharedStyles.IsDocumentElement(ve))
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vta = ve.GetVisualTreeAsset();
                var selectedElement = vta.FindElementByType(BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName);
                vta.RemoveElement(selectedElement);
            }
            else if (ve.GetVisualElementAsset() != null)
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vea = ve.GetVisualElementAsset();
                vea.Deselect();
            }
        }
    }
}