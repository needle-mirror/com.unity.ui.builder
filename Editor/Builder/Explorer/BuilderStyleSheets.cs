using System;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheets : BuilderExplorer, IBuilderSelectionNotifier
    {
        static readonly string kToolbarPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderStyleSheetsNewSelectorControls.uxml";
        static readonly string kHelpTooltipPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderStyleSheetsNewSelectorHelpTips.uxml";

        TextField m_NewSelectorTextField;
        VisualElement m_NewSelectorTextInputField;
        ToolbarMenu m_PseudoStatesMenu;
        ToolbarButton m_NewSelectorAddButton;
        BuilderTooltipPreview m_TooltipPreview;

        bool m_FieldFocusedFromStandby;
        bool m_ShouldRefocusSelectorFieldOnBlur;

        static readonly List<string> kNewSelectorPseudoStatesNames = new List<string>()
        {
            ":hover", ":active", ":selected", ":checked", ":focus"
        };

        public BuilderStyleSheets(
            BuilderViewport viewport,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderHierarchyDragger hierarchyDragger,
            BuilderElementContextMenu contextMenuManipulator,
            HighlightOverlayPainter highlightOverlayPainter,
            BuilderTooltipPreview tooltipPreview)
            : base(
                  viewport,
                  selection,
                  classDragger,
                  hierarchyDragger,
                  contextMenuManipulator,
                  viewport.styleSelectorElementContainer,
                  highlightOverlayPainter,
                  kToolbarPath)
        {
            m_TooltipPreview = tooltipPreview;
            if (m_TooltipPreview != null)
            {
                var helpTooltipTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(kHelpTooltipPath);
                var helpTooltipContainer = helpTooltipTemplate.CloneTree();
                m_TooltipPreview.Add(helpTooltipContainer); // We are the only ones using it so just add the contents and be done.
            }

            viewDataKey = "builder-style-sheets";
            AddToClassList(BuilderConstants.ExplorerStyleSheetsPaneClassName);

            var parent = this.Q("new-selector-item");

            // Init text field.
            m_NewSelectorTextField = parent.Q<TextField>("new-selector-field");
            m_NewSelectorTextField.SetValueWithoutNotify(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage);
            m_NewSelectorTextInputField = m_NewSelectorTextField.Q("unity-text-input");
            m_NewSelectorTextInputField.RegisterCallback<KeyDownEvent>(OnEnter, TrickleDown.TrickleDown);

            m_NewSelectorTextInputField.RegisterCallback<FocusEvent>((evt) =>
            {
                var input = evt.target as VisualElement;
                var field = input.parent as TextField;
                m_FieldFocusedFromStandby = true;
                if (field.text == BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage || m_ShouldRefocusSelectorFieldOnBlur)
                {
                    m_ShouldRefocusSelectorFieldOnBlur = false;
                    field.value = BuilderConstants.UssSelectorClassNameSymbol;
                }

                ShowTooltip();
            });

            m_NewSelectorTextField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                var field = evt.target as TextField;

                if (!string.IsNullOrEmpty(evt.newValue) && evt.newValue != BuilderConstants.UssSelectorClassNameSymbol)
                {
                    m_NewSelectorAddButton.SetEnabled(true);
                    m_PseudoStatesMenu.SetEnabled(true);
                }
                else
                {
                    m_NewSelectorAddButton.SetEnabled(false);
                    m_PseudoStatesMenu.SetEnabled(false);
                }

                if (!m_FieldFocusedFromStandby)
                    return;

                m_FieldFocusedFromStandby = false;

                // We don't want the '.' we just inserted in the FocusEvent to be highlighted,
                // which is the default behavior.
                field.SelectRange(1, 1);
            });

            m_NewSelectorTextInputField.RegisterCallback<BlurEvent>((evt) =>
            {
                var input = evt.target as VisualElement;
                var field = input.parent as TextField;
                if (m_ShouldRefocusSelectorFieldOnBlur)
                {
                    field.schedule.Execute(PostEnterRefocus);
                    evt.PreventDefault();
                    evt.StopImmediatePropagation();
                    return;
                }

                if (string.IsNullOrEmpty(field.text) || field.text == BuilderConstants.UssSelectorClassNameSymbol)
                {
                    field.SetValueWithoutNotify(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage);
                    m_NewSelectorAddButton.SetEnabled(false);
                    m_PseudoStatesMenu.SetEnabled(false);
                }

                HideTooltip();
            });

            // Setup new selector button.
            m_NewSelectorAddButton = parent.Q<ToolbarButton>("add-new-selector-button");
            m_NewSelectorAddButton.clickable.clicked += OnAddPress;
            m_NewSelectorAddButton.SetEnabled(false);

            // Setup pseudo states menu.
            m_PseudoStatesMenu = parent.Q<ToolbarMenu>("add-pseudo-state-menu");
            m_PseudoStatesMenu.SetEnabled(false);
            SetUpPseudoStatesMenu();
        }

        protected override bool IsSelectedItemValid(VisualElement element)
        {
            var isCS = element.GetStyleComplexSelector() != null;
            var isSS = element.GetStyleSheet() != null;

            return isCS || isSS;
        }

        void PostEnterRefocus()
        {
            m_NewSelectorTextInputField.Focus();
        }

        void OnEnter(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                return;

            CreateNewSelector();

            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        void OnAddPress()
        {
            CreateNewSelector();

            PostEnterRefocus();
        }

        void CreateNewSelector()
        {
            var newValue = m_NewSelectorTextField.text;
            if (newValue == BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage)
                return;
            
            m_ShouldRefocusSelectorFieldOnBlur = true;
            
            var newSelectorStr = newValue;
            if (newSelectorStr.StartsWith(BuilderConstants.UssSelectorClassNameSymbol))
            {
                newSelectorStr = BuilderConstants.UssSelectorClassNameSymbol + newSelectorStr.TrimStart(BuilderConstants.UssSelectorClassNameSymbol[0]);
            }

            if (string.IsNullOrEmpty(newSelectorStr))
                return;
            
            if(newSelectorStr.Length == 1 && (
                    newSelectorStr.StartsWith(BuilderConstants.UssSelectorClassNameSymbol)
                    || newSelectorStr.StartsWith("-")
                    || newSelectorStr.StartsWith("_")))
                return;
                
            if (!BuilderNameUtilities.StyleSelectorRegex.IsMatch(newSelectorStr))
            {
                Builder.ShowWarning(BuilderConstants.StyleSelectorValidationSpacialCharacters);
                m_NewSelectorTextField.schedule.Execute(() =>
                {
                    m_NewSelectorTextField.SetValueWithoutNotify(newValue);
                    m_NewSelectorTextField.SelectAll();
                });
                return;
            }

            var selectorContainerElement = m_Viewport.styleSelectorElementContainer;
            var styleSheet = selectorContainerElement.GetStyleSheet();
            BuilderSharedStyles.CreateNewSelector(selectorContainerElement, styleSheet, newSelectorStr);

            m_Selection.NotifyOfHierarchyChange();
            m_Selection.NotifyOfStylingChange();
        }

        void SetUpPseudoStatesMenu()
        {
            foreach (var state in kNewSelectorPseudoStatesNames)
                m_PseudoStatesMenu.menu.AppendAction(state, a =>
                {
                    m_NewSelectorTextField.value += a.name;
                });
        }

        void ShowTooltip()
        {
            if (m_TooltipPreview == null)
                return;

            if (m_TooltipPreview.isShowing)
                return;

            m_TooltipPreview.Show();

            m_TooltipPreview.style.left = this.pane.resolvedStyle.width + BuilderConstants.TooltipPreviewYOffset;
            m_TooltipPreview.style.top = m_Viewport.viewportWrapper.worldBound.y;
        }

        void HideTooltip()
        {
            if (m_TooltipPreview == null)
                return;

            m_TooltipPreview.Hide();
        }
    }
}
