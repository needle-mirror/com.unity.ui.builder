using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace Unity.UI.Builder
{
    internal class BuilderSharedStyles
    {
        internal static bool IsDocumentElement(VisualElement element)
        {
            return element.name == "document" && element.ClassListContains("unity-builder-viewport__document");
        }

        public static VisualElement GetDocumentRootLevelElement(VisualElement element)
        {
            if (element == null)
                return null;

            while (element.parent != null)
            {
                if (IsDocumentElement(element.parent))
                    return element;

                element = element.parent;
            }

            return null;
        }

        internal static bool IsSelectorsContainerElement(VisualElement element)
        {
            return element.name == BuilderConstants.StyleSelectorElementContainerName;
        }

        internal static bool IsSelectorElement(VisualElement element)
        {
            return element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) != null;
        }

        public static bool IsSharedStyleSpecialElement(VisualElement element)
        {
            return IsSelectorElement(element) || IsSelectorsContainerElement(element);
        }

        internal static string GetSelectorString(VisualElement element)
        {
            var complexSelector = element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            var selectorStr = StyleSheetToUss.ToUssSelector(complexSelector);
            return selectorStr;
        }

        internal static void SetSelectorString(VisualElement element, StyleSheet styleSheet, string newString)
        {
            var complexSelector = element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            styleSheet.SetSelectorString(complexSelector, newString);
        }

        internal static List<string> GetSelectorParts(VisualElement element)
        {
            var complexSelector = element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            if (complexSelector == null)
                return null;

            var selectorParts = new List<string>();
            foreach (var selector in complexSelector.selectors)
            {
                if (selector.previousRelationship != StyleSelectorRelationship.None)
                    selectorParts.Add(selector.previousRelationship == StyleSelectorRelationship.Child ? " > " : " ");

                foreach (var selectorPart in selector.parts)
                {
                    switch (selectorPart.type)
                    {
                        case StyleSelectorType.Wildcard:
                            selectorParts.Add("*");
                            break;
                        case StyleSelectorType.Type:
                            selectorParts.Add(selectorPart.value);
                            break;
                        case StyleSelectorType.Class:
                            selectorParts.Add("." + selectorPart.value);
                            break;
                        case StyleSelectorType.PseudoClass:
                            selectorParts.Add(":" + selectorPart.value);
                            break;
                        case StyleSelectorType.ID:
                            selectorParts.Add("#" + selectorPart.value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return selectorParts;
        }

        internal static VisualElement GetSelectorContainerElement(VisualElement root)
        {
            var sharedStylesContainer = root.parent.Q(BuilderConstants.StyleSelectorElementContainerName);
            return sharedStylesContainer;
        }

        internal static void AddSelectorElementsFromStyleSheet(VisualElement documentElement, StyleSheet styleSheet)
        {
            var selectorContainerElement = GetSelectorContainerElement(documentElement);
            selectorContainerElement.SetProperty(BuilderConstants.ElementLinkedStyleSheetVEPropertyName, styleSheet);
            selectorContainerElement.Clear();

            if (styleSheet == null)
                return;

            foreach (var complexSelector in styleSheet.complexSelectors)
            {
                var complexSelectorStr = StyleSheetToUss.ToUssSelector(complexSelector);
                if (complexSelectorStr == BuilderConstants.SelectedStyleSheetSelectorName)
                    continue;

                var ssVE = CreateNewSelectorElement(complexSelector);
                selectorContainerElement.Add(ssVE);
            }
        }

        internal static void CreateNewSelector(VisualElement selectorContainerElement, StyleSheet styleSheet, string selectorStr)
        {
            var complexSelector = styleSheet.AddSelector(selectorStr);
            var ssVE = CreateNewSelectorElement(complexSelector);
            selectorContainerElement.Add(ssVE);
        }

        private static VisualElement CreateNewSelectorElement(StyleComplexSelector complexSelector)
        {
            var ssVE = new VisualElement();

            ssVE.name = BuilderConstants.StyleSelectorElementName + complexSelector.ruleIndex;
            ssVE.style.display = DisplayStyle.None;

            ssVE.SetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName, complexSelector);

            return ssVE;
        }
    }
}