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
                    element = BuilderWindow.documentRootElement.Q(Selection.isEmpty ? "multi-uss-label" : "multi-uss-button");
                    break;
                case BuilderSelectionType.ElementInTemplateInstance:
                    element = BuilderWindow.documentRootElement.Q<TemplateContainer>(Selection.isEmpty ? "no-uss-document1" : "no-uss-document2").Q<Label>("no-uss-label");
                    break;
                case BuilderSelectionType.StyleSelector:
                    element = ViewportPane.styleSelectorElementContainer.Q(Selection.isEmpty ? "ColorsTestStyleSheet" : "LayoutTestStyleSheet").Children().First();
                    break;
                case BuilderSelectionType.StyleSheet:
                    element = ViewportPane.styleSelectorElementContainer.Q(Selection.isEmpty ? "ColorsTestStyleSheet" : "LayoutTestStyleSheet");
                    break;
                case BuilderSelectionType.VisualTreeAsset:
                    element = BuilderWindow.documentRootElement;
                    break;
            }

            if (element == null)
                return null;

            if (Selection.isEmpty || selectionType == BuilderSelectionType.VisualTreeAsset)
                Selection.Select(null, element);
            else
                Selection.AddToSelection(null, element);
            return element;
        }

        [TestCase(BuilderSelectionType.Element)]
        [TestCase(BuilderSelectionType.ElementInTemplateInstance)]
        [TestCase(BuilderSelectionType.StyleSelector)]
        [TestCase(BuilderSelectionType.StyleSheet)]
        [TestCase(BuilderSelectionType.VisualTreeAsset)]
        public void SelectAndUnselect(BuilderSelectionType selectionType)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            Assert.AreEqual(BuilderSelectionType.Nothing, Selection.selectionType);
            Assert.AreEqual(0, Selection.selection.Count());

            var selectedElement1 = SelectByType(selectionType);
            Assert.NotNull(selectedElement1);
            Assert.AreEqual(selectionType, Selection.selectionType);
            Assert.AreEqual(1, Selection.selection.Count());
            Assert.AreEqual(selectedElement1, Selection.selection.ElementAt(0));
            Assert.AreEqual(selectedElement1, InspectorPane.currentVisualElement);

            if (selectionType != BuilderSelectionType.VisualTreeAsset)
            {
                var selectedElement2 = SelectByType(selectionType);
                Assert.NotNull(selectedElement2);
                Assert.AreEqual(selectionType, Selection.selectionType);
                Assert.AreEqual(2, Selection.selection.Count());
                Assert.AreEqual(selectedElement2, Selection.selection.ElementAt(1));
                Assert.AreNotEqual(selectedElement2, InspectorPane.currentVisualElement); // Only first in selection is set as currentVisualElement.
            }

            Selection.ClearSelection(null);
            Assert.AreEqual(BuilderSelectionType.Nothing, Selection.selectionType);
            Assert.AreEqual(0, Selection.selection.Count());
        }

#if UNITY_2019_2
        [UnityTest, Ignore("Fails on 2019.2 only (but all functionality works when manually doing the same steps). We'll drop 2019.2 support soon anyway.")]
#else
        [UnityTest]
#endif
        public IEnumerable SelectionUndoRedo()
        {
            var selectionType = BuilderSelectionType.Element;

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            Assert.AreEqual(BuilderSelectionType.Nothing, Selection.selectionType);
            Assert.AreEqual(0, Selection.selection.Count());

            var selectedElement1 = SelectByType(selectionType);
            Assert.NotNull(selectedElement1);
            Assert.AreEqual(selectionType, Selection.selectionType);
            Assert.AreEqual(1, Selection.selection.Count());
            Assert.AreEqual(selectedElement1, Selection.selection.ElementAt(0));
            Assert.AreEqual(selectedElement1, InspectorPane.currentVisualElement);

            var selectedElement2 = SelectByType(selectionType);
            Assert.NotNull(selectedElement2);
            Assert.AreEqual(selectionType, Selection.selectionType);
            Assert.AreEqual(2, Selection.selection.Count());
            Assert.AreEqual(selectedElement2, Selection.selection.ElementAt(1));
            Assert.AreNotEqual(selectedElement2, InspectorPane.currentVisualElement); // Only first in selection is set as currentVisualElement.

            yield return null;
            Undo.PerformUndo();
            yield return null;

            Assert.AreEqual(selectionType, Selection.selectionType);
            Assert.AreEqual(1, Selection.selection.Count());

            yield return null;
            Undo.PerformUndo();
            yield return null;

            Assert.AreEqual(BuilderSelectionType.Nothing, Selection.selectionType);
            Assert.AreEqual(0, Selection.selection.Count());

            yield return null;
            Undo.PerformUndo();
            yield return null;

            Assert.AreEqual(selectionType, Selection.selectionType);
            Assert.AreEqual(1, Selection.selection.Count());
        }

        [UnityTest]
        public IEnumerator SelectionSurvivesPlaymode()
        {
            var selectionType = BuilderSelectionType.Element;

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            Assert.AreEqual(BuilderSelectionType.Nothing, Selection.selectionType);
            Assert.AreEqual(0, Selection.selection.Count());

            var selectedElement1 = SelectByType(selectionType);
            Assert.NotNull(selectedElement1);
            Assert.AreEqual(selectionType, Selection.selectionType);
            Assert.AreEqual(1, Selection.selection.Count());
            Assert.AreEqual(selectedElement1, Selection.selection.ElementAt(0));
            Assert.AreEqual(selectedElement1, InspectorPane.currentVisualElement);

            var selectedElement2 = SelectByType(selectionType);
            Assert.NotNull(selectedElement2);
            Assert.AreEqual(selectionType, Selection.selectionType);
            Assert.AreEqual(2, Selection.selection.Count());
            Assert.AreEqual(selectedElement2, Selection.selection.ElementAt(1));
            Assert.AreNotEqual(selectedElement2, InspectorPane.currentVisualElement); // Only first in selection is set as currentVisualElement.

            yield return new EnterPlayMode();

            Assert.AreEqual(2, Selection.selection.Count());

            yield return new ExitPlayMode();

            Assert.AreEqual(2, Selection.selection.Count());

            yield return null;
        }
    }
}
