using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Unity.UI.Builder
{
    internal class BuilderUssPreview : BuilderCodePreview, IBuilderSelectionNotifier
    {
        BuilderPaneWindow m_PaneWindow;

        public BuilderUssPreview(BuilderPaneWindow paneWindow)
        {
            m_PaneWindow = paneWindow;
            RefreshUSS();
        }

        public void RefreshUSS()
        {
            var uss = string.Empty;
            if (m_PaneWindow.document != null && m_PaneWindow.document.mainStyleSheet != null)
                uss = m_PaneWindow.document.mainStyleSheet.GenerateUSS();

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