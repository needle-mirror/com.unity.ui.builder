using UnityEngine.UIElements;
using UnityEditor.UIElements.Debugger;
using System.Text;
using UnityEngine.UIElements.StyleSheets;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Unity.UI.Builder
{
    internal partial class BuilderInspector
    {
        private PersistedFoldout m_SharedStylesSection;
        private VisualElement m_ClassListContainer;
        private PersistedFoldout m_MatchingSelectorsFoldout;

        private TextField m_AddClassField;
        private Button m_AddClassButton;

        private VisualElement m_AddClassValidationMessageContainer;

        private VisualTreeAsset m_ClassPillTemplate;

        private string m_AddClassValidationMessage = string.Empty;
        private Regex m_AddClassValidationRegex;

        private VisualElement InitSharedStyleSection()
        {
            m_SharedStylesSection = this.Q<PersistedFoldout>("inspector-shared-styles-foldout");
            m_ClassListContainer = this.Q("class-list-container");
            m_MatchingSelectorsFoldout = this.Q<PersistedFoldout>("matching-selectors-container");

            m_AddClassField = this.Q<TextField>("add-class-field");
            m_AddClassField.isDelayed = true;
            m_AddClassField.RegisterCallback<KeyUpEvent>(OnAddClassFieldChange);

            m_AddClassButton = this.Q<Button>("add-class-button");

            m_AddClassValidationMessageContainer = this.Q("add-class-validation-message-container");
            m_AddClassValidationMessageContainer.Add(new IMGUIContainer(DrawAddClassValidationMessage));
            m_AddClassValidationMessageContainer.style.display = DisplayStyle.None;

            m_ClassPillTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Builder/BuilderClassPill.uxml");

            m_AddClassButton.clickable.clicked += AddStyleClass;

            m_AddClassValidationRegex = new Regex(@"^[a-zA-Z0-9\-_]+$");

            return m_SharedStylesSection;
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

        private void AddStyleClass()
        {
            ClearAddStyleValidationWarning();

            var className = m_AddClassField.value;

            if (string.IsNullOrEmpty(className))
                return;

            if (className.StartsWith("."))
            {
                DisplayAddStyleValidationWarning(BuilderConstants.AddStyleClassValidationStartsWithDot);
                return;
            }
            else if (className.Contains(" "))
            {
                DisplayAddStyleValidationWarning(BuilderConstants.AddStyleClassValidationSpaces);
                return;
            }
            else if (!m_AddClassValidationRegex.IsMatch(className))
            {
                DisplayAddStyleValidationWarning(BuilderConstants.AddStyleClassValidationSpacialCharacters);
                return;
            }

            AddStyleClass(className);
        }

        private void AddStyleClass(string className)
        {
            m_AddClassField.SetValueWithoutNotify(string.Empty);

            // Remove temporary min-size class on VisualElement.
            currentVisualElement.RemoveMinSizeSpecialElement();

            // Actually add the style class to the element in the canvas.
            currentVisualElement.AddToClassList(className);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.AddStyleClassToElementInAsset(
                m_Builder.document, currentVisualElement, className);

            // We actually want to get the notification back and refresh ourselves.
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
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

                pillLabel.text = className;
                pillDeleteButton.userData = className;
                pillDeleteButton.clickable.clickedWithEventInfo += OnStyleClassDelete;
            }
        }

        private VisualElement GeneratedMatchingSelectors()
        {
            GetElementMatchers();
            if (m_MatchedRulesExtractor.selectedElementRules == null || m_MatchedRulesExtractor.selectedElementRules.Count <= 0)
                return null;

            var container = new VisualElement();

            foreach (MatchedRulesExtractor.MatchedRule rule in m_MatchedRulesExtractor.selectedElementRules)
            {
                var builder = new StringBuilder();
                for (int j = 0; j < rule.matchRecord.complexSelector.selectors.Length; j++)
                {
                    var selector = rule.matchRecord.complexSelector.selectors[j];
                    switch (selector.previousRelationship)
                    {
                        case StyleSelectorRelationship.Child:
                            builder.Append(" > ");
                            break;
                        case StyleSelectorRelationship.Descendent:
                            builder.Append(" ");
                            break;
                    }
                    for (int k = 0; k < selector.parts.Length; k++)
                    {
                        var part = selector.parts[k];
                        switch (part.type)
                        {
                            case StyleSelectorType.Class:
                                builder.Append(".");
                                break;
                            case StyleSelectorType.ID:
                                builder.Append("#");
                                break;
                            case StyleSelectorType.PseudoClass:
                            case StyleSelectorType.RecursivePseudoClass:
                                builder.Append(":");
                                break;
                            case StyleSelectorType.Wildcard:
                                break;
                        }
                        builder.Append(part.value);
                    }
                }

                var selectorStr = builder.ToString();

                StyleProperty[] props = rule.matchRecord.complexSelector.rule.properties;
                var ruleFoldout = new PersistedFoldout()
                {
                    value = false,
                    text = selectorStr,
                    viewDataKey = "builder-inspector-rule-foldout__" + builder.ToString()
                };
                container.Add(ruleFoldout);

                /*bool expanded = m_CurFoldout.Contains(i);
                EditorGUILayout.BeginHorizontal();
                bool foldout = EditorGUILayout.Foldout(m_CurFoldout.Contains(i), new GUIContent(builder.ToString()), true);
                if (rule.displayPath != null && GUILayout.Button(rule.displayPath, EditorStyles.miniButton, GUILayout.MaxWidth(150)) && CanOpenStyleSheet(rule.fullPath))
                    InternalEditorUtility.OpenFileAtLineExternal(rule.fullPath, rule.lineNumber, -1);
                EditorGUILayout.EndHorizontal();

                if (expanded && !foldout)
                    m_CurFoldout.Remove(i);
                else if (!expanded && foldout)
                    m_CurFoldout.Add(i); */
                if (props.Length == 0)
                {
                    var label = new Label("None");
                    label.AddToClassList(s_EmptyFoldoutLabelClassName);
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
                matchingSelectors.Query().Where(e => e.focusable).ForEach((e) => AddFocusable(e));
            }
            else
            {
                var label = new Label("None");
                label.AddToClassList(s_EmptyFoldoutLabelClassName);
                m_MatchingSelectorsFoldout.Add(label);
            }
        }
    }
}
