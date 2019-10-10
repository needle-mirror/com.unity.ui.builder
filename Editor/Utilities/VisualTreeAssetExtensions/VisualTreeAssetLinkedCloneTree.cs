using System.Collections.Generic;
using UnityEngine.Assertions;
using System;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal static class VisualTreeAssetLinkedCloneTree
    {
#if UNITY_2019_3_OR_NEWER
        static readonly StylePropertyReader s_StylePropertyReader = new StylePropertyReader();
#endif
        static readonly Dictionary<string, VisualElement> s_TemporarySlotInsertionPoints = new Dictionary<string, VisualElement>();

        static VisualElement CloneSetupRecursively(VisualTreeAsset vta, VisualElementAsset root,
            Dictionary<int, List<VisualElementAsset>> idToChildren, CreationContext context)
        {
            // This is needed because of asset reloads during domain reloads where the
            // stylesheet path might be a temporary instance id to a pure in-memory stylesheet.
            List<string> originalStyleSheets = null;
#if UNITY_2019_3_OR_NEWER
            if (root.stylesheetPaths != null && root.stylesheetPaths.Count > 0)
#else
            if (root.stylesheets != null && root.stylesheets.Count > 0)
#endif
            {
                originalStyleSheets = root.GetStyleSheetPaths();
                var strippedList = originalStyleSheets.Where(
                    (s) => !s.StartsWith(BuilderConstants.VisualTreeAssetStyleSheetPathAsInstanceIdSchemeName));

#if UNITY_2019_3_OR_NEWER
                root.stylesheetPaths = strippedList.ToList();
#else
                root.stylesheets = strippedList.ToList();
#endif
            }

#if UNITY_2019_3_OR_NEWER
            var resolvedSheets = new List<StyleSheet>();
            foreach (var sheetPath in root.stylesheetPaths)
            {
                resolvedSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(sheetPath));
            }
            root.stylesheets = resolvedSheets;
#endif

            var ve = VisualTreeAsset.Create(root, context);

            // Restore stylesheets.
            if (originalStyleSheets != null)
#if UNITY_2019_3_OR_NEWER
                root.stylesheetPaths = originalStyleSheets;
#else
                root.stylesheets = originalStyleSheets;
#endif

            // Linking the new element with its VisualElementAsset.
            // All this copied code for this one line!
            ve.SetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName, root);

            // context.target is the created templateContainer
            if (root.id == context.visualTreeAsset.contentContainerId)
            {
                if (context.target is TemplateContainer)
                    ((TemplateContainer)context.target).SetContentContainer(ve);
                else
                    Debug.LogError(
                        "Trying to clone a VisualTreeAsset with a custom content container into a element which is not a template container");
            }

            // if the current element had a slot-name attribute, put it in the resulting slot mapping
            string slotName;
            if (context.slotInsertionPoints != null && vta.TryGetSlotInsertionPoint(root.id, out slotName))
            {
                context.slotInsertionPoints.Add(slotName, ve);
            }

            if (root.classes != null)
            {
                for (int i = 0; i < root.classes.Length; i++)
                {
                    ve.AddToClassList(root.classes[i]);
                }
            }

            if (root.ruleIndex != -1)
            {
                if (vta.inlineSheet == null)
                    Debug.LogWarning("VisualElementAsset has a RuleIndex but no inlineStyleSheet");
                else
                {
                    var rule = vta.inlineSheet.rules[root.ruleIndex];
#if UNITY_2020_1_OR_NEWER
                    ve.SetInlineRule(vta.inlineSheet, rule);
#elif UNITY_2019_3_OR_NEWER
                    var stylesData = new VisualElementStylesData(false);
                    ve.SetInlineStyles(stylesData);
                    s_StylePropertyReader.SetInlineContext(vta.inlineSheet, rule, root.ruleIndex);
                    stylesData.ApplyProperties(s_StylePropertyReader, null);
#else
                    var stylesData = new VisualElementStylesData(false);
                    ve.SetInlineStyles(stylesData);
                    var propIds = StyleSheetCache.GetPropertyIDs(vta.inlineSheet, root.ruleIndex);
                    stylesData.ApplyRule(vta.inlineSheet, Int32.MaxValue, rule, propIds);
#endif
                }
            }

            var templateAsset = root as TemplateAsset;
            if (templateAsset != null)
            {
                var templatePath = vta.GetPathFromTemplateName(templateAsset.templateAlias);
                ve.SetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName, templatePath);
            }

            List<VisualElementAsset> children;
            if (idToChildren.TryGetValue(root.id, out children))
            {
                children.Sort(VisualTreeAssetUtilities.CompareForOrder);

                foreach (VisualElementAsset childVea in children)
                {
                    // this will fill the slotInsertionPoints mapping
                    VisualElement childVe = CloneSetupRecursively(vta, childVea, idToChildren, context);
                    if (childVe == null)
                        continue;

                    // if the parent is not a template asset, just add the child to whatever hierarchy we currently have
                    // if ve is a scrollView (with contentViewport as contentContainer), this will go to the right place
                    if (templateAsset == null)
                    {
                        ve.Add(childVe);
                        continue;
                    }

                    int index = templateAsset.slotUsages == null
                        ? -1
                        : templateAsset.slotUsages.FindIndex(u => u.assetId == childVea.id);
                    if (index != -1)
                    {
                        VisualElement parentSlot;
                        string key = templateAsset.slotUsages[index].slotName;
                        Assert.IsFalse(String.IsNullOrEmpty(key),
                            "a lost name should not be null or empty, this probably points to an importer or serialization bug");
                        if (context.slotInsertionPoints == null ||
                            !context.slotInsertionPoints.TryGetValue(key, out parentSlot))
                        {
                            Debug.LogErrorFormat("Slot '{0}' was not found. Existing slots: {1}", key,
                                context.slotInsertionPoints == null
                                ? String.Empty
                                : String.Join(", ",
                                    System.Linq.Enumerable.ToArray(context.slotInsertionPoints.Keys)));
                            ve.Add(childVe);
                        }
                        else
                            parentSlot.Add(childVe);
                    }
                    else
                        ve.Add(childVe);
                }
            }

            if (templateAsset != null && context.slotInsertionPoints != null)
                context.slotInsertionPoints.Clear();

            return ve;
        }

        public static void CloneTree(
            VisualTreeAsset vta, VisualElement target,
            Dictionary<string, VisualElement> slotInsertionPoints,
            List<TemplateAsset.AttributeOverride> attributeOverrides)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if ((vta.visualElementAssets == null || vta.visualElementAssets.Count <= 0) &&
                (vta.templateAssets == null || vta.templateAssets.Count <= 0))
                return;

            var idToChildren = VisualTreeAssetUtilities.GenerateIdToChildren(vta);

            List<VisualElementAsset> rootAssets;

            // Tree root has parentId == 0
            idToChildren.TryGetValue(0, out rootAssets);
            if (rootAssets == null || rootAssets.Count == 0)
                return;

