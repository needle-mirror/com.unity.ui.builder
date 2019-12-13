using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Unity.UI.Builder
{
    internal class BuilderUssPreview : BuilderCodePreview, IBuilderSelectionNotifier
    {
        public BuilderUssPreview(BuilderPaneWindow paneWindow):base(paneWindow)
        {
           
        }

        protected override void OnAttachToPanelDefaultAction()
        {
            base.OnAttachToPanelDefaultAction();
            RefreshUSS();
        }

        void RefreshUSS()
        {
            if (hasDocument)
            {
                SetText(document.mainStyleSheet.GenerateUSS());
                SetTargetAsset(document.mainStyleSheet);
            }
            else
            {
                SetText(string.Empty);
                SetTargetAsset(null);
            }
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

        protected override string previewAssetExtension => BuilderConstants.UssExtension;
    }
}