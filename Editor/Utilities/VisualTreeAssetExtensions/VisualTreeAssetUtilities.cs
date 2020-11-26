using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class VisualTreeAssetUtilities
    {
        public static VisualTreeAsset CreateInstance()
        {
            var vta = ScriptableObject.CreateInstance<VisualTreeAsset>();
            vta.visualElementAssets = new List<VisualElementAsset>();
            vta.templateAssets = new List<TemplateAsset>();

            vta.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;

#if !UNITY_2019_4
            var uxmlTagElement = new VisualElementAsset(BuilderConstants.UxmlTagTypeName);
            InitializeElement(uxmlTagElement);
            uxmlTagElement.id = GenerateNewId(vta, uxmlTagElement);
            vta.visualElementAssets.Add(uxmlTagElement);
#endif

            return vta;
        }

        public static int CompareForOrder(VisualElementAsset a, VisualElementAsset b) => a.orderInDocument.CompareTo(b.orderInDocument);

        public static Dictionary<int, List<VisualElementAsset>> GenerateIdToChildren(VisualTreeAsset vta)
        {
            Dictionary<int, List<VisualElementAsset>> idToChildren = new Dictionary<int, List<VisualElementAsset>>();
            int eltcount = vta.visualElementAssets == null ? 0 : vta.visualElementAssets.Count;
            int tplcount = vta.templateAssets == null ? 0 : vta.templateAssets.Count;
            for (int i = 0; i < eltcount + tplcount; i++)
            {
                VisualElementAsset asset = i < eltcount ? vta.visualElementAssets[i] : vta.templateAssets[i - eltcount];
                List<VisualElementAsset> children;
                if (!idToChildren.TryGetValue(asset.parentId, out children))
                {
                    children = new List<VisualElementAsset>();
                    idToChildren.Add(asset.parentId, children);
                }

                children.Add(asset);
            }

            return idToChildren;
        }

        public static void InitializeElement(VisualElementAsset vea)
        {
            vea.classes = new string[0];
            vea.orderInDocument = -1;
            vea.ruleIndex = -1;
        }

        public static void ReOrderDocument(VisualTreeAsset vta)
        {
            var idToChildren = GenerateIdToChildren(vta);

            List<VisualElementAsset> rootAssets;
            if (!idToChildren.TryGetValue(0, out rootAssets) || rootAssets == null)
                return;

            rootAssets.Sort(CompareForOrder);

            int orderInDocument = 0;
            foreach (var rootElement in rootAssets)
            {
                Assert.IsNotNull(rootElement);

                rootElement.orderInDocument = orderInDocument;
                orderInDocument += BuilderConstants.VisualTreeAssetOrderIncrement;

                ReOrderDocumentRecursive(vta, rootElement, idToChildren);
            }
        }

        static void ReOrderDocumentRecursive(
            VisualTreeAsset vta,
            VisualElementAsset rootElement,
            Dictionary<int, List<VisualElementAsset>> idToChildren)
        {
            List<VisualElementAsset> children;
            if (idToChildren.TryGetValue(rootElement.id, out children))
            {
                children.Sort(CompareForOrder);

                int orderInDocument = 0;
                foreach (var child in children)
                {
                    child.orderInDocument = orderInDocument;
                    orderInDocument += BuilderConstants.VisualTreeAssetOrderIncrement;

                    ReOrderDocumentRecursive(vta, child, idToChildren);
                }
            }
        }

        static void AddElementToDocumentArrays(VisualTreeAsset vta, VisualElementAsset vea)
        {
            if (vea is TemplateAsset)
                vta.templateAssets.Add(vea as TemplateAsset);
            else
                vta.visualElementAssets.Add(vea);
        }

        static void SetInitElementWithParent(
            VisualTreeAsset vta, VisualElementAsset vea, VisualElementAsset parent,
            Dictionary<int, List<VisualElementAsset>> idToChildren,
            int orderIndex = -1)
        {
            var childCount = 0;
            var parentId = 0;
            if (parent == null)
            {
                vea.parentId = 0;

                // Just has to be the highest number there is. It will be reset to a proper
                // number when we reorder below.
                childCount = vta.visualElementAssets.Count + vta.templateAssets.Count;
            }
            else
            {
                parentId = parent.id;
                vea.parentId = parent.id;
            }

            List<VisualElementAsset> children;
            if (idToChildren.TryGetValue(parentId, out children))
                childCount = children.Count;

            if (orderIndex < 0)
            {
                // Just put it at the end of all siblings.
                vea.orderInDocument = (childCount + 1) * BuilderConstants.VisualTreeAssetOrderIncrement;
            }
            else
            {
                var newOrderIndex = orderIndex * BuilderConstants.VisualTreeAssetOrderIncrement;

                if (children != null && children.Contains(vea) && newOrderIndex > vea.orderInDocument)
                    newOrderIndex = newOrderIndex + BuilderConstants.VisualTreeAssetOrderHalfIncrement;
                else
                    newOrderIndex = newOrderIndex - BuilderConstants.VisualTreeAssetOrderHalfIncrement;

                vea.orderInDocument = newOrderIndex;
            }

            // Only init id the first time as needed.
            if (vea.id == 0)
                vea.id = GenerateNewId(vta, vea);
        }

        public static int GenerateNewId(VisualTreeAsset vta, VisualElementAsset vea)
        {
            int parentHash;
            if (!vea.HasParent())
                parentHash = vta.GetHashCode();
            else
                parentHash = vea.parentId;

            var guid = System.Guid.NewGuid().GetHashCode();

            return (vta.GetNextChildSerialNumber() + 585386304) * -1521134295 + parentHash + guid;
        }

        public static VisualElementAsset AddElementToDocument(
            VisualTreeAsset vta, VisualElementAsset vea, VisualElementAsset parent)
        {
            var idToChildren = GenerateIdToChildren(vta);

            SetInitElementWithParent(vta, vea, parent, idToChildren);

            AddElementToDocumentArrays(vta, vea);

            // Fix document ordering.
            ReOrderDocument(vta);

            return vea;
        }

        public static VisualElementAsset ReparentElementInDocument(
            VisualTreeAsset vta, VisualElementAsset vea,
            VisualElementAsset newParent, int index = -1)
        {
            var idToChildren = GenerateIdToChildren(vta);

            // HACK: We clear ALL stylesheets here if element is no longer at root.
            // this is fine as long as we only support one uss but when we support more
            // we need to make sure we only remove the stylesheets that make sense to remove.
            // See: https://unity3d.atlassian.net/browse/UIT-469
            if (vta.IsRootElement(vea) && newParent != null)
            {
                vea.stylesheetPaths.Clear();
                vea.stylesheets.Clear();
            }

            SetInitElementWithParent(vta, vea, newParent, idToChildren, index);

            // Fix document ordering.
            ReOrderDocument(vta);

            return vea;
        }
    }
}
