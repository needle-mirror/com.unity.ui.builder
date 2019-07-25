using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderTooltipPreview : VisualElement
    {
        private static readonly string s_UssClassName = "unity-builder-tooltip-preview";
        private static readonly string s_EnablerClassName = "unity-builder-tooltip-preview__enabler";
        private static readonly string s_ContainerClassName = "unity-builder-tooltip-preview__container";

        private VisualElement m_Enabler;
        private VisualElement m_Container;

        public new class UxmlFactory : UxmlFactory<BuilderTooltipPreview, UxmlTraits> { }

        public new class UxmlTraits : BindableElement.UxmlTraits { }

        public override VisualElement contentContainer => m_Container;

        public BuilderTooltipPreview()
        {
            AddToClassList(s_UssClassName);

            m_Enabler = new VisualElement();
            m_Enabler.name = "enabler";
            m_Enabler.AddToClassList(s_EnablerClassName);
            hierarchy.Add(m_Enabler);

            m_Container = new VisualElement();
            m_Container.name = "content-container";
            m_Container.AddToClassList(s_ContainerClassName);
            m_Enabler.Add(m_Container);
        }

        public void Show()
        {
            m_Enabler.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            m_Enabler.style.display = DisplayStyle.None;
        }

        public void Enable()
        {
            this.style.display = DisplayStyle.Flex;
        }

        public void Disable()
        {
            this.style.display = DisplayStyle.None;
        }
    }
}