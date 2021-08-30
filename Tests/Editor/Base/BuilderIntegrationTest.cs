using System.Collections;
using System.IO;
using System.Linq;
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
        protected const string k_TestUSSFileName = "MyTestVisualTreeAsset.uss";
        protected const string k_TestUSSFileName_2 = "MyTestVisualTreeAsset_2.uss";
        protected const string k_TestUXMLFileName = "MyTestVisualTreeAsset.uxml";
        protected const string k_TestEmptyUSSFileNameNoExt = "EmptyTestStyleSheet";
        protected const string k_TestNoUSSDocumentUXMLFileNameNoExt = "NoUSSDocument";
        protected const string k_TestMultiUSSDocumentUXMLFileNameNoExt = "MultiUSSDocument";
        protected const string k_TestLegacyAllRootElementsUSSDocumentUXMLFileNameNoExt = "LegacyAllRootElementsUSSDocument";
        protected const string k_ParentTestUXMLFileNameNoExt = "ParentTestUXMLDocument";
        protected const string k_ChildTestUXMLFileNameNoExt = "ChildTestUXMLDocument";

        protected const string k_TestEmptyUSSFileName = k_TestEmptyUSSFileNameNoExt + ".uss";
        protected const string k_TestNoUSSDocumentUXMLFileName = k_TestNoUSSDocumentUXMLFileNameNoExt + ".uxml";
        protected const string k_TestMultiUSSDocumentUXMLFileName = k_TestMultiUSSDocumentUXMLFileNameNoExt + ".uxml";
        protected const string k_TestLegacyAllRootElementsUSSDocumentUXMLFileName = k_TestLegacyAllRootElementsUSSDocumentUXMLFileNameNoExt + ".uxml";
        protected const string k_ParentTestUXMLFileName = k_ParentTestUXMLFileNameNoExt + ".uxml";
        protected const string k_ChildTestUXMLFileName = k_ChildTestUXMLFileNameNoExt + ".uxml";

        protected const string k_TestUSSFilePath = "Assets/" + k_TestUSSFileName;
        protected const string k_TestUSSFilePath_2 = "Assets/" + k_TestUSSFileName_2;
        protected const string k_TestUXMLFilePath = "Assets/" + k_TestUXMLFileName;
        protected const string k_TestEmptyUSSFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_TestEmptyUSSFileName;
        protected const string k_TestNoUSSDocumentUXMLFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_TestNoUSSDocumentUXMLFileName;
        protected const string k_TestMultiUSSDocumentUXMLFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_TestMultiUSSDocumentUXMLFileName;
        protected const string k_TestLegacyAllRootElementsUSSDocumentUXMLFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_TestLegacyAllRootElementsUSSDocumentUXMLFileName;
        protected const string k_ParentTestUXMLPath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_ParentTestUXMLFileName;
        protected const string k_ChildTestUXMLPath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_ChildTestUXMLFileName;

        // TODO: This needs to be converted to an actual file.
        protected static readonly string k_TestUXMLFileContent
            = WebUtility.UrlDecode("%3Cui%3AUXML+xmlns%3Aui%3D%22UnityEngine.UIElements%22+xmlns%3Auie%3D%22UnityEditor.UIElements%22%3E%0D%0A++++%3Cui%3AVisualElement%3E%0D%0A++++++++%3Cui%3AVisualElement+%2F%3E%0D%0A++++%3C%2Fui%3AVisualElement%3E%0D%0A%3C%2Fui%3AUXML%3E%0D%0A");

        protected Builder builder { get; private set; }
        protected BuilderSelection selection { get; private set; }
        protected BuilderLibrary library { get; private set; }
        protected BuilderHierarchy hierarchy { get; private set; }
        protected BuilderStyleSheets styleSheetsPane { get; private set; }
        protected BuilderViewport viewport { get; private set; }
        protected BuilderToolbar toolbar { get; private set; }
        protected BuilderInspector inspector { get; private set; }
        protected BuilderCanvas canvas { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            if (EditorApplication.isPlaying)
                builder = EditorWindow.GetWindow<Builder>();
            else
                builder = BuilderTestsHelper.MakeNewBuilderWindow();

            selection = builder.selection;
            canvas = builder.rootVisualElement.Q<BuilderCanvas>();
            library = builder.rootVisualElement.Q<BuilderLibrary>();
            hierarchy = builder.rootVisualElement.Q<BuilderHierarchy>();
            styleSheetsPane = builder.rootVisualElement.Q<BuilderStyleSheets>();
            viewport = builder.rootVisualElement.Q<BuilderViewport>();
            toolbar = viewport.Q<BuilderToolbar>();
            inspector = builder.rootVisualElement.Q<BuilderInspector>();

            if (EditorApplication.isPlaying)
                return;

            BuilderProjectSettings.Reset();
            BuilderProjectSettings.hideNotificationAboutMissingUITKPackage = true;
            ForceNewDocument();
            var createSelectorField = styleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Blur();
            library.SetViewMode(BuilderLibrary.LibraryViewMode.TreeView);
        }

        [UnityTearDown]
        protected virtual IEnumerator TearDown()
        {
            ForceNewDocument();
            MouseCaptureController.ReleaseMouse();

            yield return null;
            builder?.Close();
            yield return null;
        }

        protected void ForceNewDocument()
        {
            if (builder == null)
                return;

            builder.rootVisualElement.Q<BuilderToolbar>().NewDocument(false);
        }

        protected IEnumerator CodeOnlyAddUSSToDocument(string path)
        {
            var builderWindow = this.builder;

#if UNITY_2019_4
            // Need to have at least one element in the asset.
            if (builderWindow.document.visualTreeAsset.IsEmpty())
                AddElementCodeOnly("TestElement");
#endif

            yield return UIETestHelpers.Pause(1);

            // Make sure there's no modified version in memory.
            AssetDatabase.ImportAsset(
                k_TestEmptyUSSFilePath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            yield return UIETestHelpers.Pause(1);

            BuilderStyleSheetsUtilities.AddUSSToAsset(builderWindow, path);

            yield return UIETestHelpers.Pause(1);

            styleSheetsPane.elementHierarchyView.ExpandRootItems();
            hierarchy.elementHierarchyView.ExpandRootItems();
        }

        protected void AddElementCodeOnly(string name = "")
        {
            AddElementCodeOnly<VisualElement>(name);
        }

        protected void AddElementCodeOnly<T>(string name = "") where T : VisualElement, new()
        {
            var element = BuilderLibraryContent.GetLibraryItemForType(typeof(T)).makeVisualElementCallback.Invoke();

            if (!string.IsNullOrEmpty(name))
                element.name = name;

            builder.documentRootElement.Add(element);
            BuilderAssetUtilities.AddElementToAsset(builder.document, element);
            builder.OnEnableAfterAllSerialization();
            selection.NotifyOfHierarchyChange();
            hierarchy.elementHierarchyView.ExpandRootItems();
        }

        protected IEnumerator EnsureSelectorsCanBeAddedAndReloadBuilder()
        {
            var builderWindow = this.builder;

            // Need to have at least one element in the asset.
            if (builderWindow.document.visualTreeAsset.IsEmpty())
                AddElementCodeOnly("TestElement");

            yield return UIETestHelpers.Pause(1);

            // If the builder currently has no stylesheets,
            // we add the test one so we can add selectors.
            if (builderWindow.document.firstStyleSheet == null)
            {
                // Make sure there's no modified version in memory.
                AssetDatabase.ImportAsset(
                    k_TestEmptyUSSFilePath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

                yield return UIETestHelpers.Pause(1);

                BuilderStyleSheetsUtilities.AddUSSToAsset(builderWindow, k_TestEmptyUSSFilePath);
            }

            yield return UIETestHelpers.Pause(1);

            styleSheetsPane.elementHierarchyView.ExpandRootItems();
            hierarchy.elementHierarchyView.ExpandRootItems();
        }

        protected IEnumerator AddVisualElement()
        {
            yield return AddElement(nameof(VisualElement));
        }

        protected IEnumerator AddTextFieldElement()
        {
            yield return AddElement("Text Field");
        }

        protected BuilderLibraryTreeItem FindLibraryItemWithData(string data)
        {
#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var libraryTreeView = library.Q<TreeView>();
#else
            var libraryTreeView = library.Q<InternalTreeView>();
#endif
            foreach (var item in libraryTreeView.items)
            {
                if (item is BuilderLibraryTreeItem libraryTreeItem)
                {
                    if (libraryTreeItem.data.Equals(data))
                        return libraryTreeItem;
                }
            }

            return null;
        }

        protected IEnumerator SelectLibraryTreeItemWithName(string elementLabel)
        {
            var builderLibraryTreeItem = FindLibraryItemWithData(elementLabel);
            Assert.IsNotNull(builderLibraryTreeItem);
#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var libraryTreeView = library.Q<TreeView>();
#else
            var libraryTreeView = library.Q<InternalTreeView>();
#endif
            yield return libraryTreeView.SelectAndScrollToItemWithId(builderLibraryTreeItem.id);
        }

        protected IEnumerator AddElement(string elementLabel)
        {
            yield return SelectLibraryTreeItemWithName(elementLabel);
            var label = BuilderTestsHelper.GetLabelWithName(library, elementLabel);
            Assert.IsNotNull(label);

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                label.worldBound.center,
                hierarchy.worldBound.center);

            yield return UIETestHelpers.Pause(1);
        }

        protected IEnumerator AddSelector(string selectorName)
        {
            // TODO: No idea why but the artificial way of adding selectors with AddSelector() produces
            // selector elements that have no layout. I don't know why they don't layout even
            // though they are part of the hierarchy and have a panel! The Inspector remains blank because
            // it needs elements to be layed out.

            var builderWindow = this.builder;

            var inputField = styleSheetsPane.Q<TextField>(className: BuilderNewSelectorField.s_TextFieldUssClassName);
            inputField.visualInput.Focus();

            // Make
            yield return UIETestEvents.KeyBoard.SimulateTyping(builderWindow, selectorName);
            // TODO: I noticed many times the same key events being sent again (twice).
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builderWindow, KeyCode.Return);

            // TODO: This does not always fire. Most of the time, the Blur event never makes
            // it to the control.
            inputField.visualInput.Blur();

            yield return UIETestHelpers.Pause(1);
        }

        protected void CreateTestUSSFile()
        {
            CreateTestUSSFile(k_TestUSSFilePath);
        }

        protected void CreateTestUSSFile(string file, string content = "")
        {
            // We have tests that _wait_ for the AssetModificationProcessor to kick in with
            // the new asset being created here. If we, for some reason, leak the asset
            // from a previous run and we don't _re_create it, those some tests may way
            // forever. It is very important to delete the file if it's already there
            // and re-create it.
            AssetDatabase.DeleteAsset(file);
            AssetDatabase.Refresh();

            File.WriteAllText(file, content);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate);
        }

        protected void CreateTestUXMLFile()
        {
            // We have tests that _wait_ for the AssetModificationProcessor to kick in with
            // the new asset being created here. If we, for some reason, leak the asset
            // from a previous run and we don't _re_create it, those some tests may way
            // forever. It is very important to delete the file if it's already there
            // and re-create it.
            AssetDatabase.DeleteAsset(k_TestUXMLFilePath);
            AssetDatabase.Refresh();

            File.WriteAllText(k_TestUXMLFilePath, k_TestUXMLFileContent);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(k_TestUXMLFilePath, ImportAssetOptions.ForceUpdate);
        }

        protected void DeleteTestUSSFiles()
        {
            AssetDatabase.DeleteAsset(k_TestUSSFilePath);
            AssetDatabase.DeleteAsset(k_TestUSSFilePath_2);
        }

        protected void DeleteTestUXMLFile()
        {
            AssetDatabase.DeleteAsset(k_TestUXMLFilePath);
        }

        protected IEnumerator LoadTestUXMLDocument(string filePath = k_TestUXMLFilePath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(filePath);
            Assert.That(asset, Is.Not.Null);
            builder.LoadDocument(asset);
            yield return UIETestHelpers.Pause();
            hierarchy.elementHierarchyView.ExpandRootItems();
            styleSheetsPane.elementHierarchyView.ExpandRootItems();
        }

        protected IEnumerator ReturnToParentDocumentThroughEntryItem(BuilderTestContextualMenuManager menu, string parentString, string parentName = null)
        {
            // Go back to root document through 'entry' item context menu
            BuilderExplorerItem parentRoot;
            if (parentName != null)
                parentRoot = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, parentName);
            else
                parentRoot = BuilderTestsHelper.GetHeaderItem(hierarchy);
            Assert.NotNull(parentRoot);
            yield return UIETestEvents.Mouse.SimulateClick(parentRoot, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var parentClick = menu.FindMenuAction(parentString);
            Assert.That(parentClick, Is.Not.Null);
            parentClick.Execute();
            yield return UIETestHelpers.Pause(1);
        }

        // By default opens in isolation mode
        protected IEnumerator OpenChildTemplateContainerAsSubDocument(BuilderTestContextualMenuManager menu, string nameOfChildSubDocument, bool inPlace = false)
        {
            hierarchy.elementHierarchyView.ExpandAllItems();

            // Open child
            var childInHierarchy = BuilderTestsHelper.GetExplorerItemsWithName(hierarchy, nameOfChildSubDocument);
            Assert.NotZero(childInHierarchy.Count);

            // Simulate right click on child TemplateContainer
            yield return UIETestEvents.Mouse.SimulateClick(childInHierarchy[0], MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            DropdownMenuAction subdocumentClick;
            if (inPlace)
                subdocumentClick = menu.FindMenuAction(BuilderConstants.ExplorerHierarchyPaneOpenSubDocumentInPlace);
            else
                subdocumentClick = menu.FindMenuAction(BuilderConstants.ExplorerHierarchyPaneOpenSubDocument);

            Assert.That(subdocumentClick, Is.Not.Null);
            subdocumentClick.Execute();
        }

        internal BuilderExplorerItem GetStyleSelectorNodeWithName(string selectorName)
        {
            return BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, selectorName);
        }

        internal BuilderExplorerItem GetHierarchyExplorerItemByElementName(string name)
        {
            return hierarchy.Query<BuilderExplorerItem>()
                .Where(item => BuilderTestsHelper.GetLinkedDocumentElement(item).name == name).ToList().First();
        }

        internal BuilderExplorerItem GetFirstExplorerVisualElementNode(string nodeName)
        {
            return BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nodeName);
        }

        internal VisualElement GetFirstDocumentElement()
        {
            return viewport.documentRootElement[0];
        }

        internal BuilderExplorerItem GetFirstExplorerItem()
        {
            var firstDocumentElement = viewport.documentRootElement[0];
            return BuilderTestsHelper.GetLinkedExplorerItem(firstDocumentElement);
        }

        public void CheckStyleSheetPathsIsNotPopulated()
        {
            var vta = builder.document.visualTreeAsset;

            Assert.Greater(vta.visualElementAssets.Count, 0);
#if !UI_BUILDER_PACKAGE || UNITY_2020_2_OR_NEWER
            Assert.Greater(vta.stylesheets.Count(), 0);
#endif

            foreach (var asset in vta.visualElementAssets)
            {
                Assert.AreEqual(0, asset.stylesheetPaths.Count());
            }
        }

        public VisualElement FindInspectorStyleField(string propName)
        {
            var field = inspector.styleFields.m_StyleFields[propName].First();

            var curParent = field.parent;

            while (curParent != inspector)
            {
                if (curParent is Foldout foldout)
                {
                    foldout.value = true;
                }
                else if (curParent is PersistedFoldout persistedFoldout)
                {
                    persistedFoldout.value = true;
                }
                curParent = curParent.parent;
            }
            return field;
        }

        public T FindInspectorStyleField<T>(string name) where T : VisualElement
        {
            return FindInspectorStyleField(name) as T;
        }

        public IEnumerator EditInspectorStyleField<T>(string name, T value)
        {
            var fieldField = FindInspectorStyleField<BaseField<T>>(name);

            fieldField.value = value;
            yield return null;
        }
    }
}
