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
    class BuilderDocumentTests : BuilderIntegrationTest
    {
        const string k_NewUxmlFilePath = "Assets/BuildDocumentTests__TestUI.uxml";

        protected override IEnumerator TearDown()
        {
            ForceNewDocument();
            AssetDatabase.DeleteAsset(k_NewUxmlFilePath);
            var guid = AssetDatabase.AssetPathToGUID(k_NewUxmlFilePath);
            if (!string.IsNullOrEmpty(guid))
            {
                var folderPath = BuilderConstants.BuilderDocumentDiskSettingsJsonFolderAbsolutePath;
                var fileName = guid + ".json";
                var path = folderPath + "/" + fileName;
                File.Delete(path);
            }

            yield return base.TearDown();
        }

        void CheckNoUSSDocument()
        {
            var document = BuilderWindow.document;

            Assert.Null(document.firstStyleSheet);
            Assert.AreEqual(document.openUSSFiles.Count, 0);

            Assert.False(document.visualTreeAsset.IsEmpty());
            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));

            var labelInDocument = BuilderWindow.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
        }

        IEnumerator CheckMultiUSSDocument()
        {
            var document = BuilderWindow.document;

            Assert.NotNull(document.firstStyleSheet);
            Assert.AreEqual(document.openUSSFiles.Count, 2);

            Assert.False(document.visualTreeAsset.IsEmpty());
            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));

            yield return UIETestHelpers.Pause(1);

            var labelInDocument = BuilderWindow.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
            Assert.AreEqual(labelInDocument.resolvedStyle.width, 60);
            Assert.AreEqual(labelInDocument.resolvedStyle.backgroundColor, Color.green);
        }

        void UndoRedoCheckWithTextField()
        {
            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(2));
            Undo.PerformUndo();
            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));
            Undo.PerformRedo();
            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator SaveNewDocument()
        {
            var labelName = "test-label";

            AddElementCodeOnly<Label>(labelName);

            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));
            var labelInDocument = BuilderWindow.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
            Assert.AreEqual(labelInDocument.name, labelName);

            BuilderWindow.document.SaveUnsavedChanges(k_NewUxmlFilePath);

            var document = BuilderWindow.document;
            Assert.AreEqual(document.uxmlPath, k_NewUxmlFilePath);
            Assert.AreEqual(document.uxmlOldPath, k_NewUxmlFilePath);

            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));
            labelInDocument = BuilderWindow.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
            Assert.AreEqual(labelInDocument.name, labelName);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveAsWithNoUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestNoUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            BuilderWindow.document.SaveUnsavedChanges(k_NewUxmlFilePath, true);

            var document = BuilderWindow.document;
            Assert.AreEqual(document.uxmlPath, k_NewUxmlFilePath);
            Assert.AreEqual(document.uxmlOldPath, k_NewUxmlFilePath);

            yield return UIETestHelpers.Pause(1);

            CheckNoUSSDocument();
        }

        [UnityTest]
        public IEnumerator SaveAsWithMoreThanOneUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            BuilderWindow.document.SaveUnsavedChanges(k_NewUxmlFilePath, true);

            var document = BuilderWindow.document;
            Assert.AreEqual(document.uxmlPath, k_NewUxmlFilePath);
            Assert.AreEqual(document.uxmlOldPath, k_NewUxmlFilePath);

            yield return UIETestHelpers.Pause(1);

            yield return CheckMultiUSSDocument();
        }

        [Test]
        public void LoadExistingDocumentWithNoUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestNoUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            CheckNoUSSDocument();
        }

        [UnityTest]
        public IEnumerator LoadExistingDocumentWithMoreThanOneUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            yield return CheckMultiUSSDocument();
        }

        [UnityTest]
        public IEnumerator EnsureChangesAreUndoneIfOpeningNewDocWithoutSaving()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            var assetCount = asset.visualElementAssets.Count;
            BuilderWindow.LoadDocument(asset);
            Assert.AreEqual(BuilderWindow.document.visualTreeAsset, asset);

            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));

            yield return AddTextFieldElement();

            // Test restoration of backup.
            Assert.AreNotEqual(asset.visualElementAssets.Count, assetCount);
            ForceNewDocument();
            Assert.AreEqual(asset.visualElementAssets.Count, assetCount);
            var asset2 = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            Assert.AreEqual(asset2.visualElementAssets.Count, assetCount);
        }

        [UnityTest]
        public IEnumerator UndoRedoCreationOfTextFieldInMultiUSSDocument()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            yield return AddTextFieldElement();

            UndoRedoCheckWithTextField();
        }

        [UnityTest]
        public IEnumerator UndoRedoBeforeAndAfterGoingIntoPlaymode()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));

            yield return AddTextFieldElement();

            UndoRedoCheckWithTextField();

            yield return new EnterPlayMode();

            UndoRedoCheckWithTextField();

            yield return new ExitPlayMode();

            UndoRedoCheckWithTextField();

            yield return null;
        }

        [UnityTest]
        public IEnumerator UndoRedoBeforeAndAfterGoingIntoPlaymodeWithSceneReference()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);

            var newObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var component = newObject.AddComponent<Tests.UIBuilderUXMLReferenceForTests>();
            component.visualTreeAssetRef = asset;

            BuilderWindow.LoadDocument(asset);

            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));

            yield return AddTextFieldElement();

            UndoRedoCheckWithTextField();

            yield return new EnterPlayMode();

            UndoRedoCheckWithTextField();

            yield return new ExitPlayMode();

            UndoRedoCheckWithTextField();

            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsCopiedFromUnsavedDocument()
        {
            var documentHierarchyHeader = HierarchyPane.Q<BuilderExplorerItem>();
            yield return UIETestEvents.Mouse.SimulateClick(documentHierarchyHeader);

            var colorButton = InspectorPane.Q<Button>("Color");
            yield return UIETestEvents.Mouse.SimulateClick(colorButton);

            var colorField = InspectorPane.Q<ColorField>("background-color-field");
            colorField.value = Color.green;
            yield return UIETestHelpers.Pause(1);

            BuilderWindow.document.SaveUnsavedChanges(k_NewUxmlFilePath);
            Assert.That(BuilderWindow.document.settings.CanvasBackgroundMode, Is.EqualTo(BuilderCanvasBackgroundMode.Color));
            Assert.That(BuilderWindow.document.settings.CanvasBackgroundColor, Is.EqualTo(Color.green));
        }
    }
}
