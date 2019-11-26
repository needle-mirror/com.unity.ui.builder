using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.UIElements.Debugger;
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

        internal static StyleComplexSelector CreateNewSelector(VisualElement selectorContainerElement, StyleSheet styleSheet, string selectorStr)
        {
            var complexSelector = styleSheet.AddSelector(selectorStr);
            var ssVE = CreateNewSelectorElement(complexSelector);
            selectorContainerElement.Add(ssVE);

            return complexSelector;
        }

        public static VisualElement FindSelectorElement(VisualElement documentRootElement, string selectorStr)
        {
            var selectorContainer = GetSelectorContainerElement(documentRootElement);
            var allSelectorElements = selectorContainer.Query().Where((e) => true).ToList();

            foreach (var selectorElement in allSelectorElements)
            {
                var complexSelector = selectorElement.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
                if (complexSelector == null)
                    continue;

                var currentSelectorStr = StyleSheetToUss.ToUssSelector(complexSelector);
                if (currentSelectorStr == selectorStr)
                    return selectorElement;
            }

            return null;
        }

        static VisualElement CreateNewSelectorElement(StyleComplexSelector complexSelector)
        {
            var ssVE = new VisualElement();

            ssVE.name = BuilderConstants.StyleSelectorElementName + complexSelector.ruleIndex;
            ssVE.style.display = DisplayStyle.None;

            ssVE.SetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName, complexSelector);

            return ssVE;
        }

        public static List<string> GetMatchingSelectorsOnElement(VisualElement documentElement)
        {
            var matchedElementsSelector = new MatchedRulesExtractor();
            matchedElementsSelector.FindMatchingRules(documentElement);

            if (matchedElementsSelector.selectedElementRules == null || matchedElementsSelector.selectedElementRules.Count <= 0)
                return null;

            var complexSelectors = new List<string>();
            foreach (var rule in matchedElementsSelector.selectedElementRules)
            {
                var complexSelector = rule.matchRecord.complexSelector;
                var complexSelectorString = StyleSheetToUss.ToUssSelector(complexSelector);
                complexSelectors.Add(complexSelectorString);
            }

            return complexSelectors;
        }

        public static List<VisualElement> GetMatchingElementsForSelector(VisualElement documentRootElement, string selectorStr)
        {
            var allElements = documentRootElement.Query().Where((e) => true);
            var matchedElements = new List<VisualElement>();

            if (allElements == null)
                return matchedElements;

            // TODO: Seems we are calling this before the DefaultCommon stylesheet has been fully initialized
            // (during OnEnable()). Need to fix this at some point. But for now, you just won't have the matching
            // selectors highlight properly after initial load.
            try
            {
                allElements.ForEach((e) =>
                {
                    var matchedSelectors = GetMatchingSelectorsOnElement(e);
                    if (matchedSelectors != null && matchedSelectors.Contains(selectorStr))
                        matchedElements.Add(e);
                });
            }
            catch { }

            return matchedElements;
        }
    }
}