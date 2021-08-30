using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        const int k_NoUSSElementCount = 1;
        const int k_MultiUSSElementCount = 4;

        public override void Setup()
        {
            base.Setup();

            // Make sure there's no modified version in memory.
            if (!EditorApplication.isPlaying)
            {
                AssetDatabase.ImportAsset(k_TestNoUSSDocumentUXMLFilePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(k_TestMultiUSSDocumentUXMLFilePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
        }

        protected override IEnumerator TearDown()
        {
            ForceNewDocument();
            AssetDatabase.DeleteAsset(k_NewUxmlFilePath);
            var guid = AssetDatabase.AssetPathToGUID(k_NewUxmlFilePath);
            if (!string.IsNullOrEmpty(guid))
            {
                var folderPath = BuilderConstants.builderDocumentDiskSettingsJsonFolderAbsolutePath;
                var fileName = guid + ".json";
                var path = folderPath + "/" + fileName;
                File.Delete(path);
            }

            yield return base.TearDown();
            DeleteTestUXMLFile();
            DeleteTestUSSFiles();
        }

        void CheckNoUSSDocument()
        {
            var document = builder.document;

            Assert.Null(document.firstStyleSheet);
            Assert.AreEqual(document.openUSSFiles.Count, 0);

            Assert.False(document.visualTreeAsset.IsEmpty());
            Assert.That(builder.documentRootElement.childCount, Is.EqualTo(k_NoUSSElementCount));

            var labelInDocument = builder.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
        }

        IEnumerator CheckMultiUSSDocument()
        {
            var document = builder.document;

            Assert.NotNull(document.firstStyleSheet);
            Assert.AreEqual(document.openUSSFiles.Count, 2);

            Assert.False(document.visualTreeAsset.IsEmpty());
            Assert.That(builder.documentRootElement.childCount, Is.EqualTo(k_MultiUSSElementCount));

            yield return UIETestHelpers.Pause(1);

            var labelInDocument = builder.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
            Assert.AreEqual(labelInDocument.resolvedStyle.width, 60);
            Assert.AreEqual(labelInDocument.resolvedStyle.backgroundColor, Color.green);
        }

        void UndoRedoCheckWithTextField()
        {
            Assert.That(builder.documentRootElement.childCount, Is.EqualTo(k_MultiUSSElementCount + 1));
            Undo.PerformUndo();
            Assert.That(builder.documentRootElement.childCount, Is.EqualTo(k_MultiUSSElementCount));
            Undo.PerformRedo();
            Assert.That(builder.documentRootElement.childCount, Is.EqualTo(k_MultiUSSElementCount + 1));
        }

#if !UNITY_2019_4
        [Test]
        public void LoadLegacyAllRootElementsUSSFile()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestLegacyAllRootElementsUSSDocumentUXMLFilePath);
            builder.LoadDocument(asset);

            // Check that we correctly recovered the global stylesheets from one of the root elements.
            Assert.AreEqual(2, builder.document.openUSSFiles.Count);

            // Check that the auto-fixing worked and we removed the redundant stylesheets on the root element.
            Assert.AreEqual(0, builder.document.visualTreeAsset.visualElementAssets[1].stylesheets.Count);
        }
#endif

        [UnityTest]
        public IEnumerator SaveNewDocument()
        {
            var labelName = "test-label";

            AddElementCodeOnly<Label>(labelName);

            Assert.That(builder.documentRootElement.childCount, Is.EqualTo(1));
            var labelInDocument = builder.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
            Assert.AreEqual(labelInDocument.name, labelName);

            builder.document.SaveUnsavedChanges(k_NewUxmlFilePath);

            var document = builder.document;
            Assert.AreEqual(document.uxmlPath, k_NewUxmlFilePath);
            Assert.AreEqual(document.uxmlOldPath, k_NewUxmlFilePath);

            Assert.That(builder.documentRootElement.childCount, Is.EqualTo(1));
            labelInDocument = builder.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
            Assert.AreEqual(labelInDocument.name, labelName);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveAsWithNoUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestNoUSSDocumentUXMLFilePath);
            builder.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            builder.document.SaveUnsavedChanges(k_NewUxmlFilePath, true);

            var document = builder.document;
            Assert.AreEqual(document.uxmlPath, k_NewUxmlFilePath);
            Assert.AreEqual(document.uxmlOldPath, k_NewUxmlFilePath);

            yield return UIETestHelpers.Pause(1);

            CheckNoUSSDocument();
        }

        [UnityTest]
        public IEnumerator SaveAsWithMoreThanOneUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            builder.LoadDocument(asset);

            builder.document.SaveUnsavedChanges(k_NewUxmlFilePath, true);

            var document = builder.document;
            Assert.AreEqual(document.uxmlPath, k_NewUxmlFilePath);
            Assert.AreEqual(document.uxmlOldPath, k_NewUxmlFilePath);

            yield return UIETestHelpers.Pause(1);

            yield return CheckMultiUSSDocument();
        }

