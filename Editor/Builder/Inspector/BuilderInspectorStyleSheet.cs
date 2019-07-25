using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal partial class BuilderInspector
    {
        private VisualElement m_StyleSheetSection;
        private TextField m_NewSelectorNameNameField;
        private Button m_AddNewSelectorButton;

        private VisualElement InitStyleSheetSection()
        {
            m_StyleSheetSection = this.Q("shared-styles-controls");
            m_NewSelectorNameNameField = this.Q<TextField>("add-new-selector-field");
            m_AddNewSelectorButton = this.Q<Button>("add-new-selector-button");

            m_AddNewSelectorButton.clickable.clicked += CreateNewSelector;
            m_NewSelectorNameNameField.RegisterValueChangedCallback(OnCreateNewSelector);
            m_NewSelectorNameNameField.isDelayed = true;

            return m_StyleSheetSection;
        }

        private void OnCreateNewSelector(ChangeEvent<string> evt)
        {
            CreateNewSelector(evt.newValue);
        }

        private void CreateNewSelector()
        {
            if (string.IsNullOrEmpty(m_NewSelectorNameNameField.value))
                return;

            CreateNewSelector(m_NewSelectorNameNameField.value);
        }

        private void CreateNewSelector(string newSelectorString)
        {
            m_NewSelectorNameNameField.SetValueWithoutNotify(string.Empty);

            Undo.RegisterCompleteObjectUndo(
                styleSheet, BuilderConstants.AddNewSelectorUndoMessage);

            BuilderSharedStyles.CreateNewSelector(
                currentVisualElement, styleSheet, newSelectorString);

            m_Selection.NotifyOfHierarchyChange(this);
            m_Selection.NotifyOfStylingChange(this);
        }
    }
}