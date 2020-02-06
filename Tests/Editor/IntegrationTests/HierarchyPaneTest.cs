using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    internal class HierarchyPaneTest : BuilderIntegrationTest
    {
        [UnityTest]
        public IEnumerator CreateEmptyVisualElement()
        {
            yield return AddVisualElement();
            var hierarchyCreatedItem = GetFirstExplorerVisualElementNode(nameof(VisualElement));
            Assert.That(hierarchyCreatedItem, Is.Not.Null);
        }
    }
}