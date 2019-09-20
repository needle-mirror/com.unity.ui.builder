using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorLocalStyles : IBuilderInspectorSection
    {
        BuilderInspector m_Inspector;
        BuilderInspectorStyleFields m_StyleFields;

        PersistedFoldout m_LocalStylesSection;

        List<PersistedFoldout> m_StyleCategories;

        public VisualElement root => m_LocalStylesSection;

        public BuilderInspectorLocalStyles(BuilderInspector inspector, BuilderInspectorStyleFields styleFields)
        {
            m_Inspector = inspector;
            m_StyleFields = styleFields;

            m_StyleFields.updateFlexColumnGlobalState = UpdateFlexColumnGlobalState;
            m_StyleFields.updateStyleCategoryFoldoutOverrides = UpdateStyleCategoryFoldoutOverrides;

            m_LocalStylesSection = m_Inspector.Q<PersistedFoldout>("inspector-local-styles-foldout");

            m_StyleCategories = m_LocalStylesSection.Query<PersistedFoldout>(
                className: "unity-builder-inspector__style-category-foldout").ToList();

            var styleRows = m_LocalStylesSection.Query<BuilderStyleRow>().ToList();

            foreach (var styleRow in styleRows)
            {
                var bindingPath = styleRow.bindingPath;
                var currentStyleFields = styleRow.Query<BindableElement>().ToList();

#if UNITY_2019_2
                if (styleRow.ClassListContains(BuilderConstants.Version_2019_3_OrNewer))
                {
                    styleRow.AddToClassList(BuilderConstants.HiddenStyleClassName);
                    continue;
                }
#else
                if (styleRow.ClassListContains(BuilderConstants.Version_2019_2))
                {
                    styleRow.AddToClassList(BuilderConstants.HiddenStyleClassName);
                    continue;
                }
#endif

                if (styleRow.ClassListContains("unity-builder-double-field-row"))
                {
                    m_StyleFields.BindDoubleFieldRow(styleRow);
                }

                foreach (var styleField in currentStyleFields)
                {
                    // Avoid fields within fields.
                    if (styleField.parent != styleRow)
                        continue;

                    if (styleField is FoldoutNumberField)
                    {
                        m_StyleFields.BindStyleField(styleRow, styleField as FoldoutNumberField);
                    }
                    else if (styleField is FoldoutColorField)
                    {
                        m_StyleFields.BindStyleField(styleRow, styleField as FoldoutColorField);
                    }
                    else if (!string.IsNullOrEmpty(styleField.bindingPath))
                    {
                        m_StyleFields.BindStyleField(styleRow, styleField.bindingPath, styleField);
                    }
                    else
                    {
                        BuilderStyleRow.ReAssignTooltipToChild(styleField);
                        m_StyleFields.BindStyleField(styleRow, bindingPath, styleField);
                    }
                }
            }
        }

        public void Enable()
        {
            m_Inspector.Query<BuilderStyleRow>().ForEach(e =>
            {
                e.SetEnabled(true);
            });
        }

        public void Disable()
        {
            m_Inspector.Query<BuilderStyleRow>().ForEach(e =>
            {
                e.SetEnabled(false);
            });
        }

        public void Refresh()
        {
            if (BuilderSharedStyles.IsSelectorElement(m_Inspector.currentVisualElement))
                m_LocalStylesSection.text = BuilderConstants.InspectorLocalStylesSectionTitleForSelector;
            else
                m_LocalStylesSection.text = BuilderConstants.InspectorLocalStylesSectionTitleForElement;

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

                    if (styleField is FoldoutField)
                    {
                        m_StyleFields.RefreshStyleField(styleField as FoldoutField);
                    }
                    else if (!string.IsNullOrEmpty(styleField.bindingPath))
                    {
                        m_StyleFields.RefreshStyleField(styleField.bindingPath, styleField);
                    }
                    else
                    {
                        m_StyleFields.RefreshStyleField(bindingPath, styleField);
                    }
                }
            }

            UpdateStyleCategoryFoldoutOverrides();
        }

        public void UpdateStyleCategoryFoldoutOverrides()
        {
            foreach (var styleCategory in m_StyleCategories)
            {
                if (styleCategory.Q(className: BuilderConstants.InspectorLocalStyleOverrideClassName) == null)
                    styleCategory.RemoveFromClassList(BuilderConstants.InspectorCategoryFoldoutOverrideClassName);
                else
                    styleCategory.AddToClassList(BuilderConstants.InspectorCategoryFoldoutOverrideClassName);
            }
        }

        void UpdateFlexColumnGlobalState(Enum newValue)
        {
            m_LocalStylesSection.RemoveFromClassList(BuilderConstants.InspectorFlexColumnModeClassName);
            m_LocalStylesSection.RemoveFromClassList(BuilderConstants.InspectorFlexColumnReverseModeClassName);
            m_LocalStylesSection.RemoveFromClassList(BuilderConstants.InspectorFlexRowModeClassName);
            m_LocalStylesSection.RemoveFromClassList(BuilderConstants.InspectorFlexRowReverseModeClassName);

            var newDirection = (FlexDirection)newValue;
            switch (newDirection)
            {
                case FlexDirection.Column:
                    m_LocalStylesSection.AddToClassList(BuilderConstants.InspectorFlexColumnModeClassName);
                    break;
                case FlexDirection.ColumnReverse:
                    m_LocalStylesSection.AddToClassList(BuilderConstants.InspectorFlexColumnReverseModeClassName);
                    break;
                case FlexDirection.Row:
                    m_LocalStylesSection.AddToClassList(BuilderConstants.InspectorFlexRowModeClassName);
                    break;
                case FlexDirection.RowReverse:
                    m_LocalStylesSection.AddToClassList(BuilderConstants.InspectorFlexRowReverseModeClassName);
                    break;
            }
        }
    }
}
