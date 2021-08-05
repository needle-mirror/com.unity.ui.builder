using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class LibraryPaneTests : BuilderIntegrationTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            var contentUpdated = false;
            BuilderLibraryContent.ResetProjectUxmlPathsHash();
            BuilderLibraryContent.OnLibraryContentUpdated += () =>
            {
                contentUpdated = true;
            };

            CreateTestUXMLFile();

            // TODO: We need to reconsider this pattern as it is very dangerous.
            // For one, [UniteSetUp] runs BEFORE [SetUp], so at this point
            // there is no UI Builder window open. As such, our
            // BuilderLibraryContent.AssetModificationProcessor.OnAssetChange() would
            // check for the "ActiveWindow" and because it was null it would never
            // call RegenerateLibraryContent(), and therefore OnLibraryContentUpdated.
            //
            // And that's how we get to an infinite loop.
            //
            // This would also happen if the test UXML file already existed in the project
            // (because of a previous test not cleaning up, for example).
            //
            // I've plugged all the holes:
            // - BuilderLibraryContent.AssetModificationProcessor.OnAssetChange() uses delayCall
            // - We always try to delete the text UXML file before creating a new one
            // - Added a "timeout" loop safety counter below so we never go infinite
            //
            // But we need to reconsider this pattern and be very careful how we override
            // or new [UnitySetUp] and [SetUp]. I would suggest even just sticking to
            // only using [UnitySetUp] and makeing [SetUp] non-overridable. It's still
            // a big problem either way because if you call base.UnitySetUp() _after_ your
            // new code, you're still going to run into problems.
            int count = 5;
            while (!contentUpdated)
            {
                count--;
                if (count == 0)
                {
                    Assert.Fail("We waited too long for the BuilderLibraryContent to update. Something is very wrong.");
                    break;
                }
                yield return UIETestHelpers.Pause();
            }
        }

        protected override IEnumerator TearDown()
        {
            // Switch back to the controls mode
            if (library != null)
                yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Standard);

            yield return base.TearDown();
            DeleteTestUXMLFile();
        }

        /// <summary>
        /// Displays project-defined factory elements and UXML files (with `.uxml` extension) under a **Project** heading. This includes assets inside the `Assets/` and `Packages/` folders.
        /// </summary>
        [UnityTest]
        public IEnumerator DisplaysProjectDefinedUXMLFilesInsideAssets()
        {
            yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Project);
            var testUXML = GetTestAssetsUXMLFileNode();
            Assert.That(testUXML, Is.Not.Null);
        }

        ITreeViewItem GetTestAssetsUXMLFileNode(string nodeName = k_TestUXMLFileName)
        {
#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var libraryTreeView = library.Q<TreeView>();
#else
            var libraryTreeView = library.Q<InternalTreeView>();
#endif
            var projectNode = (BuilderLibraryTreeItem)libraryTreeView.items
                .First(item => ((BuilderLibraryTreeItem)item).name.Equals(BuilderConstants.LibraryAssetsSectionHeaderName));
            libraryTreeView.ExpandItem(projectNode.id);

            var assetsNode = (BuilderLibraryTreeItem)projectNode.children?.FirstOrDefault();
            if (assetsNode == null || !assetsNode.name.Equals("Assets"))
                return null;

            libraryTreeView.ExpandItem(assetsNode.id);
            var testUXML = assetsNode.children
                .FirstOrDefault(item => ((BuilderLibraryTreeItem)item).name.Equals(nodeName));
            return testUXML;
        }

        /// <summary>
        /// Can double click to create a new element instance at the root.
        /// </summary>
#if UI_BUILDER_PACKAGE // Too unstable for trunk.
        [UnityTest]
        public IEnumerator CanDoubleClickToCreateNewElement()
        {
            Assert.That(viewport.documentRootElement.childCount, Is.EqualTo(0));
            var label = BuilderTestsHelper.GetLabelWithName(library, nameof(VisualElement));

            Assert.That(label, Is.Not.Null);
            yield return UIETestEvents.Mouse.SimulateDoubleClick(label);
            Assert.That(viewport.documentRootElement.childCount, Is.EqualTo(1));
        }
