using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Unity.UI.Builder
{
    internal class BuilderUxmlPreview : BuilderCodePreview, IBuilderSelectionNotifier
    {
        BuilderPaneWindow m_PaneWindow;

        public BuilderUxmlPreview(BuilderPaneWindow paneWindow)
        {
            m_PaneWindow = paneWindow;

            RefreshUXML();
        }

        public string GenerateUXMLText()
        {
            if (m_PaneWindow == null || m_PaneWindow.document.visualTreeAsset == null)
                return string.Empty;

            bool writingToFile = true; // Set this to false to see the special selection elements and attributes.
            var uxmlText = m_PaneWindow.document.visualTreeAsset.GenerateUXML(m_PaneWindow.document.uxmlPath, writingToFile);
            return uxmlText;
        }

        void RefreshUXML()
        {
            SetText(GenerateUXMLText());
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            RefreshUXML();
        }

        public void SelectionChanged()
        {
            RefreshUXML();
        }

        public void StylingChanged(List<string> styles)
        {
            // Do nothing for now.
        }
    }
}