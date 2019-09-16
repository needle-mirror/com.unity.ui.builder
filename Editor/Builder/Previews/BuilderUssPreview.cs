using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Unity.UI.Builder
{
    internal class BuilderUssPreview : BuilderCodePreview, IBuilderSelectionNotifier
    {
        Builder m_Builder;

        public BuilderUssPreview(Builder builder)
        {
            m_Builder = builder;
            RefreshUSS();
        }

        public void RefreshUSS()
        {
            var uss = string.Empty;
            if (m_Builder.document != null && m_Builder.document.mainStyleSheet != null)
                uss = m_Builder.document.mainStyleSheet.GenerateUSS();

            SetText(uss);
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            RefreshUSS();
        }

        public void SelectionChanged()
        {
            RefreshUSS();
        }

        public void StylingChanged(List<string> styles)
        {
            RefreshUSS();
        }
    }
}