using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderExplorerNewSelectorElement : VisualElement
    {
        [Flags]
        enum NewSelectorPseudoStates
        {
            Hover = 1 << 0,
            Active = 1 << 1,
            Focus = 1 << 2
        }

        public enum NewSelectorMode
        {
            Class,
            Complex
        }

        static readonly List<string> NewSelectorPseudoStatesNames = new List<string>()
        {
            "Hover", "Active", "Selected"
        };

        StyleSheet m_StyleSheet;
        VisualTreeAsset m_ExplorerItemTemplate;
        NewSelectorMode m_NewSelectorMode;

        public StyleSheet styleSheet
        {
            get { return m_StyleSheet; }
        }

        public VisualTreeAsset explorerItemTemplate
        {
            get { return m_ExplorerItemTemplate; }
        }

        public NewSelectorMode newSelectorMode
        {
            get { return m_NewSelectorMode; }
            set { m_NewSelectorMode = value; }
        }

        public BuilderExplorerNewSelectorElement(StyleSheet styleSheet)
        {
            AddToClassList(BuilderConstants.ExplorerItemUnselectableClassName);

            m_StyleSheet = styleSheet;
            m_ExplorerItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderExplorerNewSelectorItem.uxml");

            Action<VisualElement, ITreeViewItem, BuilderSelection> action = CreateInExplorerAddSimpleSelectorButton;
            SetProperty(BuilderConstants.ExplorerItemFillItemCallbackVEPropertyName, action);
        }

        static void HideShowStateMaskField(MaskField maskField, NewSelectorMode newSelectorMode)
        {
            if (newSelectorMode == NewSelectorMode.Complex)
                maskField.AddToClassList(BuilderConstants.HiddenStyleClassName);
            else
                maskField.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
        }

        static void CreateInExplorerAddSimpleSelectorButton(VisualElement parent, ITreeViewItem item, BuilderSelection selection)
        {
            var explorerNewSelectorElement = (item as TreeViewItem<VisualElement>).data as BuilderExplorerNewSelectorElement;
            var styleSheet = explorerNewSelectorElement.styleSheet;

            // Instantiate explorer item template.
            explorerNewSelectorElement.explorerItemTemplate.CloneTree(parent.contentContainer);

            // Init State mask field.
            var stateMaskField = parent.Q<MaskField>("state-mask-field");
            stateMaskField.choices = NewSelectorPseudoStatesNames;
            stateMaskField.m_Choices[0] = "State"; // Instead of "Nothing"
            stateMaskField.m_Choices[1] = "All"; // Instead of "Everything"
            stateMaskField.value = 1; // This changes the value so the MaskField.text updates with the above custom values.
            stateMaskField.value = 0;
            HideShowStateMaskField(stateMaskField, explorerNewSelectorElement.newSelectorMode);

            // Init mode field.
            var modeField = parent.Q<EnumField>("mode-enum-field");
            modeField.RegisterValueChangedCallback((evt) =>
            {
                var newValue = (NewSelectorMode)evt.newValue;
                explorerNewSelectorElement.newSelectorMode = newValue;
                HideShowStateMaskField(stateMaskField, newValue);
            });
            modeField.Init(explorerNewSelectorElement.newSelectorMode);

            // Init text field.
            var textField = parent.Q<TextField>("new-selector-field");
            textField.SetValueWithoutNotify(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage);
            textField.userData = explorerNewSelectorElement;
            textField.Q("unity-text-input").RegisterCallback<KeyDownEvent>((evt) =>
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                    return;

                var input = evt.target as VisualElement;
                var field = input.parent as TextField;
                var newValue = field.text;

                var inExplorerAddElement = field.userData as VisualElement;
                inExplorerAddElement.AddToClassList(BuilderConstants.PaneContentPleaseRefocusElementClassName);

                var newSelectorStr = string.Empty;
                if ((NewSelectorMode)modeField.value == NewSelectorMode.Class)
                {
                    newSelectorStr = "." + newValue;

                    var state = (NewSelectorPseudoStates)stateMaskField.value;
                    if (state.HasFlag(NewSelectorPseudoStates.Hover))
                        newSelectorStr += ":hover";
                    if (state.HasFlag(NewSelectorPseudoStates.Active))
                        newSelectorStr += ":active";
                    if (state.HasFlag(NewSelectorPseudoStates.Focus))
                        newSelectorStr += ":focus";
                }
                else // Complex
                {
                    newSelectorStr = newValue;
                }

                var selectorContainerElement = explorerNewSelectorElement.parent;
                BuilderSharedStyles.CreateNewSelector(selectorContainerElement, styleSheet, newSelectorStr);

                selection.NotifyOfHierarchyChange();
                selection.NotifyOfStylingChange();

                evt.PreventDefault();
                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
            textField.RegisterCallback<FocusEvent>((evt) =>
            {
                var field = evt.target as TextField;
                if (field.text == BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage)
                    field.SetValueWithoutNotify(string.Empty);
            });
            textField.RegisterCallback<BlurEvent>((evt) =>
            {
                var field = evt.target as TextField;
                if (string.IsNullOrEmpty(field.text))
                    field.SetValueWithoutNotify(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage);
            });

            // If we were re-created but were previously focused, ask the Pane to refocus the TextField.
            if (explorerNewSelectorElement.ClassListContains(BuilderConstants.PaneContentPleaseRefocusElementClassName))
            {
                textField.AddToClassList(BuilderConstants.PaneContentPleaseRefocusElementClassName);
                explorerNewSelectorElement.RemoveFromClassList(BuilderConstants.PaneContentPleaseRefocusElementClassName);
            }
        }
    }
}