#endif

        /// <summary>
        /// Items that have corresponding `.uxml` assets have an "Open" button visible that opens the asset for editing in UI Builder. The currently open `.uxml` asset in the Library is grayed out and is not instantiable to prevent infinite recursion.
        /// </summary>
        [UnityTest, Ignore("If you have enough folders in the Assets folder, the SelectAndScrollToItemWithId() will not work and the test will incorrectly fail.")]
        public IEnumerator UxmlAssetsOpenButtonTest()
        {
            var testUXMLTreeViewItem = GetTestAssetsUXMLFileNode();
#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var libraryTreeView = library.Q<TreeView>();
#else
            var libraryTreeView = library.Q<InternalTreeView>();
#endif
            yield return libraryTreeView.SelectAndScrollToItemWithId(testUXMLTreeViewItem.id);
            yield return UIETestHelpers.Pause();
            var testUXMLLabel = libraryTreeView.Query<Label>(null, "unity-builder-library__tree-item-label")
                .Where(label => label.text.Equals(k_TestUXMLFileName)).ToList().First();
            Assert.That(testUXMLLabel, Is.Not.Null);
            var treeViewItem = testUXMLLabel.parent;
            var openButton = treeViewItem.Q<Button>();
            Assert.That(openButton, Style.Display(DisplayStyle.Flex));
            Assert.True(openButton.enabledInHierarchy);

            yield return UIETestEvents.Mouse.SimulateClick(openButton);
            Assert.False(openButton.enabledInHierarchy);
        }

        /// <summary>
        /// Can click and drag onto a Viewport element to create new instance as a child. This will also focus the Viewport pane.
        /// </summary>
        /// ///
        /// Instability failure details:
        /* DragOntoViewportElementToCreateNewInstanceAsChild (2.400s)
            ---
            Expected: <BuilderViewport  (x:0.00, y:0.00, width:500.00, height:760.00) world rect: (x:300.00, y:41.00, width:500.00, height:760.00)>
              But was:  null
            ---
            at Unity.UI.Builder.EditorTests.LibraryPaneTests+<DragOntoViewportElementToCreateNewInstanceAsChild>d__7.MoveNext () [0x00138] in C:\Prime\Repos\Builder\Builder2020.1\Packages\com.unity.ui.builder\Tests\Editor\IntegrationTests\LibraryPaneTests.cs:123
            at UnityEngine.TestTools.TestEnumerator+<Execute>d__5.MoveNext () [0x0004c] in C:\Prime\Repos\Builder\Builder2020.1\Library\PackageCache\com.unity.test-framework@1.1.11\UnityEngine.TestRunner\NUnitExtensions\Attributes\TestEnumerator.cs:31
        */
        [UnityTest, Ignore("This is unstable. I got it to fail consistently by just having a floating UI Builder window open at the same time.")]
        public IEnumerator DragOntoViewportElementToCreateNewInstanceAsChild()
        {
            AddElementCodeOnly();
            var documentElement = viewport.documentRootElement[0];

            var veLabel = BuilderTestsHelper.GetLabelWithName(library, nameof(VisualElement));
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                veLabel.worldBound.center,
                documentElement.worldBound.center);

            var hierarchyItems = BuilderTestsHelper.GetExplorerItemsWithName(hierarchy, nameof(VisualElement));
            documentElement = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItems[0]);
            var documentElement1 = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItems[1]);

            Assert.That(documentElement1.parent, Is.EqualTo(documentElement));
            Assert.That(builder.rootVisualElement.focusController.focusedElement, Is.EqualTo(viewport));
        }

        /// <summary>
        /// Can click and drag onto a Hierarchy element to create new instance as a child, or between elements to create as a sibling.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DragOntoHierarchyElementToCreateAsChild()
        {
            AddElementCodeOnly();
            yield return UIETestHelpers.Pause();
            var explorerItem = GetFirstExplorerItem();

            var veLabel = BuilderTestsHelper.GetLabelWithName(library, nameof(VisualElement));
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                veLabel.worldBound.center,
                explorerItem.worldBound.center);

            hierarchy.elementHierarchyView.ExpandRootItems();

            var hierarchyItems = BuilderTestsHelper.GetExplorerItemsWithName(hierarchy, nameof(VisualElement));
            var documentElement1 = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItems[0]);
            var documentElement2 = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItems[1]);
            Assert.That(documentElement2.parent, Is.EqualTo(documentElement1));
        }

        [UnityTest]
        public IEnumerator DragOntoHierarchyElementToCreateAsChildFailsForListView()
        {
            AddElementCodeOnly<ListView>("test-list-view");

            yield return UIETestHelpers.Pause();

            var listView = viewport.documentRootElement.Q<ListView>();
            Assert.NotNull(listView);

            var explorerItem = GetFirstExplorerItem();
            var listViewChildCount = listView.hierarchy.childCount;
            var veLabel = BuilderTestsHelper.GetLabelWithName(library, nameof(VisualElement));

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                veLabel.worldBound.center,
                explorerItem.worldBound.center);
            Assert.AreEqual(listViewChildCount, listView.hierarchy.childCount);

            var contentContainer = listView.Q("unity-content-container");
            Assert.NotNull(contentContainer);
            Assert.NotNull(contentContainer.GetFirstAncestorOfType<ListView>());
            Assert.NotNull(contentContainer.GetFirstAncestorOfType<BuilderCanvas>());
            selection.Select(null, contentContainer);
            selection.NotifyOfHierarchyChange();
            yield return UIETestHelpers.Pause();
            var contentContainerMenuItem = GetHierarchyExplorerItemByElementName("unity-content-container");
            Assert.NotNull(contentContainerMenuItem);

            var containerChildCount = contentContainer.hierarchy.childCount;
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                veLabel.worldBound.center,
                contentContainerMenuItem.worldBound.center);
            Assert.AreEqual(containerChildCount, contentContainer.hierarchy.childCount);
        }

        /// <summary>
        /// Can click and drag onto a Hierarchy element to create new instance as a child, or between elements to create as a sibling.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DragOntoHierarchyElementToCreateAsSibling()
        {
            AddElementCodeOnly();
            AddElementCodeOnly();

            // Set 500% zoom so we also test that element gets dragged into Hierarchy and not the Canvas (behind the Hierarchy).
            viewport.zoomScale = 5f;
            viewport.contentOffset = new Vector2(-400, 0);

            var firstVisualElementItem = GetFirstExplorerItem();
            yield return SelectLibraryTreeItemWithName("Text Field");
            var textFieldLibrary = BuilderTestsHelper.GetLabelWithName(library, "Text Field");

            var veBottomPosition = new Vector2(firstVisualElementItem.worldBound.center.x, firstVisualElementItem.worldBound.yMin);
            yield return UIETestEvents.Mouse.SimulateMouseEvent(builder, EventType.MouseDown, textFieldLibrary.worldBound.center);
            yield return UIETestEvents.Mouse.SimulateMouseMove(builder, textFieldLibrary.worldBound.center, firstVisualElementItem.worldBound.center);
            yield return UIETestEvents.Mouse.SimulateMouseMove(builder, firstVisualElementItem.worldBound.center, veBottomPosition);
            yield return UIETestEvents.Mouse.SimulateMouseEvent(builder, EventType.MouseUp, veBottomPosition);

            Assert.That(viewport.documentRootElement.childCount, Is.EqualTo(3));
            Assert.NotNull(viewport.documentRootElement.Q<TextField>());
        }

        /// <summary>
        /// Can create (double-click or drag) template instances from other `.uxml` files.
        /// </summary>
        [UnityTest, Ignore("If you have enough folders in the Assets folder, the SelectAndScrollToItemWithId() will not work and the test will incorrectly fail.")]
        public IEnumerator CreateTemplateInstancesFromUXML()
        {
            var testUXMLTreeViewItem = GetTestAssetsUXMLFileNode();
#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var libraryTreeView = library.Q<TreeView>();
#else
            var libraryTreeView = library.Q<InternalTreeView>();
#endif
            yield return libraryTreeView.SelectAndScrollToItemWithId(testUXMLTreeViewItem.id);
            yield return UIETestHelpers.Pause();
            var testUXMLLabel = libraryTreeView.Query<Label>(null, "unity-builder-library__tree-item-label")
                .Where(label => label.text.Equals(k_TestUXMLFileName)).ToList().First();
            Assert.That(testUXMLLabel, Is.Not.Null);

            yield return UIETestEvents.Mouse.SimulateDoubleClick(testUXMLLabel);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                testUXMLLabel.worldBound.center,
                hierarchy.worldBound.center);

            Assert.That(viewport.documentRootElement.childCount, Is.EqualTo(2));
            foreach (var child in viewport.documentRootElement.Children())
            {
                Assert.That(child, Is.TypeOf<TemplateContainer>());
            }
        }

        /// <summary>
        /// When creating a new empty VisualElement, it has an artificial minimum size and border which is reset as soon as you parent a child element under it or change its styling.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator VisualElementArtificialSizeResetWhenChildIsAdded()
        {
            yield return AddVisualElement();
            var explorerItem = GetFirstExplorerItem();
            var documentElement = GetFirstDocumentElement();

            Assert.That(documentElement[0].classList, Contains.Item(BuilderConstants.SpecialVisualElementInitialMinSizeClassName));

            // Add child.
            var veLabel = BuilderTestsHelper.GetLabelWithName(library, nameof(VisualElement));
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                veLabel.worldBound.center,
                explorerItem.worldBound.center);

            Assert.That(documentElement.childCount, Is.EqualTo(1));
            Assert.False(documentElement[0].classList.Contains(BuilderConstants.SpecialVisualElementInitialMinSizeClassName));
        }

        /// <summary>
        /// When creating a new empty VisualElement, it has an artificial minimum size and border which is reset as soon as you parent a child element under it or change its styling.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator VisualElementArtificialSizeResetOnStyleChange()
        {
            yield return AddVisualElement();
            var documentElement = GetFirstDocumentElement();
            Assert.That(documentElement.childCount, Is.EqualTo(1));

            // Change style.
            var displayFoldout = inspector.Query<PersistedFoldout>().Where(f => f.text.Equals("Display")).ToList().First();
            displayFoldout.value = true;

            var percentSlider = displayFoldout.Query<PercentSlider>().Where(t => t.label.Equals("Opacity")).ToList().First();
            percentSlider.value = 0.5f;

            Assert.That(documentElement.childCount, Is.EqualTo(0));
        }

        /// <summary>
        /// Hovering over items in the Library shows a preview of that element in a floating preview box. The preview uses the current Theme selected for the Canvas.
        /// </summary>
        [UnityTest, Ignore("Let finalize our decision about what kind of items should have a preview")]
        public IEnumerator HoveringOverItemShowsFloatingPreviewBox()
        {
            var veLabel = BuilderTestsHelper.GetLabelWithName(library, nameof(VisualElement));
            var preview = builder.rootVisualElement.Q<BuilderTooltipPreview>("library-tooltip-preview");
            Assert.That(preview.worldBound.size, Is.EqualTo(Vector2.zero));

            yield return UIETestEvents.Mouse.SimulateMouseEvent(builder, EventType.MouseMove, veLabel.worldBound.center);
            yield return UIETestHelpers.Pause();
            Assert.That(preview.worldBound.size, Is.Not.EqualTo(Vector2.zero));
        }

        /// <summary>
        /// Library pane updates if new `.uxml` files are added/deleted/moved/renamed to/from the project.
        /// </summary>