#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator EnsureOnlyModifiedFilesAreSavedToDisk()
        {
            Dictionary<string, DateTime> beforeLastWriteTimes;
            
            Dictionary<string, DateTime> GetLastWriteTimes()
            {
                Dictionary<string, DateTime> ret = new Dictionary<string, DateTime>();

                foreach (var file in new string[] { k_TestUXMLFilePath, k_TestUSSFilePath, k_TestUSSFilePath_2 })
                {
                    ret[file] = (new FileInfo(file)).LastWriteTime;
                }
                return ret;
            };

            void SnapShotLastWriteTimes()
            {
                beforeLastWriteTimes = GetLastWriteTimes();
            }

            List<string> GetLastModifiedFiles()
            {
                List<string> files = new List<string>();
                var afterLastWriteTimes = GetLastWriteTimes();

                foreach (var times in beforeLastWriteTimes)
                {
                    if (times.Value != afterLastWriteTimes[times.Key])
                    {
                        files.Add(times.Key);
                    }
                }

                beforeLastWriteTimes = afterLastWriteTimes;
                return files;
            }

            void CheckModifiedFiles(string [] files)
            {
                var lastModifiedFiles = GetLastModifiedFiles();

                Assert.AreEqual(files.Length, lastModifiedFiles.Count);

                foreach (var file in files)
                {
                    Assert.Contains(file, lastModifiedFiles);
                }
            }

            CreateTestUXMLFile();
            CreateTestUSSFile(k_TestUSSFilePath, ".selector1 {}");
            CreateTestUSSFile(k_TestUSSFilePath_2, ".selector2 {}");

            yield return LoadTestUXMLDocument(k_TestUXMLFilePath);
            yield return CodeOnlyAddUSSToDocument(k_TestUSSFilePath);
            yield return CodeOnlyAddUSSToDocument(k_TestUSSFilePath_2);

            toolbar.SaveDocument(false);

            SnapShotLastWriteTimes();

            // 1. Verify that resaving without any change will not save any file to disk
            toolbar.SaveDocument(false);

            CheckModifiedFiles(new string[] {});

            // 2. Modify the uxml document adding a visual element and verify that only
            // k_TestUXMLFilePath was actually saved to disk
            yield return AddVisualElement();

            toolbar.SaveDocument(false);

            CheckModifiedFiles(new string[] { k_TestUXMLFilePath });

            // 3. Modify .selector1 in k_TestUSSFilePath and verify that only k_TestUSSFilePath 
            // was saved to disk
            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, ".selector1");
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            yield return EditInspectorStyleField("color", Color.red);

            toolbar.SaveDocument(false);

            CheckModifiedFiles(new string[] { k_TestUSSFilePath });

            // 4. Modify .selector1 in k_TestUSSFilePath and .selector2 in k_TestUSSFilePath_2
            // and verify that only k_TestUSSFilePath and k_TestUSSFilePath_2 were saved to disk
            yield return EditInspectorStyleField("color", Color.green);

            selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, ".selector2");
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            yield return EditInspectorStyleField("color", Color.blue);

            toolbar.SaveDocument(false);

            CheckModifiedFiles(new string[] { k_TestUSSFilePath, k_TestUSSFilePath_2 });

            // 5. Modify the uxml document, .selector1 in k_TestUSSFilePath and .selector2 in k_TestUSSFilePath_2
            // and verify that all files were saved to disk
            yield return EditInspectorStyleField("color", Color.yellow);

            selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, ".selector1");
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            yield return EditInspectorStyleField("color", Color.gray);
            
            yield return AddVisualElement();

            toolbar.SaveDocument(false);

            CheckModifiedFiles(new string[] { k_TestUXMLFilePath, k_TestUSSFilePath, k_TestUSSFilePath_2 });
        }

        [Test]
        public void LoadExistingDocumentWithNoUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestNoUSSDocumentUXMLFilePath);
            builder.LoadDocument(asset);

            CheckNoUSSDocument();
        }

        [UnityTest]
        public IEnumerator LoadExistingDocumentWithMoreThanOneUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            builder.LoadDocument(asset);

            yield return CheckMultiUSSDocument();
        }

        [Test]
        public void EnsureChangesAreUndoneIfOpeningNewDocWithoutSaving()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            var assetCount = asset.visualElementAssets.Count;
            builder.LoadDocument(asset);
            Assert.AreEqual(builder.document.visualTreeAsset, asset);

            Assert.That(builder.documentRootElement.childCount, Is.EqualTo(k_MultiUSSElementCount));

            AddElementCodeOnly<TextField>();
            // Test restoration of backup.
            Assert.AreNotEqual(asset.visualElementAssets.Count, assetCount);
            ForceNewDocument();
            Assert.AreEqual(asset.visualElementAssets.Count, assetCount);
            var asset2 = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            Assert.AreEqual(asset2.visualElementAssets.Count, assetCount);
        }

