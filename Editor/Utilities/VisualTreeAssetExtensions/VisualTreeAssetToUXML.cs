using System.Collections.Generic;
using UnityEditor.StyleSheets;
using System.Text;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal static class VisualTreeAssetToUXML
    {
        private static void Indent(StringBuilder stringBuilder, int depth)
        {
            for (int i = 0; i < depth; ++i)
                stringBuilder.Append("    ");
        }

        private static void AppendElementTypeName(VisualElementAsset root, StringBuilder stringBuilder)
        {
            if (root is TemplateAsset)
            {
                stringBuilder.Append("Instance");
                return;
            }

            var typeName = root.fullTypeName;
            typeName = typeName.Replace("UnityEngine.UIElements.", string.Empty);
            typeName = typeName.Replace("UnityEditor.UIElements.", "uie:");

            stringBuilder.Append(typeName);
        }

        private static void AppendElementAttribute(string name, string value, StringBuilder stringBuilder)
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

        private static void AppendElementNonStyleAttributes(VisualElementAsset vea, StringBuilder stringBuilder)
        {
            var fieldInfo = VisualElementAssetExtensions.AttributesListFieldInfo;
            if (fieldInfo == null)
            {
                Debug.LogError("UI Builder: VisualElementAsset.m_Properties private field has not been found! Update the reflection code!");
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
                    if (name == BuilderConstants.SelectedVisualElementAssetAttributeName)
                        continue;

                    // In 2019.3, je pense, "class" and "style" are now regular attributes??
                    if (name == "class" || name == "style")
                        continue;

                    AppendElementAttribute(name, value, stringBuilder);
                }
            }
        }

        private static void AppendTemplateRegistrations(
            VisualTreeAsset vta, StringBuilder stringBuilder, HashSet<string> templatesFilter = null)
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
                    stringBuilder.Append("<Template");
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
                                AppendElementAttribute("path", path, stringBuilder);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("UI Builder: VisualTreeAsset.m_Usings private field has not been found! Update the reflection code!");
                    }
                    stringBuilder.Append(" />\n");
                }
            }
        }

        private static void GatherUsedTemplates(
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

        private static void ProcessStyleSheetPath(
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
            AppendElementAttribute("path", path, stringBuilder);
            stringBuilder.Append(" />\n");

            hasChildTags = true;
        }

        private static void GenerateUXMLRecursive(
            VisualTreeAsset vta, VisualElementAsset root,
            Dictionary<int, List<VisualElementAsset>> idToChildren,
            StringBuilder stringBuilder, int depth, bool omitUnsavedUss = false)
        {
            Indent(stringBuilder, depth);

            stringBuilder.Append("<");
            AppendElementTypeName(root, stringBuilder);

            // Add all non-style attributes.
            AppendElementNonStyleAttributes(root, stringBuilder);

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
            var styleSheetPaths = root.GetStyleSheetPaths();
            if (styleSheetPaths != null && styleSheetPaths.Count > 0)
            {
                bool newLineAdded = false;

                foreach (var path in styleSheetPaths)
                {
                    ProcessStyleSheetPath(
                        path, stringBuilder, depth, omitUnsavedUss,
                        ref newLineAdded, ref hasChildTags);
                }
            }

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
                    GenerateUXMLRecursive(vta, childVea, idToChildren, stringBuilder, depth + 1, omitUnsavedUss);

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

        public static string GenerateUXML(VisualTreeAsset vta, VisualElementAsset vea)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(BuilderConstants.UxmlHeader);

            var idToChildren = VisualTreeAssetUtilities.GenerateIdToChildren(vta);

            // Templates
            var usedTemplates = new HashSet<string>();
            GatherUsedTemplates(vta, vea, idToChildren, usedTemplates);
            AppendTemplateRegistrations(vta, stringBuilder, usedTemplates);

            GenerateUXMLRecursive(vta, vea, idToChildren, stringBuilder, 1, true);

            stringBuilder.Append("</UXML>\n");

            return stringBuilder.ToString();
        }

        public static string GenerateUXML(VisualTreeAsset vta)
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
            AppendTemplateRegistrations(vta, stringBuilder);

            // all nodes under the tree root have a parentId == 0
            List<VisualElementAsset> rootAssets;
            if (idToChildren.TryGetValue(0, out rootAssets) && rootAssets != null)
            {
                rootAssets.Sort(VisualTreeAssetUtilities.CompareForOrder);

                foreach (VisualElementAsset rootElement in rootAssets)
                {
                    Assert.IsNotNull(rootElement);

                    // Don't try to include the special selection tracking element.
                    if (rootElement.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                        continue;

                    GenerateUXMLRecursive(vta, rootElement, idToChildren, stringBuilder, 1);
                }
            }

            stringBuilder.Append(BuilderConstants.UxmlFooter);

            return stringBuilder.ToString();
        }
    }
}
