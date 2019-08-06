using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class VisualTreeAssetExtensions
    {
        public static readonly FieldInfo UsingsListFieldInfo =
            typeof(VisualTreeAsset).GetField("m_Usings", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly IComparer<VisualTreeAsset.UsingEntry> s_UsingEntryPathComparer = new UsingEntryPathComparer();

        private class UsingEntryPathComparer : IComparer<VisualTreeAsset.UsingEntry>
        {
            public int Compare(VisualTreeAsset.UsingEntry x, VisualTreeAsset.UsingEntry y)
            {
                return Comparer<string>.Default.Compare(x.path, y.path);
            }
        }

        public static VisualTreeAsset DeepCopy(this VisualTreeAsset vta)
        {
            var newTreeAsset = VisualTreeAssetUtilities.CreateInstance();

            var json = JsonUtility.ToJson(vta);
            JsonUtility.FromJsonOverwrite(json, newTreeAsset);

            if (vta.inlineSheet != null)
                newTreeAsset.inlineSheet = vta.inlineSheet.DeepCopy();

            return newTreeAsset;
        }

        internal static string GenerateUXML(this VisualTreeAsset vta)
        {
            return VisualTreeAssetToUXML.GenerateUXML(vta);
        }

        internal static void LinkedCloneTree(this VisualTreeAsset vta, VisualElement target)
        {
            VisualTreeAssetLinkedCloneTree.CloneTree(vta, target);
        }

        internal static VisualElementAsset FindElementByType(this VisualTreeAsset vta, string fullTypeName)
        {
            foreach (var vea in vta.visualElementAssets)
            {
                if (vea.fullTypeName == fullTypeName)
                    return vea;
            }
            foreach (var vea in vta.templateAssets)
            {
                if (vea.fullTypeName == fullTypeName)
                    return vea;
            }
            return null;
        }

        internal static List<VisualElementAsset> FindElementsByType(this VisualTreeAsset vta, string fullTypeName)
        {
            var foundList = new List<VisualElementAsset>();
            foreach (var vea in vta.visualElementAssets)
            {
                if (vea.fullTypeName == fullTypeName)
                    foundList.Add(vea);
            }
            foreach (var vea in vta.templateAssets)
            {
                if (vea.fullTypeName == fullTypeName)
                    foundList.Add(vea);
            }
            return foundList;
        }

        internal static VisualElementAsset FindElementByName(this VisualTreeAsset vta, string name)
        {
            foreach (var vea in vta.visualElementAssets)
            {
                string currentName;
                vea.TryGetAttributeValue("name", out currentName);
                if (currentName == name)
                    return vea;
            }
            foreach (var vea in vta.templateAssets)
            {
                string currentName;
                vea.TryGetAttributeValue("name", out currentName);
                if (currentName == name)
                    return vea;
            }
            return null;
        }

        internal static List<VisualElementAsset> FindElementsByName(this VisualTreeAsset vta, string name)
        {
            var foundList = new List<VisualElementAsset>();
            foreach (var vea in vta.visualElementAssets)
            {
                string currentName;
                vea.TryGetAttributeValue("name", out currentName);
                if (currentName == name)
                    foundList.Add(vea);
            }
            foreach (var vea in vta.templateAssets)
            {
                string currentName;
                vea.TryGetAttributeValue("name", out currentName);
                if (currentName == name)
                    foundList.Add(vea);
            }
            return foundList;
        }

        internal static List<VisualElementAsset> FindElementsByClass(this VisualTreeAsset vta, string className)
        {
            var foundList = new List<VisualElementAsset>();
            foreach (var vea in vta.visualElementAssets)
            {
                if (vea.classes.Contains(className))
                    foundList.Add(vea);
            }
            foreach (var vea in vta.templateAssets)
            {
                if (vea.classes.Contains(className))
                    foundList.Add(vea);
            }
            return foundList;
        }

        internal static List<StyleSheet> GetAllReferencedStyleSheets(this VisualTreeAsset vta)
        {
            var sheets = new HashSet<StyleSheet>();
            foreach (var asset in vta.visualElementAssets)
            {
                var styleSheetPaths = asset.GetStyleSheetPaths();
                if (styleSheetPaths == null)
                    continue;

                foreach (var sheetPath in styleSheetPaths)
                {
                    var sheetAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(sheetPath);
                    if (sheetAsset == null)
                    {
                        sheetAsset = Resources.Load<StyleSheet>(sheetPath);
                        if (sheetAsset == null)
                            continue;
                    }

                    sheets.Add(sheetAsset);
                }
            }

            return sheets.ToList();
        }

        public static string GetPathFromTemplateName(this VisualTreeAsset vta, string templateName)
        {
            var templateAsset = vta.ResolveTemplate(templateName);
            if (templateAsset == null)
                return null;

            return AssetDatabase.GetAssetPath(templateAsset);
        }

        public static string GetTemplateNameFromPath(this VisualTreeAsset vta, string path)
        {
            var fieldInfo = UsingsListFieldInfo;
            if (fieldInfo != null)
            {
                var usings = fieldInfo.GetValue(vta) as List<VisualTreeAsset.UsingEntry>;
                if (usings != null && usings.Count > 0)
                {
                    var lookingFor = new VisualTreeAsset.UsingEntry(null, path);
                    int index = usings.BinarySearch(lookingFor, s_UsingEntryPathComparer);
                    if (index >= 0 && usings[index].path == path)
                    {
                        return usings[index].alias;
                    }
                }
            }
            else
            {
                Debug.LogError("UI Builder: VisualTreeAsset.m_Usings private field has not been found! Update the reflection code!");
            }

            return Path.GetFileNameWithoutExtension(path);
        }

        public static TemplateAsset AddTemplateInstance(
            this VisualTreeAsset vta, VisualElementAsset parent, string path)
        {
            var templateName = vta.GetTemplateNameFromPath(path);
            if (!vta.TemplateExists(templateName))
                vta.RegisterTemplate(templateName, path);

            var templateAsset = new TemplateAsset(templateName);
            VisualTreeAssetUtilities.InitializeElement(templateAsset);

            templateAsset.AddProperty("template", templateName);

            return VisualTreeAssetUtilities.AddElementToDocument(vta, templateAsset, parent) as TemplateAsset;
        }

        internal static VisualElementAsset AddElement(
            this VisualTreeAsset vta, VisualElementAsset parent, string fullTypeName, int index = -1)
        {
            var vea = new VisualElementAsset(fullTypeName);
            VisualTreeAssetUtilities.InitializeElement(vea);
            return VisualTreeAssetUtilities.AddElementToDocument(vta, vea, parent);
        }

        internal static VisualElementAsset AddElement(
            this VisualTreeAsset vta, VisualElementAsset parent, VisualElement visualElement, int index = -1)
        {
            var fullTypeName = visualElement.GetType().ToString();
            var vea = new VisualElementAsset(visualElement.GetType().ToString());
            VisualTreeAssetUtilities.InitializeElement(vea);

            visualElement.SetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName, vea);

            var overriddenAttributes = visualElement.GetOverriddenAttributes();
            foreach (var attribute in overriddenAttributes)
                vea.AddProperty(attribute.Key, attribute.Value);

            return VisualTreeAssetUtilities.AddElementToDocument(vta, vea, parent);
        }

        internal static VisualElementAsset AddElement(
            this VisualTreeAsset vta, VisualElementAsset parent, VisualElementAsset vea)
        {
            return VisualTreeAssetUtilities.AddElementToDocument(vta, vea, parent);
        }

        public static void RemoveElement(
            this VisualTreeAsset vta, VisualElementAsset element)
        {
            if (element is TemplateAsset)
                vta.templateAssets.Remove(element as TemplateAsset);
            else
                vta.visualElementAssets.Remove(element);
        }

        public static void ReparentElement(
            this VisualTreeAsset vta,
            VisualElementAsset elementToReparent,
            VisualElementAsset newParent,
            int index = -1)
        {
            VisualTreeAssetUtilities.ReparentElementInDocument(vta, elementToReparent, newParent, index);
        }

        public static StyleRule GetOrCreateInlineStyleRule(this VisualTreeAsset vta, VisualElementAsset vea)
        {
            bool wasCreated;
            return vta.GetOrCreateInlineStyleRule(vea, out wasCreated);
        }

        public static StyleRule GetOrCreateInlineStyleRule(this VisualTreeAsset vta, VisualElementAsset vea, out bool wasCreated)
        {
            wasCreated = vea.ruleIndex < 0;
            if (wasCreated)
            {
                if (vta.inlineSheet == null)
                {
                    var newSheet = StyleSheetUtilities.CreateInstance();
                    vta.inlineSheet = newSheet;
                }

                vea.ruleIndex = vta.inlineSheet.AddRule();
            }

            return vta.inlineSheet.GetRule(vea.ruleIndex);
        }

        public static void FixStyleSheetPaths(this VisualTreeAsset vta, string instanceId, string ussPath)
        {
            vta.ReplaceStyleSheetPaths(BuilderConstants.VisualTreeAssetStyleSheetPathAsInstanceIdSchemeName + instanceId, ussPath);
        }

        public static void ReplaceStyleSheetPaths(this VisualTreeAsset vta, string oldUssPath, string newUssPath)
        {
            if (oldUssPath == newUssPath)
                return;

            foreach (var element in vta.visualElementAssets)
            {
                var styleSheetPaths = element.GetStyleSheetPaths();
                if (styleSheetPaths != null)
                {
                    for (int i = 0; i < styleSheetPaths.Count(); ++i)
                    {
                        var styleSheetPath = styleSheetPaths[i];
                        if (styleSheetPath != oldUssPath && oldUssPath != String.Empty)
                            continue;

                        styleSheetPaths[i] = newUssPath;
                    }
                }
            }
        }

        public static bool IsSelected(this VisualTreeAsset vta)
        {
            var foundElement = vta.FindElementByType(BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName);
            return foundElement != null;
        }

        public static void Swallow(this VisualTreeAsset vta, VisualElementAsset parent, VisualTreeAsset other)
        {
            var otherIdToChildren = VisualTreeAssetUtilities.GenerateIdToChildren(other);

            var nextOrderInDocument = vta.visualElementAssets.Count + vta.templateAssets.Count;

            foreach (var vea in other.visualElementAssets)
            {
                ReinitElementWithNewParentAsset(
                    vta, parent, other, otherIdToChildren, vea, ref nextOrderInDocument);

                vta.visualElementAssets.Add(vea);
            }

            foreach (var vea in other.templateAssets)
            {
                ReinitElementWithNewParentAsset(
                    vta, parent, other, otherIdToChildren, vea, ref nextOrderInDocument);

                if (!vta.TemplateExists(vea.templateAlias))
                {
                    var path = other.GetPathFromTemplateName(vea.templateAlias);
                    vta.RegisterTemplate(vea.templateAlias, path);
                }

                vta.templateAssets.Add(vea);
            }

            VisualTreeAssetUtilities.ReOrderDocument(vta);
        }

        private static void ReinitElementWithNewParentAsset(
            VisualTreeAsset vta, VisualElementAsset parent, VisualTreeAsset other,
            Dictionary<int, List<VisualElementAsset>> otherIdToChildren,
            VisualElementAsset vea, ref int nextOrderInDocument)
        {
            SwallowStyleRule(vta, other, vea);

            // Set new parent id on root elements.
            if (vea.parentId == 0 && parent != null)
                vea.parentId = parent.id;

            // Set order in document.
            vea.orderInDocument = nextOrderInDocument;
            nextOrderInDocument++;

            // Create new id and update parentId in children.
            var oldId = vea.id;
            vea.id = VisualTreeAssetUtilities.GenerateNewId(vta, vea);
            List<VisualElementAsset> children;
            if (otherIdToChildren.TryGetValue(oldId, out children) && children != null)
                foreach (var child in children)
                    child.parentId = vea.id;
        }

        private static void SwallowStyleRule(VisualTreeAsset vta, VisualTreeAsset other, VisualElementAsset vea)
        {
            if (vea.ruleIndex < 0)
                return;

            if (vta.inlineSheet == null)
                vta.inlineSheet = StyleSheetUtilities.CreateInstance();

            var toStyleSheet = vta.inlineSheet;
            var fromStyleSheet = other.inlineSheet;

            var rule = fromStyleSheet.rules[vea.ruleIndex];

            // Add rule to StyleSheet.
            var rulesList = toStyleSheet.rules.ToList();
            var index = rulesList.Count;
            rulesList.Add(rule);
            toStyleSheet.rules = rulesList.ToArray();

            // Add property values to sheet.
            foreach (var property in rule.properties)
            {
                for (int i = 0; i < property.values.Length; ++i)
                {
                    var valueHandle = property.values[i];
                    valueHandle.valueIndex =
                        toStyleSheet.TransferStyleValue(fromStyleSheet, valueHandle);
                    property.values[i] = valueHandle;
                }
            }

            vea.ruleIndex = index;
        }
    }
}