#if !UI_BUILDER_PACKAGE || UNITY_2021_2_OR_NEWER
        [UnityTest]
#else
        [UnityTest, Ignore("Infinite loop when run with 2021.1 or older")]
#endif
        public IEnumerator LibraryUpdatesWhenUXMLFilesAreAddedDeletedMoved()
        {
            // Switch to the project mode
            yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Project);

            // Add
            var uxmlTreeViewItem = GetTestAssetsUXMLFileNode();
            Assert.That(uxmlTreeViewItem, Is.Not.Null);

            // Move
            var contentUpdated = false;
            BuilderLibraryContent.OnLibraryContentUpdated += () =>
            {
                contentUpdated = true;
            };
            AssetDatabase.MoveAsset(k_TestUXMLFilePath, "Assets/NewName.uxml");
            while (!contentUpdated)
                yield return UIETestHelpers.Pause();

            uxmlTreeViewItem = GetTestAssetsUXMLFileNode("NewName.uxml");
            Assert.That(uxmlTreeViewItem, Is.Not.Null);

            // Delete
            contentUpdated = false;
            BuilderLibraryContent.OnLibraryContentUpdated += () =>
            {
                contentUpdated = true;
            };
            AssetDatabase.DeleteAsset("Assets/NewName.uxml");
            while (!contentUpdated)
                yield return UIETestHelpers.Pause();

            yield return UIETestHelpers.Pause();
            uxmlTreeViewItem = GetTestAssetsUXMLFileNode("NewName.uxml");
            Assert.That(uxmlTreeViewItem, Is.Null);
        }

        IEnumerator SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab tabName)
        {
            var controlsViewButton = library.Q<Button>(tabName.ToString());
            yield return UIETestEvents.Mouse.SimulateClick(controlsViewButton);
        }

        /// <summary>
        /// Can switch between **Controls** and **Project** view using tabs in the Library header.
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchLibraryBetweenTabs()
        {
            yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Standard);
            var controlsNode = FindLibraryItemWithData(BuilderConstants.LibraryControlsSectionHeaderName);
            Assert.That(controlsNode, Is.Not.Null);

            var containersNode = FindLibraryItemWithData(BuilderConstants.LibraryContainersSectionHeaderName);
            Assert.That(containersNode, Is.Not.Null);

            yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Project);
            var projectNode = FindLibraryItemWithData(BuilderConstants.LibraryAssetsSectionHeaderName);
            Assert.That(projectNode, Is.Not.Null);
        }

        /// <summary>
        /// Controls view mode can be switched to the tree view representation using **Tree View** option from the `...` options menu in the top right of the Library pane.
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchLibraryBetweenViewModes()
        {
            yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Standard);

            library.SetViewMode(BuilderLibrary.LibraryViewMode.IconTile);
            yield return UIETestHelpers.Pause();
            Assert.That(library.Q<BuilderLibraryPlainView>(), Is.Not.Null);

            library.SetViewMode(BuilderLibrary.LibraryViewMode.TreeView);
            yield return UIETestHelpers.Pause();
            Assert.That(library.Q<BuilderLibraryTreeView>(), Is.Not.Null);
        }
    }
}
