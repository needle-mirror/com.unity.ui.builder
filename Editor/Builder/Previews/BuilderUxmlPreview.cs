using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Unity.UI.Builder
{
    internal class BuilderUxmlPreview : BuilderCodePreview, IBuilderSelectionNotifier
    {
        public BuilderUxmlPreview(BuilderPaneWindow paneWindow):base(paneWindow)
        {

        }
        
        protected override void OnAttachToPanelDefaultAction()
        {
            base.OnAttachToPanelDefaultAction();
            RefreshUXML();
        }

        string GenerateUXMLText()
        {
            bool writingToFile = true; // Set this to false to see the special selection elements and attributes.
            var uxmlText = document.visualTreeAsset.GenerateUXML(document.uxmlPath, writingToFile);
            return uxmlText;
        }

        void RefreshUXML()
        {
            if (hasDocument)
            {
                SetText(GenerateUXMLText());
                SetTargetAsset(document.visualTreeAsset);
            }
            else
            {
                SetText(string.Empty);
                SetTargetAsset(null);
            }
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

        protected override string previewAssetExtension => BuilderConstants.UxmlExtension;
    }
}