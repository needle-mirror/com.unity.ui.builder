using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Unity.UI.Builder
{
    internal partial class BuilderInspector
    {
        private PersistedFoldout m_LocalStylesSection;

        private Dictionary<string, List<VisualElement>> m_StyleFields;

        private VisualElement InitLocalStylesSection()
        {
            m_LocalStylesSection = this.Q<PersistedFoldout>("inspector-local-styles-foldout");

            m_StyleFields = new Dictionary<string, List<VisualElement>>();

            var styleRows = m_LocalStylesSection.Query<BuilderStyleRow>().ToList();

            foreach (var styleRow in styleRows)
            {
                var bindingPath = styleRow.bindingPath;
                var styleFields = styleRow.Query<BindableElement>().ToList();

                if (styleRow.ClassListContains("unity-builder-double-field-row"))
                {
                    BindDoubleFieldRow(styleRow);
                }

                foreach (var styleField in styleFields)
                {
                    // Avoid fields within fields.
                    if (styleField.parent != styleRow)
                        continue;

                    if (styleField is PersistedFoldoutWithField)
                    {
                        BindStyleField(styleRow, styleField as PersistedFoldoutWithField);
                    }
                    else if (!string.IsNullOrEmpty(styleField.bindingPath))
                    {
                        BindStyleField(styleRow, styleField.bindingPath, styleField);
                    }
                    else
                    {
                        BuilderStyleRow.ReAssignTooltipToChild(styleField);
                        BindStyleField(styleRow, bindingPath, styleField);
                    }
                }
            }

            return m_LocalStylesSection;
        }

        private void RefreshLocalStylesOverridesSection()
        {
            var styleRows = m_LocalStylesSection.Query<BuilderStyleRow>().ToList();

            foreach (var styleRow in styleRows)
            {
                var bindingPath = styleRow.bindingPath;
                var styleFields = styleRow.Query<BindableElement>().ToList();

                foreach (var styleField in styleFields)
                {
                    // Avoid fields within fields.
                    if (styleField.parent != styleRow)
                        continue;

                    if (styleField is PersistedFoldoutWithField)
                    {
                        RefreshStyleField(styleField as PersistedFoldoutWithField);
                    }
                    else if (!string.IsNullOrEmpty(styleField.bindingPath))
                    {
                        RefreshStyleField(styleField.bindingPath, styleField);
                    }
                    else
                    {
                        RefreshStyleField(bindingPath, styleField);
                    }
                }
            }
        }
    }
}