#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator EnsureNewDocumentCleansUpProperly()
        {
            CreateTestUXMLFile();
            CreateTestUSSFile();

            yield return LoadTestUXMLDocument(k_TestUXMLFilePath);
            yield return CodeOnlyAddUSSToDocument(k_TestUSSFilePath);
            Assert.NotNull(builder.document.activeStyleSheet);

            // Save
            builder.document.SaveUnsavedChanges(k_TestUXMLFilePath, false);

            ForceNewDocument();

            var assetCount = builder.document.visualTreeAsset.visualElementAssets.Count;

            yield return AddTextFieldElement();

            Assert.AreEqual(assetCount + 1, builder.document.visualTreeAsset.visualElementAssets.Count);
        }

#if (UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX) || UNITY_2020_2 || UNITY_2020_3)
        [UnityTest, Ignore("Test broken on 2019.4 on linux and on 2020.2 & 2020.3.")]
#else
        [UnityTest]
#endif
        public IEnumerator EnsureChangesToUXMLMadeExternallyAreReloaded()
        {
            const string testLabelName = "externally-added-label";

            CreateTestUXMLFile();

            yield return LoadTestUXMLDocument(k_TestUXMLFilePath);
            var assetCount = builder.document.visualTreeAsset.visualElementAssets.Count;
            yield return AddTextFieldElement();
            Assert.AreEqual(assetCount + 1, builder.document.visualTreeAsset.visualElementAssets.Count);

            // Save
            builder.document.SaveUnsavedChanges(k_TestUXMLFilePath, false);

            var vtaCopy = builder.document.visualTreeAsset.DeepCopy();
            var newElement = new VisualElementAsset(typeof(Label).ToString());
            VisualTreeAssetUtilities.InitializeElement(newElement);
            newElement.AddProperty("name", testLabelName);
            vtaCopy.AddElement(vtaCopy.GetRootUXMLElement(), newElement);
            var vtaCopyUXML = vtaCopy.GenerateUXML(k_TestUXMLFilePath, true);
            File.WriteAllText(k_TestUXMLFilePath, vtaCopyUXML);
            AssetDatabase.ImportAsset(k_TestUXMLFilePath, ImportAssetOptions.ForceUpdate);

            yield return UIETestHelpers.Pause(1);

            // Make sure the UI Builder reloaded.
            var label = builder.documentRootElement.Q<Label>(testLabelName);
            Assert.NotNull(label);
        }

        [UnityTest]
        public IEnumerator EnsureChangesToUSSMadeExternallyAreReloaded()
        {
            const string testSelector = ".externally-added-selector";

            CreateTestUXMLFile();
            CreateTestUSSFile();

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestUXMLFilePath);
            var assetCount = asset.visualElementAssets.Count;
            builder.LoadDocument(asset);
            Assert.AreEqual(builder.document.visualTreeAsset, asset);

            yield return CodeOnlyAddUSSToDocument(k_TestUSSFilePath);
            Assert.NotNull(builder.document.activeStyleSheet);

            // Save
            builder.document.SaveUnsavedChanges(k_TestUXMLFilePath, false);

            var styleSheetCopy = builder.document.activeStyleSheet.DeepCopy();
            styleSheetCopy.AddSelector(testSelector);
            var styleSheetCopyUSS = styleSheetCopy.GenerateUSS();
            File.WriteAllText(k_TestUSSFilePath, styleSheetCopyUSS);
            AssetDatabase.ImportAsset(k_TestUSSFilePath, ImportAssetOptions.ForceUpdate);

            yield return UIETestHelpers.Pause(1);

            // Make sure the UI Builder reloaded.
            var activeStyleSheet = builder.document.activeStyleSheet;
            var complexSelector = activeStyleSheet.complexSelectors.First();
            Assert.NotNull(complexSelector);
            Assert.AreEqual(StyleSheetToUss.ToUssSelector(complexSelector), testSelector);
        }

        [Test]
        public void UndoRedoCreationOfTextFieldInMultiUSSDocument()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            builder.LoadDocument(asset);
            AddElementCodeOnly<TextField>();

            UndoRedoCheckWithTextField();
        }

        [UnityTest]
        public IEnumerator UndoRedoBeforeAndAfterGoingIntoPlaymode()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            builder.LoadDocument(asset);

            Assert.That(builder.documentRootElement.childCount, Is.EqualTo(k_MultiUSSElementCount));

            AddElementCodeOnly<TextField>();

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

            builder.LoadDocument(asset);

            Assert.That(builder.documentRootElement.childCount, Is.EqualTo(k_MultiUSSElementCount));

            AddElementCodeOnly<TextField>();

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
            var documentHierarchyHeader = hierarchy.Q<BuilderExplorerItem>();
            yield return UIETestEvents.Mouse.SimulateClick(documentHierarchyHeader);

            var colorButton = inspector.Q<Button>("Color");
            yield return UIETestEvents.Mouse.SimulateClick(colorButton);

            var colorField = inspector.Q<ColorField>("background-color-field");
            colorField.value = Color.green;
            yield return UIETestHelpers.Pause(1);

            builder.document.SaveUnsavedChanges(k_NewUxmlFilePath);
            Assert.That(builder.document.settings.CanvasBackgroundMode, Is.EqualTo(BuilderCanvasBackgroundMode.Color));
            Assert.That(builder.document.settings.CanvasBackgroundColor, Is.EqualTo(Color.green));
        }

        [UnityTest]
        public IEnumerator EnsureStyleSheetPathsIsNotPopulatedAfterAddedUSS()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            CheckStyleSheetPathsIsNotPopulated();

            yield return null;
        }

        [UnityTest]
        public IEnumerator EnsureStyleSheetPathsIsNotPopulatedAfterLoadedDocument()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            builder.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            CheckStyleSheetPathsIsNotPopulated();

            yield return null;
        }

        [UnityTest]
        public IEnumerator EnsureStyleSheetPathsIsNotPopulatedAfterSavedDocument()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return UIETestHelpers.Pause(1);

            builder.document.SaveUnsavedChanges(k_NewUxmlFilePath, true);

            yield return UIETestHelpers.Pause(1);

            CheckStyleSheetPathsIsNotPopulated();

            yield return null;
        }
    }
}
