using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Unity.UI.Builder
{
    internal class BuilderUxmlPreview : BuilderCodePreview, IBuilderSelectionNotifier
    {
        private Builder m_Builder;
        private VisualElement m_Canvas;
        private BuilderSelection m_Selection;

        public BuilderUxmlPreview(Builder builder, BuilderViewport viewport, BuilderSelection selection)
        {
            m_Builder = builder;
            m_Canvas = viewport.documentElement;
            m_Selection = selection;

            RefreshUXML();
        }

        public string GenerateUXMLText()
        {
            if (m_Builder == null || m_Builder.document.visualTreeAsset == null)
                return string.Empty;
            
            var uxmlText = m_Builder.document.visualTreeAsset.GenerateUXML();
            return uxmlText;
        }

        private void RefreshUXML()
        {
            SetText(GenerateUXMLText());
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            RefreshUXML();
        }

        public void SelectionChanged()
        {
            // Do nothing for now.
        }

        public void StylingChanged(List<string> styles)
        {
            // Do nothing for now.
        }
    }
}