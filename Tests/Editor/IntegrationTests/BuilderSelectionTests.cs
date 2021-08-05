using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class BuilderSelectionTests : BuilderIntegrationTest
    {
        VisualElement SelectByType(BuilderSelectionType selectionType)
        {
            VisualElement element = null;
            switch (selectionType)
            {
                case BuilderSelectionType.Element:
                    element = builder.documentRootElement.Q(selection.isEmpty ? "multi-uss-label" : "multi-uss-button");
                    break;
                case BuilderSelectionType.ElementInTemplateInstance:
                    element = builder.documentRootElement.Q<TemplateContainer>(selection.isEmpty ? "no-uss-document1" : "no-uss-document2").Q<Label>("no-uss-label");
                    break;
                case BuilderSelectionType.StyleSelector:
                    element = viewport.styleSelectorElementContainer.Q(selection.isEmpty ? "ColorsTestStyleSheet" : "LayoutTestStyleSheet").Children().First();
                    break;
                case BuilderSelectionType.StyleSheet:
                    element = viewport.styleSelectorElementContainer.Q(selection.isEmpty ? "ColorsTestStyleSheet" : "LayoutTestStyleSheet");
                    break;
                case BuilderSelectionType.VisualTreeAsset:
                    element = builder.documentRootElement;
                    break;
            }

            if (element == null)
                return null;

            if (selection.isEmpty || selectionType == BuilderSelectionType.VisualTreeAsset)
                selection.Select(null, element);
            else
                selection.AddToSelection(null, element);
            return element;
        }

        [TestCase(BuilderSelectionType.Element)]
#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
        [TestCase(BuilderSelectionType.ElementInTemplateInstance), Ignore("Missing functionality on 2021.1 and older.")]
#else
        [TestCase(BuilderSelectionType.ElementInTemplateInstance)]
#endif
        [TestCase(BuilderSelectionType.StyleSelector)]
        [TestCase(BuilderSelectionType.StyleSheet)]
        [TestCase(BuilderSelectionType.VisualTreeAsset)]
        public void SelectAndUnselect(BuilderSelectionType selectionType)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            builder.LoadDocument(asset);

            Assert.AreEqual(BuilderSelectionType.Nothing, selection.selectionType);
            Assert.AreEqual(0, selection.selection.Count());

            var selectedElement1 = SelectByType(selectionType);
            Assert.NotNull(selectedElement1);
            Assert.AreEqual(selectionType, selection.selectionType);
            Assert.AreEqual(1, selection.selection.Count());
            Assert.AreEqual(selectedElement1, selection.selection.ElementAt(0));
            Assert.AreEqual(selectedElement1, inspector.currentVisualElement);

            if (selectionType != BuilderSelectionType.VisualTreeAsset)
            {
                var selectedElement2 = SelectByType(selectionType);
                Assert.NotNull(selectedElement2);
                Assert.AreEqual(selectionType, selection.selectionType);
                Assert.AreEqual(2, selection.selection.Count());
                Assert.AreEqual(selectedElement2, selection.selection.ElementAt(1));
                Assert.AreNotEqual(selectedElement2, inspector.currentVisualElement); // Only first in selection is set as currentVisualElement.
            }

            selection.ClearSelection(null);
            Assert.AreEqual(BuilderSelectionType.Nothing, selection.selectionType);
            Assert.AreEqual(0, selection.selection.Count());
        }

        [UnityTest, Ignore("This test was broken in a recent change to the testing framework. See: UIT-1226")]
        public IEnumerator SelectionUndoRedo()
        {
            var selectionType = BuilderSelectionType.Element;

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            builder.LoadDocument(asset);

            Assert.AreEqual(BuilderSelectionType.Nothing, selection.selectionType);
            Assert.AreEqual(0, selection.selection.Count());

            var selectedElement1 = SelectByType(selectionType);
            Assert.NotNull(selectedElement1);
            Assert.AreEqual(selectionType, selection.selectionType);
            Assert.AreEqual(1, selection.selection.Count());
            Assert.AreEqual(selectedElement1, selection.selection.ElementAt(0));
            Assert.AreEqual(selectedElement1, inspector.currentVisualElement);

            var selectedElement2 = SelectByType(selectionType);
            Assert.NotNull(selectedElement2);
            Assert.AreEqual(selectionType, selection.selectionType);
            Assert.AreEqual(2, selection.selection.Count());
            Assert.AreEqual(selectedElement2, selection.selection.ElementAt(1));
            Assert.AreNotEqual(selectedElement2, inspector.currentVisualElement); // Only first in selection is set as currentVisualElement.

            yield return null;
            Undo.PerformUndo();
            yield return null;

            Assert.AreEqual(selectionType, selection.selectionType);
            Assert.AreEqual(1, selection.selection.Count());

            yield return null;
            Undo.PerformUndo();
            yield return null;

            Assert.AreEqual(BuilderSelectionType.Nothing, selection.selectionType);
            Assert.AreEqual(0, selection.selection.Count());

            yield return null;
            Undo.PerformUndo();
            yield return null;

            Assert.AreEqual(selectionType, selection.selectionType);
            Assert.AreEqual(1, selection.selection.Count());
        }

        [UnityTest]
        public IEnumerator SelectionSurvivesPlaymode()
        {
            var selectionType = BuilderSelectionType.Element;

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            builder.LoadDocument(asset);

            Assert.AreEqual(BuilderSelectionType.Nothing, selection.selectionType);
            Assert.AreEqual(0, selection.selection.Count());

            var selectedElement1 = SelectByType(selectionType);
            Assert.NotNull(selectedElement1);
            Assert.AreEqual(selectionType, selection.selectionType);
            Assert.AreEqual(1, selection.selection.Count());
            Assert.AreEqual(selectedElement1, selection.selection.ElementAt(0));
            Assert.AreEqual(selectedElement1, inspector.currentVisualElement);

            var selectedElement2 = SelectByType(selectionType);
            Assert.NotNull(selectedElement2);
            Assert.AreEqual(selectionType, selection.selectionType);
            Assert.AreEqual(2, selection.selection.Count());
            Assert.AreEqual(selectedElement2, selection.selection.ElementAt(1));
            Assert.AreNotEqual(selectedElement2, inspector.currentVisualElement); // Only first in selection is set as currentVisualElement.

            yield return new EnterPlayMode();

            Assert.AreEqual(2, selection.selection.Count());

            yield return new ExitPlayMode();

            Assert.AreEqual(2, selection.selection.Count());

            yield return null;
        }
    }
}
