using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderNewSelectorField : VisualElement
    {
        static readonly List<string> kNewSelectorPseudoStatesNames = new List<string>()
        {
            ":hover", ":active", ":selected", ":checked", ":focus"
        };

        static readonly string s_UssPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderNewSelectorField.uss";
        static readonly string s_UxmlPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderNewSelectorField.uxml";

#if UNITY_2019_2
        static readonly string s_UssPath2019_2 = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderNewSelectorField2019_2.uss";
        static readonly string s_UssPathDark2019_2 = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderNewSelectorFieldDark2019_2.uss";
        static readonly string s_UssPathLight2019_2 = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderNewSelectorFieldLight2019_2.uss";
#endif

        static readonly string s_UssClassName = "unity-new-selector-field";
        static readonly string s_OptionsPopupUssClassName = "unity-new-selector-field__options-popup";
        static readonly string s_TextFieldName = "unity-text-field";
        static readonly string s_OptionsPopupContainerName = "unity-options-popup-container";
        internal static readonly string s_TextFieldUssClassName = "unity-new-selector-field__text-field";

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public new class UxmlFactory : UxmlFactory<BuilderNewSelectorField, UxmlTraits> { }

        TextField m_TextField;
        ToolbarMenu m_OptionsPopup;

        public TextField textField => m_TextField;

        public ToolbarMenu pseudoStatesMenu => m_OptionsPopup;

        public BuilderNewSelectorField()
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(s_UssPath));

#if UNITY_2019_2
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(s_UssPath2019_2));
            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(s_UssPathDark2019_2));
            else
                styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(s_UssPathLight2019_2));
#endif

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
            template.CloneTree(this);

            m_TextField = this.Q<TextField>(s_TextFieldName);

            var popupContainer = this.Q(s_OptionsPopupContainerName);
            m_OptionsPopup = new ToolbarMenu();
            m_OptionsPopup.AddToClassList(s_OptionsPopupUssClassName);
            popupContainer.Add(m_OptionsPopup);

            SetUpPseudoStatesMenu();
            m_OptionsPopup.text = ":";
            m_OptionsPopup.SetEnabled(false);

            m_TextField.RegisterValueChangedCallback<string>(OnTextFieldValueChange);
        }

        protected void OnTextFieldValueChange(ChangeEvent<string> evt)
        {
            if (!string.IsNullOrEmpty(evt.newValue) && evt.newValue != BuilderConstants.UssSelectorClassNameSymbol)
            {
                m_OptionsPopup.SetEnabled(true);
            }
            else
            {
                m_OptionsPopup.SetEnabled(false);
            }
        }

        void SetUpPseudoStatesMenu()
        {
            foreach (var state in kNewSelectorPseudoStatesNames)
                m_OptionsPopup.menu.AppendAction(state, a =>
                {
                    textField.value += a.name;
                });
        }
    }
}