#if UNITY_2020_1_OR_NEWER
            //vta.AssignClassListFromAssetToElement(rootAssets[0], target);
            //vta.AssignStyleSheetFromAssetToElement(rootAssets[0], target);

            // Get the first-level elements. These will be instantiated and added to target.
            idToChildren.TryGetValue(rootAssets[0].id, out rootAssets);
            if (rootAssets == null || rootAssets.Count == 0)
                return;
#endif

            rootAssets.Sort(VisualTreeAssetUtilities.CompareForOrder);
            foreach (VisualElementAsset rootElement in rootAssets)
            {
                Assert.IsNotNull(rootElement);

                // Don't try to instatiate the special selection tracking element.
                if (rootElement.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                    continue;

                VisualElement rootVe = CloneSetupRecursively(vta, rootElement, idToChildren,
                    new CreationContext(slotInsertionPoints, attributeOverrides, vta, target));

                // if contentContainer == this, the shadow and the logical hierarchy are identical
                // otherwise, if there is a CC, we want to insert in the shadow
                target.hierarchy.Add(rootVe);
            }
        }

        public static void CloneTree(VisualTreeAsset vta, VisualElement target)
        {
            try
            {
                CloneTree(vta, target, s_TemporarySlotInsertionPoints, null);
            }
            finally
            {
                s_TemporarySlotInsertionPoints.Clear();
            }
        }
    }
}