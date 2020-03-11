using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class BuilderDocumentTests : BuilderIntegrationTest
    {
        const string k_NewUxmlFilePath = "Assets/BuildDocumentTests__TestUI.uxml";
        const string k_NewUssFilePath = "Assets/BuildDocumentTests__TestUI.uss";

        protected override IEnumerator TearDown()
        {
            ForceNewDocument();
            AssetDatabase.DeleteAsset(k_NewUxmlFilePath);
            AssetDatabase.DeleteAsset(k_NewUssFilePath);
            var guid = AssetDatabase.AssetPathToGUID(k_NewUxmlFilePath);
            if (!string.IsNullOrEmpty(guid))
            {
                var folderPath = BuilderWindow.document.diskSettingsJsonFolderPath;
                var fileName = guid + ".json";
                var path = folderPath + "/" + fileName;
                File.Delete(path);
            }

            return base.TearDown();
        }

        [UnityTest]
        public IEnumerator SettingsCopiedFromUnsavedDocument()
        {
            var toolbar = ViewportPane.Q<BuilderToolbar>();
            var documentHierarchyHeader = HierarchyPane.Q<BuilderExplorerItem>();
            yield return UIETestEvents.Mouse.SimulateClick(documentHierarchyHeader);

            var colorButton = InspectorPane.Q<Button>("Color");
            yield return UIETestEvents.Mouse.SimulateClick(colorButton);

            var colorField = InspectorPane.Q<ColorField>("background-color-field");
            colorField.value = Color.green;
            yield return UIETestHelpers.Pause(1);

            toolbar.SaveDocument(k_NewUxmlFilePath, k_NewUssFilePath);
            Assert.That(BuilderWindow.document.settings.CanvasBackgroundMode, Is.EqualTo(BuilderCanvasBackgroundMode.Color));
            Assert.That(BuilderWindow.document.settings.CanvasBackgroundColor, Is.EqualTo(Color.green));
        }
    }
}
