using System.Collections.Generic;
using UnityEditor.StyleSheets;
using System.Text;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Unity.UI.Builder
{
    internal static class VisualTreeAssetToUXML
    {
        static void Indent(StringBuilder stringBuilder, int depth)
        {
            for (int i = 0; i < depth; ++i)
                stringBuilder.Append("    ");
        }

        static void AppendElementTypeName(VisualElementAsset root, StringBuilder stringBuilder)
        {
            if (root is TemplateAsset)
            {
                stringBuilder.Append(BuilderConstants.UxmlEngineNamespaceReplace);
                stringBuilder.Append("Instance");
                return;
            }

            var typeName = root.fullTypeName;
            if (typeName.StartsWith(BuilderConstants.UxmlEngineNamespace))
            {
                typeName = typeName.Substring(BuilderConstants.UxmlEngineNamespace.Length);
                stringBuilder.Append(BuilderConstants.UxmlEngineNamespaceReplace);
                stringBuilder.Append(typeName);
                return;
            }
            else if (typeName.StartsWith(BuilderConstants.UxmlEditorNamespace))
            {
                typeName = typeName.Substring(BuilderConstants.UxmlEditorNamespace.Length);
                stringBuilder.Append(BuilderConstants.UxmlEditorNamespaceReplace);
                stringBuilder.Append(typeName);
                return;
            }

            stringBuilder.Append(typeName);
        }

        static void AppendElementAttribute(string name, string value, StringBuilder stringBuilder)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (name == "picking-mode" && value == "Position")
                return;

            stringBuilder.Append(" ");
            stringBuilder.Append(name);
            stringBuilder.Append("=\"");
            stringBuilder.Append(value);
            stringBuilder.Append("\"");
        }

        static void AppendElementNonStyleAttributes(VisualElementAsset vea, StringBuilder stringBuilder, bool writingToFile)
        {
            var fieldInfo = VisualElementAssetExtensions.AttributesListFieldInfo;
            if (fieldInfo == null)
            {
                Debug.LogError("UI Builder: VisualElementAsset.m_Properties field has not been found! Update the reflection code!");
                return;
            }

            var attributes = fieldInfo.GetValue(vea) as List<string>;
            if (attributes != null && attributes.Count > 0)
            {
                for (int i = 0; i < attributes.Count; i += 2)
                {
                    var name = attributes[i];
                    var value = attributes[i + 1];

                    // Avoid writing the selection attribute to UXML.
                    if (writingToFile && name == BuilderConstants.SelectedVisualElementAssetAttributeName)
                        continue;

                    // In 2019.3, je pense, "class" and "style" are now regular attributes??
                    if (name == "class" || name == "style")
                        continue;

                    AppendElementAttribute(name, value, stringBuilder);
                }
            }
        }

        static void AppendTemplateRegistrations(
            VisualTreeAsset vta, string vtaPath, StringBuilder stringBuilder, HashSet<string> templatesFilter = null)
        {
            if (vta.templateAssets != null && vta.templateAssets.Count > 0)
            {
                var templatesMap = new Dictionary<string, TemplateAsset>();
                foreach (var templateAsset in vta.templateAssets)
                {
                    if (!templatesMap.ContainsKey(templateAsset.templateAlias))
                        templatesMap.Add(templateAsset.templateAlias, templateAsset);
                }
                foreach (var templateAsset in templatesMap.Values)
                {
                    // Skip templates if not in filter.
                    if (templatesFilter != null && !templatesFilter.Contains(templateAsset.templateAlias))
                        continue;

                    Indent(stringBuilder, 1);
                    stringBuilder.Append("<");
                    stringBuilder.Append(BuilderConstants.UxmlEngineNamespaceReplace);
                    stringBuilder.Append("Template");
                    AppendElementAttribute("name", templateAsset.templateAlias, stringBuilder);

                    var fieldInfo = VisualTreeAssetExtensions.UsingsListFieldInfo;
                    if (fieldInfo != null)
                    {
                        var usings = fieldInfo.GetValue(vta) as List<VisualTreeAsset.UsingEntry>;
                        if (usings != null && usings.Count > 0)
                        {
                            var lookingFor = new VisualTreeAsset.UsingEntry(templateAsset.templateAlias, string.Empty);
                            int index = usings.BinarySearch(lookingFor, VisualTreeAsset.UsingEntry.comparer);
                            if (index >= 0)
                            {
                                string path = usings[index].path;
#if UNITY_2019_3_OR_NEWER
                                path = GetProcessedPathForSrcAttribute(vtaPath, path);
                                AppendElementAttribute("src", path, stringBuilder);
#else
                                AppendElementAttribute("path", path, stringBuilder);
#endif
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("UI Builder: VisualTreeAsset.m_Usings field has not been found! Update the reflection code!");
                    }
                    stringBuilder.Append(" />\n");
                }
            }
        }

        static void GatherUsedTemplates(
            VisualTreeAsset vta, VisualElementAsset root,
            Dictionary<int, List<VisualElementAsset>> idToChildren,
            HashSet<string> templates)
        {
            if (root is TemplateAsset)
                templates.Add((root as TemplateAsset).templateAlias);

            // Iterate through child elements.
            List<VisualElementAsset> children;
            if (idToChildren != null && idToChildren.TryGetValue(root.id, out children) && children.Count > 0)
            {
                foreach (VisualElementAsset childVea in children)
                    GatherUsedTemplates(vta, childVea, idToChildren, templates);
            }
        }

        public static string GetProcessedPathForSrcAttribute(string vtaPath, string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || assetPath.StartsWith(BuilderConstants.VisualTreeAssetUnsavedUssFileMessage))
                return assetPath;

            var result = string.Empty;
            if (!string.IsNullOrEmpty(vtaPath))
            {
                var vtaDir = Path.GetDirectoryName(vtaPath);
                vtaDir = vtaDir.Replace('\\', '/');
                vtaDir += "/";

                var assetPathDir = Path.GetDirectoryName(assetPath);
                assetPathDir = assetPathDir.Replace('\\', '/');
                assetPathDir += "/";

                if (assetPathDir.StartsWith(vtaDir))
                    result = assetPath.Substring(vtaDir.Length); // +1 for the /
            }

            if (string.IsNullOrEmpty(result))
                result = "/" + assetPath;

            return result;
        }

        static void ProcessStyleSheetPath(
            string vtaPath,
            string path, StringBuilder stringBuilder, int depth, bool omitUnsavedUss,
            ref bool newLineAdded, ref bool hasChildTags)
        {
            if (path.StartsWith(BuilderConstants.VisualTreeAssetStyleSheetPathAsInstanceIdSchemeName))
            {
                if (omitUnsavedUss)
                    return;
                else
                    path = BuilderConstants.VisualTreeAssetUnsavedUssFileMessage + path;
            }

            if (!newLineAdded)
            {
                stringBuilder.Append(">\n");
                newLineAdded = true;
            }

            Indent(stringBuilder, depth + 1);
            stringBuilder.Append("<Style");
#if UNITY_2019_3_OR_NEWER
            {
                path = GetProcessedPathForSrcAttribute(vtaPath, path);
                AppendElementAttribute("src", path, stringBuilder);
            }
#else
            AppendElementAttribute("path", path, stringBuilder);
#endif
            stringBuilder.Append(" />\n");

            hasChildTags = true;
        }

        static void GenerateUXMLRecursive(
            VisualTreeAsset vta, string vtaPath, VisualElementAsset root,
            Dictionary<int, List<VisualElementAsset>> idToChildren,
            StringBuilder stringBuilder, int depth,
            bool omitUnsavedUss, bool writingToFile)
        {
            Indent(stringBuilder, depth);

            stringBuilder.Append("<");
            AppendElementTypeName(root, stringBuilder);

            // Add all non-style attributes.
            AppendElementNonStyleAttributes(root, stringBuilder, writingToFile);

            // Add style classes to class attribute.
            if (root.classes != null && root.classes.Length > 0)
            {
                stringBuilder.Append(" class=\"");
                for (int i = 0; i < root.classes.Length; i++)
                {
                    if (i > 0)
                        stringBuilder.Append(" ");

                    stringBuilder.Append(root.classes[i]);
                }
                stringBuilder.Append("\"");
            }

            // Add inline StyleSheet attribute.
            if (root.ruleIndex != -1)
            {
                if (vta.inlineSheet == null)
                    Debug.LogWarning("VisualElementAsset has a RuleIndex but no inlineStyleSheet");
                else
                {
                    StyleRule r = vta.inlineSheet.rules[root.ruleIndex];

                    if (r.properties != null && r.properties.Length > 0)
                    {
                        stringBuilder.Append(" style=\"");

                        var ruleBuilder = new StringBuilder();
                        var exportOptions = new UssExportOptions();
                        exportOptions.propertyIndent = string.Empty;
                        StyleSheetToUss.ToUssString(vta.inlineSheet, exportOptions, r, ruleBuilder);
                        var ruleStr = ruleBuilder.ToString();
                        ruleStr = ruleStr.Replace('\n', ' ');
                        ruleStr = ruleStr.Trim();
                        stringBuilder.Append(ruleStr);

                        stringBuilder.Append("\"");
                    }
                }
            }

            // If we have no children, avoid adding the full end tag and just end the open tag.
            bool hasChildTags = false;

            // Add special children.
#if UNITY_2019_3_OR_NEWER2
            var styleSheets = root.stylesheets;
            if (styleSheets != null && styleSheets.Count > 0)
            {
                bool newLineAdded = false;

                foreach (var styleSheet in styleSheets)
                {
                    var path = AssetDatabase.GetAssetPath(styleSheet);
                    ProcessStyleSheetPath(
                        vtaPath,
                        path, stringBuilder, depth, omitUnsavedUss,
                        ref newLineAdded, ref hasChildTags);
                }
            }
#else
            var styleSheetPaths = root.GetStyleSheetPaths();
            if (styleSheetPaths != null && styleSheetPaths.Count > 0)
            {
                bool newLineAdded = false;

                foreach (var path in styleSheetPaths)
                {
                    ProcessStyleSheetPath(
                        vtaPath,
                        path, stringBuilder, depth, omitUnsavedUss,
                        ref newLineAdded, ref hasChildTags);
                }
            }
#endif

            var templateAsset = root as TemplateAsset;
            if (templateAsset != null && templateAsset.attributeOverrides != null && templateAsset.attributeOverrides.Count > 0)
            {
                if (!hasChildTags)
                    stringBuilder.Append(">\n");

                var overridesMap = new Dictionary<string, List<TemplateAsset.AttributeOverride>>();
                foreach (var attributeOverride in templateAsset.attributeOverrides)
                {
                    if (!overridesMap.ContainsKey(attributeOverride.m_ElementName))
                        overridesMap.Add(attributeOverride.m_ElementName, new List<TemplateAsset.AttributeOverride>());

                    overridesMap[attributeOverride.m_ElementName].Add(attributeOverride);
                }
                foreach (var attributeOverridePair in overridesMap)
                {
                    var elementName = attributeOverridePair.Key;
                    var overrides = attributeOverridePair.Value;

                    Indent(stringBuilder, depth + 1);
                    stringBuilder.Append("<AttributeOverrides");
                    AppendElementAttribute("element-name", elementName, stringBuilder);

                    foreach (var attributeOverride in overrides)
                        AppendElementAttribute(attributeOverride.m_AttributeName, attributeOverride.m_Value, stringBuilder);

                    stringBuilder.Append(" />\n");
                }

                hasChildTags = true;
            }

            // Iterate through child elements.
            List<VisualElementAsset> children;
            if (idToChildren != null && idToChildren.TryGetValue(root.id, out children) && children.Count > 0)
            {
                if (!hasChildTags)
                    stringBuilder.Append(">\n");

                children.Sort(VisualTreeAssetUtilities.CompareForOrder);

                foreach (VisualElementAsset childVea in children)
                    GenerateUXMLRecursive(
                        vta, vtaPath, childVea, idToChildren, stringBuilder,
                        depth + 1, omitUnsavedUss, writingToFile);

                hasChildTags = true;
            }

            if (hasChildTags)
            {
                Indent(stringBuilder, depth);
                stringBuilder.Append("</");
                AppendElementTypeName(root, stringBuilder);
                stringBuilder.Append(">\n");
            }
            else
            {
                stringBuilder.Append(" />\n");
            }
        }

        public static string GenerateUXML(VisualTreeAsset vta, string vtaPath, VisualElementAsset vea)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(BuilderConstants.UxmlHeader);

            var idToChildren = VisualTreeAssetUtilities.GenerateIdToChildren(vta);

            // Templates
            var usedTemplates = new HashSet<string>();
            GatherUsedTemplates(vta, vea, idToChildren, usedTemplates);
            AppendTemplateRegistrations(vta, vtaPath, stringBuilder, usedTemplates);

            GenerateUXMLRecursive(vta, vtaPath, vea, idToChildren, stringBuilder, 1, true, true);

            stringBuilder.Append(BuilderConstants.UxmlFooter);

            return stringBuilder.ToString();
        }

        static void GenerateUXMLFromRootElements(
            VisualTreeAsset vta,
            Dictionary<int, List<VisualElementAsset>> idToChildren,
            StringBuilder stringBuilder,
            string vtaPath,
            bool writingToFile)
        {
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

                // Don't try to include the special selection tracking element.
                if (writingToFile && rootElement.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                    continue;

                GenerateUXMLRecursive(vta, vtaPath, rootElement, idToChildren, stringBuilder, 1, false, writingToFile);
            }
        }

        public static string GenerateUXML(VisualTreeAsset vta, string vtaPath, bool writingToFile = false)
        {
            var stringBuilder = new StringBuilder();

            if ((vta.visualElementAssets == null || vta.visualElementAssets.Count <= 0) &&
                (vta.templateAssets == null || vta.templateAssets.Count <= 0))
            {
                stringBuilder.Append(BuilderConstants.UxmlHeader);
                stringBuilder.Append(BuilderConstants.UxmlFooter);
                return stringBuilder.ToString();
            }

            var idToChildren = VisualTreeAssetUtilities.GenerateIdToChildren(vta);

            stringBuilder.Append(BuilderConstants.UxmlHeader);

            // Templates
            AppendTemplateRegistrations(vta, vtaPath, stringBuilder);

            GenerateUXMLFromRootElements(vta, idToChildren, stringBuilder, vtaPath, writingToFile);

            stringBuilder.Append(BuilderConstants.UxmlFooter);

            return stringBuilder.ToString();
        }
    }
}
