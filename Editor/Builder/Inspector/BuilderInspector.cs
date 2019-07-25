using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal partial class BuilderInspector : BuilderPaneContent, IBuilderSelectionNotifier
    {
        private enum Section
        {
            NothingSelected = 1 << 0,
            StyleSheet = 1 << 1,
            StyleSelector = 1 << 2,
            ElementAttributes = 1 << 3,
            ElementSharedStyles = 1 << 4,
            LocalStyleOverrides = 1 << 5,
            ElementInTemplateInstance = 1 << 6,
            VisualTreeAsset = 1 << 7
        }

        // Constants
        private static readonly string s_UssClassName = "unity-builder-inspector";
        private static readonly string s_LocalStyleOverrideClassName = "unity-builder-inspector__style--override";
        private static readonly string s_LocalStyleResetClassName = "unity-builder-inspector__style--reset"; // used to reset font style of children
        private static readonly string s_EmptyFoldoutLabelClassName = "unity-builder-inspector__empty-foldout-label";

        // External References
        private Builder m_Builder;
        private BuilderSelection m_Selection;

        // Current Selection
        private StyleRule m_CurrentRule;
        private VisualElement m_CurrentVisualElement;

        // Sections List (for hiding/showing based on current selection)
        private List<VisualElement> m_Sections;

        // HACK! REMOVE!
#if UNITY_2019_3_OR_NEWER
        private VisualElement m_SectionContainer;
#endif

        // Minor Sections
        private Label m_NothingSelectedSection;
        private Label m_ElementInTemplateInstanceSection;
        private Label m_VisualTreeAssetSection;

        private StyleSheet styleSheet
        {
            get
            {
                if (currentVisualElement == null)
                    return null;

                if (BuilderSharedStyles.IsSelectorElement(currentVisualElement) ||
                    BuilderSharedStyles.IsSelectorsContainerElement(currentVisualElement))
                    return m_Builder.document.mainStyleSheet;

                return visualTreeAsset.inlineSheet;
            }
        }

        private VisualTreeAsset visualTreeAsset
        {
            get { return m_Builder.document.visualTreeAsset; }
        }

        private StyleRule currentRule
        {
            get
            {
                if (m_CurrentRule != null)
                    return m_CurrentRule;

                if (currentVisualElement == null)
                    return null;

                if (BuilderSharedStyles.IsSelectorElement(currentVisualElement))
                {
                    var complexSelectorStr = BuilderSharedStyles.GetSelectorString(currentVisualElement);
                    var complexSelector = styleSheet.FindSelector(complexSelectorStr);
                    m_CurrentRule = complexSelector?.rule;
                }
                else if (currentVisualElement.GetVisualElementAsset() != null)
                {
                    var vea = currentVisualElement.GetVisualElementAsset();
                    m_CurrentRule = visualTreeAsset.GetOrCreateInlineStyleRule(vea);
                }
                else
                {
                    return null;
                }

                return m_CurrentRule;
            }
            set
            {
                m_CurrentRule = value;
            }
        }

        private VisualElement currentVisualElement
        {
            get { return m_CurrentVisualElement; }
            set { m_CurrentVisualElement = value; }
        }

        public BuilderInspector(Builder builder, BuilderSelection selection)
        {
            // Init External References
            m_Selection = selection;
            m_Builder = builder;

            // Load Template
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Builder/Inspector/BuilderInspector.uxml");
            template.CloneTree(this);

            // Load styles.
            AddToClassList(s_UssClassName);
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.InspectorUssPathNoExt + ".uss"));
            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.InspectorUssPathNoExt + "Dark.uss"));
            else
                styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.InspectorUssPathNoExt + "Light.uss"));

            // HACK! REMOVE!
#if UNITY_2019_3_OR_NEWER
            m_SectionContainer = this.Q<ScrollView>();
