using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    internal class BuilderIntegrationTest
    {
        protected Builder BuilderWindow { get; private set; }
        protected BuilderLibrary LibraryPane { get; private set; }
        protected BuilderHierarchy HierarchyPane { get; private set; }
        protected BuilderStyleSheets StyleSheetsPane { get; private set; }
        protected BuilderViewport ViewportPane { get; private set; }
        protected BuilderInspector InspectorPane { get; private set; }

        [SetUp]
        public void Setup()
        {
            BuilderWindow = BuilderTestsHelper.MakeNewBuilderWindow();
            LibraryPane = BuilderWindow.rootVisualElement.Q<BuilderLibrary>();
            HierarchyPane = BuilderWindow.rootVisualElement.Q<BuilderHierarchy>();
            StyleSheetsPane = BuilderWindow.rootVisualElement.Q<BuilderStyleSheets>();
            ViewportPane = BuilderWindow.rootVisualElement.Q<BuilderViewport>();
            InspectorPane = BuilderWindow.rootVisualElement.Q<BuilderInspector>();

            ForceNewDocument();
            var createSelectorField = StyleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Blur();
        }

        [TearDown]
        public void TearDown()
        {
            ForceNewDocument();
            MouseCaptureController.ReleaseMouse();
            BuilderWindow.Close();
        }

        protected void ForceNewDocument()
        {
            BuilderWindow.rootVisualElement.Q<BuilderToolbar>().ForceNewDocument();
        }

        protected IEnumerator AddVisualElement()
        {
            var label = BuilderTestsHelper.GetLabelWithName(LibraryPane, nameof(VisualElement));
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                label.worldBound.center,
                HierarchyPane.worldBound.center);

            yield return UIETestHelpers.Pause(1);
        }

        protected IEnumerator AddTextFieldElement()
        {
            var label = BuilderTestsHelper.GetLabelWithName(LibraryPane, "Text Field");
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                label.worldBound.center,
                HierarchyPane.worldBound.center);

            yield return UIETestHelpers.Pause(1);
        }

        protected IEnumerator AddSelector(string selectorName)
        {
            var builderWindow = BuilderWindow;
            var inputField = StyleSheetsPane.Q<TextField>();
            inputField.visualInput.Focus();

            // Make
            yield return UIETestEvents.KeyBoard.SimulateTyping(builderWindow, selectorName);
            UIETestEvents.KeyBoard.SimulateKeyDown(builderWindow, KeyCode.Return);
        }

        internal BuilderExplorerItem GetStyleSelectorNodeWithName(string selectorName)
        {
            return BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, selectorName);
        }

        internal BuilderExplorerItem GetFirstExplorerVisualElementNode(string nodeName)
        {
            return BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nodeName);
        }

        internal VisualElement GetFirstViewportElement()
        {
            return ViewportPane.documentElement[0];
        }

        internal BuilderExplorerItem GetFirstExplorerItem()
        {
            var firstDocumentElement = ViewportPane.documentElement[0];
            return BuilderTestsHelper.GetLinkedExplorerItem(firstDocumentElement);
        }
    }
}