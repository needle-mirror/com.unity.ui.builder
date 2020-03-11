using System.Collections;
using System.IO;
using System.Net;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    abstract class BuilderIntegrationTest
    {
        protected const string k_TestUXMLFileName = "MyVisualTreeAsset.uxml";
        protected const string k_TestUXMLFilePath = "Assets/" + k_TestUXMLFileName;
        protected static readonly string k_TestUXMLFileContent
            = WebUtility.UrlDecode("%3Cui%3AUXML+xmlns%3Aui%3D%22UnityEngine.UIElements%22+xmlns%3Auie%3D%22UnityEditor.UIElements%22%3E%0D%0A++++%3Cui%3AVisualElement%3E%0D%0A++++++++%3Cui%3AVisualElement+%2F%3E%0D%0A++++%3C%2Fui%3AVisualElement%3E%0D%0A%3C%2Fui%3AUXML%3E%0D%0A");

        protected Builder BuilderWindow { get; private set; }
        protected BuilderLibrary LibraryPane { get; private set; }
        protected BuilderHierarchy HierarchyPane { get; private set; }
        protected BuilderStyleSheets StyleSheetsPane { get; private set; }
        protected BuilderViewport ViewportPane { get; private set; }
        protected BuilderInspector InspectorPane { get; private set; }

        [SetUp]
        public virtual void Setup()
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

        [UnityTearDown]
        protected virtual IEnumerator TearDown()
        {
            ForceNewDocument();
            MouseCaptureController.ReleaseMouse();

            yield return null;
            BuilderWindow.Close();
            yield return null;
        }

        protected void ForceNewDocument()
        {
            BuilderWindow.rootVisualElement.Q<BuilderToolbar>().ForceNewDocument();
        }

        protected IEnumerator AddVisualElement()
        {
            yield return AddElement(nameof(VisualElement));
        }

        protected IEnumerator AddButtonElement()
        {
            yield return AddElement(nameof(Button));
        }

        protected IEnumerator AddTextFieldElement()
        {
            yield return AddElement("Text Field");
        }

        protected IEnumerator AddElement(string elementLabel)
        {
            var label = BuilderTestsHelper.GetLabelWithName(LibraryPane, elementLabel);
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
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builderWindow, KeyCode.Return);
        }

        protected void CreateTestUXMLFile()
        {
            File.WriteAllText(k_TestUXMLFilePath, k_TestUXMLFileContent);
            AssetDatabase.ImportAsset(k_TestUXMLFilePath);
        }

        protected void DeleteTestUXMLFile()
        {
            AssetDatabase.DeleteAsset(k_TestUXMLFilePath);
        }

        internal BuilderExplorerItem GetStyleSelectorNodeWithName(string selectorName)
        {
            return BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, selectorName);
        }

        internal BuilderExplorerItem GetFirstExplorerVisualElementNode(string nodeName)
        {
            return BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nodeName);
        }

        internal VisualElement GetFirstDocumentElement()
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