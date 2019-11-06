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
        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;
        BuilderPaneWindow m_PaneWindow;
        BuilderInspectorMatchingSelectors m_MatchingSelectors;

        PersistedFoldout m_InheritedStylesSection;
        VisualElement m_ClassListContainer;
        PersistedFoldout m_MatchingSelectorsFoldout;

        TextField m_AddClassField;
        Button m_AddClassButton;
        Button m_CreateClassButton;

        VisualElement m_AddClassValidationMessageContainer;

        VisualTreeAsset m_ClassPillTemplate;

        string m_AddClassValidationMessage = string.Empty;
        Regex m_AddClassValidationRegex;

        VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        public VisualElement root => m_InheritedStylesSection;

        public BuilderInspectorInheritedStyles(BuilderInspector inspector, BuilderInspectorMatchingSelectors matchingSelectors)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;
            m_PaneWindow = inspector.paneWindow;
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

        void DrawAddClassValidationMessage()
        {
            EditorGUILayout.HelpBox(m_AddClassValidationMessage, MessageType.Info, true);
        }

        void OnAddClassFieldChange(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Return)
                return;

            AddStyleClass();

            evt.StopPropagation();
            evt.PreventDefault();

            m_AddClassField.Focus();
        }

        bool VerifyNewClassNameIsValid(string className)
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

        void AddStyleClass()
        {
            var className = m_AddClassField.value;

            if (!VerifyNewClassNameIsValid(className))
                return;

            AddStyleClass(className);
        }

        void ExtractLocalStylesToNewClass()
        {
            var className = m_AddClassField.value;

            if (!VerifyNewClassNameIsValid(className))
                return;

            ExtractLocalStylesToNewClass(className);
        }

        void PreAddStyleClass(string className)
        {
            m_AddClassField.SetValueWithoutNotify(string.Empty);

            // Remove temporary min-size class on VisualElement.
            currentVisualElement.RemoveMinSizeSpecialElement();

            // Actually add the style class to the element in the canvas.
            currentVisualElement.AddToClassList(className);
        }

        void AddStyleClass(string className)
        {
            PreAddStyleClass(className);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.AddStyleClassToElementInAsset(
                m_PaneWindow.document, currentVisualElement, className);

            // We actually want to get the notification back and refresh ourselves.
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
        }

        void ExtractLocalStylesToNewClass(string className)
        {
            PreAddStyleClass(className);

            // Create new selector in main StyleSheet.
            var selectorString = "." + className;
            var mainStyleSheet = m_PaneWindow.document.mainStyleSheet;
            var selectorsRootElement = BuilderSharedStyles.GetSelectorContainerElement(m_Selection.documentElement);
            var newSelector = BuilderSharedStyles.CreateNewSelector(selectorsRootElement, mainStyleSheet, selectorString);

            // Transfer all properties from inline styles rule to new selector.
            mainStyleSheet.TransferRulePropertiesToSelector(
                newSelector, m_Inspector.styleSheet, m_Inspector.currentRule);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.AddStyleClassToElementInAsset(
                m_PaneWindow.document, currentVisualElement, className);

            // Overwrite Undo Message.
            Undo.RegisterCompleteObjectUndo(
                new Object[] { m_PaneWindow.document.visualTreeAsset, m_PaneWindow.document.mainStyleSheet },
                BuilderConstants.CreateStyleClassUndoMessage);

            // We actually want to get the notification back and refresh ourselves.
            m_Selection.NotifyOfStylingChange(null);
            m_Selection.NotifyOfHierarchyChange(null, currentVisualElement);
        }

        void OnStyleClassDelete(EventBase evt)
        {
            var target = evt.target as VisualElement;
            var className = target.userData as string;

            // Remove temporary min-size class on VisualElement.
            currentVisualElement.RemoveMinSizeSpecialElement();

            // Actually remove the style class from the element in the canvas.
            currentVisualElement.RemoveFromClassList(className);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.RemoveStyleClassToElementInAsset(
                m_PaneWindow.document, currentVisualElement, className);

            // We actually want to get the notification back and refresh ourselves.
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
        }

        void DisplayAddStyleValidationWarning(string warningMessage)
        {
            m_AddClassValidationMessage = warningMessage;
            m_AddClassValidationMessageContainer.style.display = DisplayStyle.Flex;
        }

        void ClearAddStyleValidationWarning()
        {
            m_AddClassValidationMessageContainer.style.display = DisplayStyle.None;
            m_AddClassValidationMessage = string.Empty;
        }

        Clickable CreateClassPillClickableManipulator()
        {
            var clickable = new Clickable(OnClassPillDoubleClick);
            var activator = clickable.activators[0];
            activator.clickCount = 2;
            clickable.activators[0] = activator;
            return clickable;
        }

        void RefreshClassListContainer()
        {
            ClearAddStyleValidationWarning();

            m_ClassListContainer.Clear();
            if (BuilderSharedStyles.IsSelectorElement(currentVisualElement))
                return;

            var builderWindow = m_PaneWindow as Builder;
            if (builderWindow == null)
                return;

            var documentRootElement = builderWindow.documentRootElement;

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

                // See if the class is in document as its own selector.
                var selector = BuilderSharedStyles.FindSelectorElement(documentRootElement, "." + className);
                pill.SetProperty(BuilderConstants.InspectorClassPillLinkedSelectorElementVEPropertyName, selector);
                var clickable = CreateClassPillClickableManipulator();
                pill.AddManipulator(clickable);
                if (selector == null)
                {
                    pill.AddToClassList(BuilderConstants.InspectorClassPillNotInDocumentClassName);
                    pill.tooltip = BuilderConstants.InspectorClassPillDoubleClickToCreate;
                }
                else
                {
                    pill.tooltip = BuilderConstants.InspectorClassPillDoubleClickToSelect;
                }
            }
        }

        void OnClassPillDoubleClick(EventBase evt)
        {
            var pill = evt.target as VisualElement;
            var pillDeleteButton = pill.Q<Button>("delete-class-button");
            var className = pillDeleteButton.userData as string;
            var selectorString = "." + className;
            var selectorElement = pill.GetProperty(BuilderConstants.InspectorClassPillLinkedSelectorElementVEPropertyName) as VisualElement;

            if (selectorElement == null)
            {
                var selectorsRootElement = BuilderSharedStyles.GetSelectorContainerElement(m_Selection.documentElement);
                var mainStyleSheet = m_PaneWindow.document.mainStyleSheet;
                BuilderSharedStyles.CreateNewSelector(selectorsRootElement, mainStyleSheet, selectorString);

                m_Selection.NotifyOfStylingChange();
                m_Selection.NotifyOfHierarchyChange();
            }
            else
            {
                m_Selection.Select(null, selectorElement);
            }
        }

        VisualElement GeneratedMatchingSelectors()
        {
            m_MatchingSelectors.GetElementMatchers();
            if (m_MatchingSelectors.matchedRulesExtractor.selectedElementRules == null ||
                m_MatchingSelectors.matchedRulesExtractor.selectedElementRules.Count <= 0)
                return null;

            var container = new VisualElement();

            int ruleIndex = 0;
            foreach (var rule in m_MatchingSelectors.matchedRulesExtractor.selectedElementRules)
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

        void RefreshMatchingSelectorsContainer()
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
