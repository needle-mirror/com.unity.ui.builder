using UnityEngine.UIElements;
using UnityEditor.UIElements.Debugger;
using System.Text;
using UnityEngine.UIElements.StyleSheets;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorInheritedStyles : IBuilderInspectorSection
    {
        private BuilderInspector m_Inspector;
        private BuilderSelection m_Selection;
        private Builder m_Builder;
        private BuilderInspectorMatchingSelectors m_MatchingSelectors;

        private PersistedFoldout m_InheritedStylesSection;
        private VisualElement m_ClassListContainer;
        private PersistedFoldout m_MatchingSelectorsFoldout;

        private TextField m_AddClassField;
        private Button m_AddClassButton;
        private Button m_CreateClassButton;

        private VisualElement m_AddClassValidationMessageContainer;

        private VisualTreeAsset m_ClassPillTemplate;

        private string m_AddClassValidationMessage = string.Empty;
        private Regex m_AddClassValidationRegex;

        private VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        public VisualElement root => m_InheritedStylesSection;

        public BuilderInspectorInheritedStyles(BuilderInspector inspector, BuilderInspectorMatchingSelectors matchingSelectors)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;
            m_Builder = inspector.builder;
            m_MatchingSelectors = matchingSelectors;

            m_InheritedStylesSection = m_Inspector.Q<PersistedFoldout>("inspector-inherited-styles-foldout");
            m_ClassListContainer = m_Inspector.Q("class-list-container");
            m_MatchingSelectorsFoldout = m_Inspector.Q<PersistedFoldout>("matching-selectors-container");

            m_AddClassField = m_Inspector.Q<TextField>("add-class-field");
            m_AddClassField.isDelayed = true;
            m_AddClassField.RegisterCallback<KeyUpEvent>(OnAddClassFieldChange);

            m_AddClassButton = m_Inspector.Q<Button>("add-class-button");
            m_CreateClassButton = m_Inspector.Q<Button>("create-class-button");

            m_AddClassValidationMessageContainer = m_Inspector.Q("add-class-validation-message-container");
            m_AddClassValidationMessageContainer.Add(new IMGUIContainer(DrawAddClassValidationMessage));
            m_AddClassValidationMessageContainer.style.display = DisplayStyle.None;

            m_ClassPillTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderClassPill.uxml");

            m_AddClassButton.clickable.clicked += AddStyleClass;
            m_CreateClassButton.clickable.clicked += ExtractLocalStylesToNewClass;

            m_AddClassValidationRegex = new Regex(@"^[a-zA-Z0-9\-_]+$");
        }

        public void Enable()
        {
            m_Inspector.Query<Button>().ForEach(e =>
            {
                e.SetEnabled(true);
            });
            m_AddClassField.SetEnabled(true);
            m_ClassListContainer.SetEnabled(true);
        }

        public void Disable()
        {
            m_Inspector.Query<Button>().ForEach(e =>
            {
                e.SetEnabled(false);
            });
            m_AddClassField.SetEnabled(false);
            m_ClassListContainer.SetEnabled(false);
        }

        private void DrawAddClassValidationMessage()
        {
            EditorGUILayout.HelpBox(m_AddClassValidationMessage, MessageType.Info, true);
        }

        private void OnAddClassFieldChange(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Return)
                return;

            AddStyleClass();

            evt.StopPropagation();
            evt.PreventDefault();

            m_AddClassField.Focus();
        }

        private bool VerifyNewClassNameIsValid(string className)
        {
            ClearAddStyleValidationWarning();

            if (string.IsNullOrEmpty(className))
                return false;

            if (className.StartsWith("."))
            {
                DisplayAddStyleValidationWarning(BuilderConstants.AddStyleClassValidationStartsWithDot);
                return false;
            }
            else if (className.Contains(" "))
            {
                DisplayAddStyleValidationWarning(BuilderConstants.AddStyleClassValidationSpaces);
                return false;
            }
            else if (!m_AddClassValidationRegex.IsMatch(className))
            {
                DisplayAddStyleValidationWarning(BuilderConstants.AddStyleClassValidationSpacialCharacters);
                return false;
            }

            return true;
        }

        private void AddStyleClass()
        {
            var className = m_AddClassField.value;

            if (!VerifyNewClassNameIsValid(className))
                return;

            AddStyleClass(className);
        }

        private void ExtractLocalStylesToNewClass()
        {
            var className = m_AddClassField.value;

            if (!VerifyNewClassNameIsValid(className))
                return;

            ExtractLocalStylesToNewClass(className);
        }

        private void PreAddStyleClass(string className)
        {
            m_AddClassField.SetValueWithoutNotify(string.Empty);

            // Remove temporary min-size class on VisualElement.
            currentVisualElement.RemoveMinSizeSpecialElement();

            // Actually add the style class to the element in the canvas.
            currentVisualElement.AddToClassList(className);
        }

        private void AddStyleClass(string className)
        {
            PreAddStyleClass(className);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.AddStyleClassToElementInAsset(
                m_Builder.document, currentVisualElement, className);

            // We actually want to get the notification back and refresh ourselves.
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
        }

        private void ExtractLocalStylesToNewClass(string className)
        {
            PreAddStyleClass(className);

            // Create new selector in main StyleSheet.
            var selectorString = "." + className;
            var mainStyleSheet = m_Builder.document.mainStyleSheet;
            var selectorsRootElement = BuilderSharedStyles.GetSelectorContainerElement(m_Selection.documentElement);
            var newSelector = BuilderSharedStyles.CreateNewSelector(selectorsRootElement, mainStyleSheet, selectorString);

            // Transfer all properties from inline styles rule to new selector.
            mainStyleSheet.TransferRulePropertiesToSelector(
                newSelector, m_Inspector.styleSheet, m_Inspector.currentRule);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.AddStyleClassToElementInAsset(
                m_Builder.document, currentVisualElement, className);

            // Overwrite Undo Message.
            Undo.RegisterCompleteObjectUndo(
                new Object[] { m_Builder.document.visualTreeAsset, m_Builder.document.mainStyleSheet },
                BuilderConstants.CreateStyleClassUndoMessage);

            // We actually want to get the notification back and refresh ourselves.
            m_Selection.NotifyOfStylingChange(null);
            m_Selection.NotifyOfHierarchyChange(null, currentVisualElement);
        }

        private void OnStyleClassDelete(EventBase evt)
        {
            var target = evt.target as VisualElement;
            var className = target.userData as string;

            // Remove temporary min-size class on VisualElement.
            currentVisualElement.RemoveMinSizeSpecialElement();

            // Actually remove the style class from the element in the canvas.
            currentVisualElement.RemoveFromClassList(className);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.RemoveStyleClassToElementInAsset(
                m_Builder.document, currentVisualElement, className);

            // We actually want to get the notification back and refresh ourselves.
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
        }

        private void DisplayAddStyleValidationWarning(string warningMessage)
        {
            m_AddClassValidationMessage = warningMessage;
            m_AddClassValidationMessageContainer.style.display = DisplayStyle.Flex;
        }

        private void ClearAddStyleValidationWarning()
        {
            m_AddClassValidationMessageContainer.style.display = DisplayStyle.None;
            m_AddClassValidationMessage = string.Empty;
        }

        private void RefreshClassListContainer()
        {
            ClearAddStyleValidationWarning();

            m_ClassListContainer.Clear();
            if (BuilderSharedStyles.IsSelectorElement(currentVisualElement))
                return;

            foreach (var className in currentVisualElement.GetClasses())
            {
                m_ClassPillTemplate.CloneTree(m_ClassListContainer.contentContainer);
                var pill = m_ClassListContainer.contentContainer.ElementAt(m_ClassListContainer.childCount - 1);
                var pillLabel = pill.Q<Label>("class-name-label");
                var pillDeleteButton = pill.Q<Button>("delete-class-button");

                // Add ellipsis if the class name is too long.
                var classNameShortened = BuilderNameUtilities.CapStringLengthAndAddEllipsis(className, BuilderConstants.ClassNameInPillMaxLength);
                pillLabel.text = "." + classNameShortened;

                pillDeleteButton.userData = className;
                pillDeleteButton.clickable.clickedWithEventInfo += OnStyleClassDelete;
            }
        }

        private VisualElement GeneratedMatchingSelectors()
        {
            m_MatchingSelectors.GetElementMatchers();
            if (m_MatchingSelectors.matchedRulesExtractor.selectedElementRules == null ||
                m_MatchingSelectors.matchedRulesExtractor.selectedElementRules.Count <= 0)
                return null;

            var container = new VisualElement();

            int ruleIndex = 0;
            foreach (MatchedRulesExtractor.MatchedRule rule in m_MatchingSelectors.matchedRulesExtractor.selectedElementRules)
            {
                var selectorStr = StyleSheetToUss.ToUssSelector(rule.matchRecord.complexSelector);

                StyleProperty[] props = rule.matchRecord.complexSelector.rule.properties;
                var ruleFoldout = new PersistedFoldout()
                {
                    value = false,
                    text = selectorStr,
                    viewDataKey = "builder-inspector-rule-foldout__" + ruleIndex
                };
                ruleIndex++;
                container.Add(ruleFoldout);

                if (props.Length == 0)
                {
                    var label = new Label("None");
                    label.AddToClassList(BuilderConstants.InspectorEmptyFoldoutLabelClassName);
                    ruleFoldout.Add(label);
                    continue;
                }

                for (int j = 0; j < props.Length; j++)
                {
                    string s = "";
                    for (int k = 0; k < props[j].values.Length; k++)
                    {
                        if (k > 0)
                            s += " ";

                        var str = rule.matchRecord.sheet.ReadAsString(props[j].values[k]);
                        s += str;
                    }

                    s = s.ToLower();
                    var textField = new TextField(props[j].name) { value = s };
                    textField.isReadOnly = true;
                    ruleFoldout.Add(textField);
                }
            }

            return container;
        }

        private void RefreshMatchingSelectorsContainer()
        {
            m_MatchingSelectorsFoldout.Clear();
            if (BuilderSharedStyles.IsSelectorElement(currentVisualElement))
                return;

            VisualElement matchingSelectors = GeneratedMatchingSelectors();
            if (matchingSelectors != null)
            {
                m_MatchingSelectorsFoldout.Add(matchingSelectors);

                // Forward focus to the panel header.
                matchingSelectors
                    .Query()
                    .Where(e => e.focusable)
                    .ForEach((e) => m_Inspector.AddFocusable(e));
            }
            else
            {
                var label = new Label("None");
                label.AddToClassList(BuilderConstants.InspectorEmptyFoldoutLabelClassName);
                m_MatchingSelectorsFoldout.Add(label);
            }
        }

        public void Refresh()
        {
            RefreshClassListContainer();
            RefreshMatchingSelectorsContainer();
        }
    }
}
