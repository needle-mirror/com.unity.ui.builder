using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderExplorerItem : VisualElement
    {
        private VisualElement m_Container;
        private VisualElement m_ReorderZoneAbove;
        private VisualElement m_ReorderZoneBelow;

        public override VisualElement contentContainer => m_Container;

        public VisualElement reorderZoneAbove => m_ReorderZoneAbove;
        public VisualElement reorderZoneBelow => m_ReorderZoneBelow;

        public BuilderExplorerItem()
        {
            // Load Template
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Builder/Explorer/BuilderExplorerItem.uxml");
            template.CloneTree(this);

            m_Container = this.Q("content-container");

            m_ReorderZoneAbove = this.Q("reorder-zone-above");
            m_ReorderZoneBelow = this.Q("reorder-zone-below");

            m_ReorderZoneAbove.userData = this;
            m_ReorderZoneBelow.userData = this;
        }
    }
}