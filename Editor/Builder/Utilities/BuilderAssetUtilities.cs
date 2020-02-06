using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal static class BuilderAssetUtilities
    {
        public static string GetResourcesPathForAsset(Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            return GetResourcesPathForAsset(assetPath);
        }

        public static string GetResourcesPathForAsset(string assetPath)
        {
            var resourcesFolder = "Resources/";
            if (string.IsNullOrEmpty(assetPath) || !assetPath.Contains(resourcesFolder))
                return null;

            var lastResourcesSubstring = assetPath.LastIndexOf(resourcesFolder) + resourcesFolder.Length;
            assetPath = assetPath.Substring(lastResourcesSubstring);
            var lastExtDot = assetPath.LastIndexOf(".");
            assetPath = assetPath.Substring(0, lastExtDot);

            return assetPath;
        }

        public static VisualElementAsset AddElementToAsset(
            BuilderDocument document, VisualElement ve, int index = -1)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);

            var veParent = ve.parent;
            VisualElementAsset veaParent = null;
            if (veParent != null)
                veaParent = veParent.GetVisualElementAsset();

#if UNITY_2020_1_OR_NEWER
            if (veaParent == null)
                veaParent = document.visualTreeAsset.GetRootUXMLElement(); // UXML Root Element
#endif

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

#if UNITY_2020_1_OR_NEWER
            if (veaParent == null)
                veaParent = document.visualTreeAsset.GetRootUXMLElement(); // UXML Root Element
#endif

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

            VisualElementAsset veaNewParent = null;
            if (newParent != null)
                veaNewParent = newParent.GetVisualElementAsset();

#if UNITY_2020_1_OR_NEWER
            if (veaNewParent == null)
                veaNewParent = document.visualTreeAsset.GetRootUXMLElement(); // UXML Root Element
#endif

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

        public static void TransferAssetToAsset(
            BuilderDocument document, StyleSheet otherStyleSheet)
        {
            Undo.RegisterCompleteObjectUndo(
                document.mainStyleSheet, BuilderConstants.AddNewSelectorUndoMessage);

            document.mainStyleSheet.Swallow(otherStyleSheet);
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

        public static void AddStyleComplexSelectorToSelection(BuilderDocument document, StyleComplexSelector scs)
        {
            var selectionProp = document.mainStyleSheet.AddProperty(
                scs,
                BuilderConstants.SelectedStyleRulePropertyName,
                BuilderConstants.ChangeSelectionUndoMessage);

            // Need to add at least one dummy value because lots of code will die
            // if it encounters a style property with no values.
            document.mainStyleSheet.AddValue(
                selectionProp, 42.0f, BuilderConstants.ChangeSelectionUndoMessage);
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
                AddStyleComplexSelectorToSelection(document, scs);
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
        
        public static string GetVisualTreeAssetAssetName(VisualTreeAsset visualTreeAsset) =>
            GetAssetName(visualTreeAsset, BuilderConstants.UxmlExtension);
        
        public static string GetStyleSheetAssetName(StyleSheet styleSheet) =>
            GetAssetName(styleSheet, BuilderConstants.UssExtension);

        public static string GetAssetName(ScriptableObject asset, string extension)
        {
            if (asset == null)
                return BuilderConstants.ToolbarUnsavedFileDisplayMessage + extension;

            var assetPath = AssetDatabase.GetAssetPath(asset);
            if(string.IsNullOrEmpty(assetPath))
                return BuilderConstants.ToolbarUnsavedFileDisplayMessage + extension;
           
            return Path.GetFileName(assetPath);
        }
    }
}