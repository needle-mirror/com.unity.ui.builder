using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal partial class BuilderInspector
    {
        private PersistedFoldout m_StyleSelectorSection;
        private TextField m_StyleSelectorNameField;

        private VisualElement InitSelectorSection()
        {
            m_StyleSelectorSection = this.Q<PersistedFoldout>("shared-style-selector-controls");
            m_StyleSelectorNameField = m_StyleSelectorSection.Q<TextField>("rename-selector-field");
            m_StyleSelectorNameField.isDelayed = true;
            m_StyleSelectorNameField.RegisterValueChangedCallback(OnStyleSelectorNameChange);

            return m_StyleSelectorSection;
        }

        private void OnStyleSelectorNameChange(ChangeEvent<string> evt)
        {
            if (m_Selection.selectionType != BuilderSelectionType.StyleSelector)
                return;

            if (evt.newValue == evt.previousValue)
                return;

            Undo.RegisterCompleteObjectUndo(
                styleSheet, BuilderConstants.RenameSelectorUndoMessage);

            BuilderSharedStyles.SetSelectorString(currentVisualElement, styleSheet, evt.newValue);

            m_Selection.NotifyOfHierarchyChange(this);
            m_Selection.NotifyOfStylingChange(this);
        }
    }
}