#endif

            // Matching Selectors
            InitMatchingSelectors();

            // Sections
            m_Sections = new List<VisualElement>();

            // Nothing Selected Section
            m_NothingSelectedSection = this.Q<Label>("nothing-selected-label");
            m_Sections.Add(m_NothingSelectedSection);

            // Element In Template Instance Section
            m_ElementInTemplateInstanceSection = this.Q<Label>("template-instance-label");
            m_Sections.Add(m_ElementInTemplateInstanceSection);

            // Visual Tree Asset Section
            m_VisualTreeAssetSection = this.Q<Label>("uxml-document-label");
            m_Sections.Add(m_VisualTreeAssetSection);

            // StyleSheet Section
            var styleSheetSection = InitStyleSheetSection();
            m_Sections.Add(styleSheetSection);

            // Style Selector Section
            var selectorSection = InitSelectorSection();
            m_Sections.Add(selectorSection);

            // Attributes Section
            var attributesSection = InitAttributesSection();
            m_Sections.Add(attributesSection);

            // Shared Styles Section
            var sharedStylesSection = InitSharedStyleSection();
            m_Sections.Add(sharedStylesSection);

            // Local Style Overrides Section
            var localStyleOverridesSection = InitLocalStyleOverridesSection();
            m_Sections.Add(localStyleOverridesSection);

            // This will take into account the current selection and then call RefreshUI().
            SelectionChanged();

            // Forward focus to the panel header.
            this.Query().Where(e => e.focusable).ForEach((e) => AddFocusable(e));
        }

        private void RefreshAfterFirstInit(GeometryChangedEvent evt)
        {
            currentVisualElement.UnregisterCallback<GeometryChangedEvent>(RefreshAfterFirstInit);
            RefreshUI();
        }

        private void ResetSection(VisualElement section)
        {
            // HACK! REMOVE!
#if UNITY_2019_3_OR_NEWER
            section.RemoveFromHierarchy();
#else
            section.AddToClassList(BuilderConstants.HiddenStyleClassName);
#endif
        }

        private void EnableSection(VisualElement section)
        {
            // HACK! REMOVE!
#if UNITY_2019_3_OR_NEWER
            m_SectionContainer.Add(section);
#else
            section.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
#endif
        }

        private void EnableSections(Section section)
        {
            if (section.HasFlag(Section.NothingSelected))
                EnableSection(m_NothingSelectedSection);
            if (section.HasFlag(Section.StyleSheet))
                EnableSection(m_StyleSheetSection);
            if (section.HasFlag(Section.StyleSelector))
                EnableSection(m_StyleSelectorSection);
            if (section.HasFlag(Section.ElementAttributes))
                EnableSection(m_AttributesSection);
            if (section.HasFlag(Section.ElementSharedStyles))
                EnableSection(m_SharedStylesSection);
            if (section.HasFlag(Section.LocalStyleOverrides))
                EnableSection(m_LocalStyleOverridesSection);
            if (section.HasFlag(Section.ElementInTemplateInstance))
                EnableSection(m_ElementInTemplateInstanceSection);
            if (section.HasFlag(Section.VisualTreeAsset))
                EnableSection(m_VisualTreeAssetSection);
        }

        private void ResetSections()
        {
            foreach (var section in m_Sections)
                ResetSection(section);
        }

        public void RefreshUI()
        {
            // On the first RefreshUI, if an element is already selected, we need to make sure it
            // has a valid style. If not, we need to delay our UI building until it is properly initialized.
            if (currentVisualElement != null && float.IsNaN(currentVisualElement.layout.width))
            {
                currentVisualElement.RegisterCallback<GeometryChangedEvent>(RefreshAfterFirstInit);
                return;
            }

            // Determine what to show based on selection.
            ResetSections();
            switch (m_Selection.selectionType)
            {
                case BuilderSelectionType.Nothing:
                    EnableSections(Section.NothingSelected);
                    return;
                case BuilderSelectionType.StyleSheet:
                    EnableSections(Section.StyleSheet);
                    return;
                case BuilderSelectionType.StyleSelector:
                    EnableSections(
                        Section.StyleSelector |
                        Section.LocalStyleOverrides);
                    break;
                case BuilderSelectionType.Element:
                    EnableSections(
                        Section.ElementAttributes |
                        Section.ElementSharedStyles |
                        Section.LocalStyleOverrides);
                    break;
                case BuilderSelectionType.ElementInTemplateInstance:
                    EnableSections(Section.ElementInTemplateInstance);
                    return;
                case BuilderSelectionType.VisualTreeAsset:
                    EnableSections(Section.VisualTreeAsset);
                    return;
            }

            // Bind the style selector controls.
            if (m_Selection.selectionType == BuilderSelectionType.StyleSelector)
                m_StyleSelectorNameField.SetValueWithoutNotify(BuilderSharedStyles.GetSelectorString(currentVisualElement));

            // Recreate Attribute Fields
            RefreshAttributesSection();

            // Reset current style rule.
            currentRule = null;

            // Get all shared style selectors and draw their fields.
            GetElementMatchers();
            RefreshClassListContainer();
            RefreshMatchingSelectorsContainer();

            // Create the fields for the overridable styles.
            RefreshLocalStylesOverridesSection();
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            // Do nothing.
        }

        public void SelectionChanged()
        {
            currentVisualElement = null;

            foreach (var element in m_Selection.selection)
            {
                if (currentVisualElement != null) // We only support editing one element. Disable for for multiple elements.
                {
                    currentVisualElement = null;
                    break;
                }

                currentVisualElement = element;
            }

            RefreshUI();
        }

        public void StylingChanged(List<string> styles)
        {
            if (styles != null)
            {
                foreach (var styleName in styles)
                {
                    List<VisualElement> fieldList = null;
                    m_StyleFields.TryGetValue(styleName, out fieldList);
                    if (fieldList == null)
                        continue;

                    foreach (var field in fieldList)
                        RefreshStyleField(styleName, field);
                }
            }
            else
            {
                RefreshUI();
            }
        }
    }
}
