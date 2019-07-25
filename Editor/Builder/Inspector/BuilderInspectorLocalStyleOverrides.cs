using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal partial class BuilderInspector
    {
        private PersistedFoldout m_LocalStyleOverridesSection;

        private Dictionary<string, List<VisualElement>> m_StyleFields;

        private VisualElement InitLocalStyleOverridesSection()
        {
            m_LocalStyleOverridesSection = this.Q<PersistedFoldout>("inspector-local-style-overrides-foldout");

            m_StyleFields = new Dictionary<string, List<VisualElement>>();

            var styleRows = m_LocalStyleOverridesSection.Query<BuilderStyleRow>().ToList();

            foreach (var styleRow in styleRows)
            {
                var bindingPath = styleRow.bindingPath;
                var styleFields = styleRow.Query<BindableElement>().ToList();

                foreach (var styleField in styleFields)
                {
                    if (!string.IsNullOrEmpty(styleField.bindingPath))
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

            return m_LocalStyleOverridesSection;
        }

        private void RefreshLocalStylesOverridesSection()
        {
            var styleRows = m_LocalStyleOverridesSection.Query<BuilderStyleRow>().ToList();

            foreach (var styleRow in styleRows)
            {
                var bindingPath = styleRow.bindingPath;
                var styleFields = styleRow.Query<BindableElement>().ToList();

                foreach (var styleField in styleFields)
                { 
                    if (!string.IsNullOrEmpty(styleField.bindingPath))
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
