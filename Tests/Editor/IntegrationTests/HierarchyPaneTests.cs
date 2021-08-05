using System;
using System.Collections;
using System.Linq;
using System.Net;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Toolbar = UnityEditor.UIElements.Toolbar;

namespace Unity.UI.Builder.EditorTests
{
    class HierarchyPaneTests : BuilderIntegrationTest
    {
        /// <summary>
        /// Can click to select an element.
        /// </summary>
        [UnityTest]
        public IEnumerator ClickToSelect()
        {
            const string testElementName = "test_element_name";
            AddElementCodeOnly<TextField>(testElementName);
            selection.ClearSelection(null);

            yield return UIETestHelpers.Pause();
            var hierarchyCreatedItem = GetHierarchyExplorerItemByElementName(testElementName);
            Assert.That(hierarchyCreatedItem, Is.Not.Null);

#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var hierarchyTreeView = hierarchy.Q<TreeView>();
#else
            var hierarchyTreeView = hierarchy.Q<InternalTreeView>();
#endif
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Null);
            Assert.That(selection.isEmpty, Is.True);

            yield return UIETestEvents.Mouse.SimulateClick(hierarchyCreatedItem);
            var documentElement = GetFirstDocumentElement();
            Assert.That(documentElement.name, Is.EqualTo(testElementName));

            var selectedItem = (TreeViewItem<VisualElement>)hierarchyTreeView.GetSelectedItem();
            Assert.That(documentElement, Is.EqualTo(selectedItem.data));
            Assert.That(selection.selection.First(), Is.EqualTo(documentElement));
        }

        /// <summary>
        /// Can drag element onto other elements in the Hierarchy to re-parent.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DragToReparentInHierarchy()
        {
            AddElementCodeOnly();
            AddElementCodeOnly();
            yield return UIETestHelpers.Pause();

            var documentElement1 = viewport.documentRootElement[0];
            var documentElement2 = viewport.documentRootElement[1];

            var hierarchyItems = BuilderTestsHelper.GetExplorerItemsWithName(hierarchy, nameof(VisualElement));
            var hierarchyItem1 = hierarchyItems[0];
            var hierarchyItem2 = hierarchyItems[1];

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                hierarchyItem1.worldBound.center,
                hierarchyItem2.worldBound.center);

            yield return null;
            Assert.That(viewport.documentRootElement.childCount, Is.EqualTo(1));
            Assert.That(documentElement2.parent, Is.EqualTo(viewport.documentRootElement));
            Assert.That(documentElement1.parent, Is.EqualTo(documentElement2));
        }

        /// <summary>
        /// Can drag an element onto other elements in the Viewport to re-parent.
        /// </summary>
        [UnityTest, Ignore("Remove ignore once reparenting bug is fixed.")]
        public IEnumerator DragToReparentInViewport()
        {
            AddElementCodeOnly();
            AddElementCodeOnly();

            var documentElement1 = viewport.documentRootElement[0];
            var documentElement2 = viewport.documentRootElement[1];

            var hierarchyItems = BuilderTestsHelper.GetExplorerItemsWithName(hierarchy, nameof(VisualElement));
            var hierarchyItem1 = hierarchyItems[0];

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                hierarchyItem1.worldBound.center,
                documentElement2.worldBound.center);

            yield return UIETestHelpers.Pause();
            Assert.That(viewport.documentRootElement.childCount, Is.EqualTo(1));
            Assert.That(documentElement2.parent, Is.EqualTo(viewport.documentRootElement));
            Assert.That(documentElement1.parent, Is.EqualTo(documentElement2));
        }

        /// <summary>
        /// Can drag an element between other elements to reorder, with live preview in the Canvas.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DragBetweenAndLivePreview()
        {
            AddElementCodeOnly();
            AddElementCodeOnly();
            AddElementCodeOnly<TextField>();
            yield return UIETestHelpers.Pause();

            var textFieldCanvas = viewport.documentRootElement[2];
            var firstVisualElementHierarchy = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(VisualElement));
            var textFieldHierarchy = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(TextField));

            Assert.That(viewport.documentRootElement.IndexOf(textFieldCanvas), Is.EqualTo(2));

            yield return UIETestEvents.Mouse.SimulateMouseEvent(builder, EventType.MouseDown, textFieldHierarchy.worldBound.center);
            var textFieldCenter = textFieldHierarchy.worldBound.center;
            var veBottomPosition = new Vector2(textFieldCenter.x, firstVisualElementHierarchy.worldBound.yMax);
            yield return UIETestEvents.Mouse.SimulateMouseMove(builder, textFieldCenter, veBottomPosition);
            yield return UIETestEvents.Mouse.SimulateMouseEvent(builder, EventType.MouseUp, veBottomPosition);

            Assert.That(viewport.documentRootElement.IndexOf(textFieldCanvas), Is.EqualTo(1));
        }

        /// <summary>
        /// Elements are displayed using their #name in blue. If they have no name, they are displayed using their C# type in white.
        /// Can double-click on an item to rename it.
        /// During element rename, if new name is not valid, an error message will display and rename will not be applied - keeping the focus on the rename field.
        /// </summary>
        ///
        /// Instability failure details:
        /* DisplayNameStyleAndRenameOption (1.119s)
            ---
            Expected string length 9 but was 0. Strings differ at index 0.
              Expected: "test_name"
              But was:  <string.Empty>
              -----------^
            ---
            at Unity.UI.Builder.EditorTests.HierarchyPaneTests+<DisplayNameStyleAndRenameOption>d__4.MoveNext ()[0x0016c] in C:\Prime\Repos\Builder\Builder2020.1\Packages\com.unity.ui.builder\Tests\Editor\IntegrationTests\HierarchyPaneTests.cs:131
            at UnityEngine.TestTools.TestEnumerator+<Execute>d__5.MoveNext ()[0x0004c] in C:\Prime\Repos\Builder\Builder2020.1\Library\PackageCache\com.unity.test-framework@1.1.11\UnityEngine.TestRunner\NUnitExtensions\Attributes\TestEnumerator.cs:31
        */
        [UnityTest, Ignore("This is unstable. I got it to fail consistently by just having a floating UI Builder window open at the same time.")]
        public IEnumerator DisplayNameStyleAndRenameOption()
        {
            const string testItemName = "test_name";
            AddElementCodeOnly();
            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(VisualElement));
            var documentElement = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItem);
            var nameLabel = hierarchyItem.Q<Label>(className: BuilderConstants.ExplorerItemLabelClassName);

            Assert.That(nameLabel.text, Is.EqualTo(nameof(VisualElement)));
            Assert.That(nameLabel.classList, Contains.Item(BuilderConstants.ElementTypeClassName));

            yield return UIETestEvents.Mouse.SimulateDoubleClick(hierarchyItem);
            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, testItemName);
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builder, KeyCode.Return);

            Assert.That(documentElement.name, Is.EqualTo(testItemName));

            hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, BuilderConstants.UssSelectorNameSymbol + testItemName);
            nameLabel =  hierarchyItem.Q<Label>(className: BuilderConstants.ExplorerItemLabelClassName);
            Assert.That(nameLabel.classList, Contains.Item(BuilderConstants.ElementNameClassName));

            hierarchyItem = GetFirstExplorerItem();
            yield return UIETestEvents.Mouse.SimulateDoubleClick(hierarchyItem);
            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, "invalid&name");
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builder, KeyCode.Return);
            Assert.That(documentElement.name, Is.EqualTo(testItemName));
        }

        /// <summary>
        /// When editing name of element in Hierarchy, clicking somewhere else will commit the change (if the new name is valid).
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator OutsideClickWillCommitRename()
        {
            const string testItemName = "test_name";
            AddElementCodeOnly();
            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(VisualElement));
            var documentElement = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItem);
            var nameLabel = hierarchyItem.Q<Label>(className: BuilderConstants.ExplorerItemLabelClassName);

            Assert.That(nameLabel.text, Is.EqualTo(nameof(VisualElement)));
            Assert.That(nameLabel.classList, Contains.Item(BuilderConstants.ElementTypeClassName));

            yield return UIETestEvents.Mouse.SimulateDoubleClick(hierarchyItem);
            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, testItemName);
            yield return UIETestEvents.Mouse.SimulateClick(viewport);

            Assert.That(documentElement.name, Is.EqualTo(testItemName));

            hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, BuilderConstants.UssSelectorNameSymbol + testItemName);
            nameLabel =  hierarchyItem.Q<Label>(className: BuilderConstants.ExplorerItemLabelClassName);
            Assert.That(nameLabel.classList, Contains.Item(BuilderConstants.ElementNameClassName));
        }

        /// <summary>
        /// When editing name of element in Hierarchy, hitting the Esc key will cancel the edit and revert to value before the edit started.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator EscKeyWillCancelRename()
        {
            const string testItemName = "test_name";
            AddElementCodeOnly();
            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(VisualElement));
            var documentElement = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItem);
            Assert.That(string.IsNullOrEmpty(documentElement.name));

            yield return UIETestEvents.Mouse.SimulateDoubleClick(hierarchyItem);
            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, testItemName);
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builder, KeyCode.Escape);

            // Test that not only the name has not changed to the new value entered...
            hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(VisualElement));
            documentElement = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItem);
            Assert.AreNotEqual(documentElement.name, testItemName);
            // But is also equal to its original name
            Assert.That(string.IsNullOrEmpty(documentElement.name));
        }

        /// <summary>
        /// Elements are displayed grayed out if they are children of a template instance or C# type.
        /// </summary>
        [UnityTest]
        public IEnumerator CSharpTypeTemplateChildrenMustBeGrayedOutAndNotEditable()
        {
            AddElementCodeOnly<TextField>();
            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(TextField));
            yield return UIETestHelpers.ExpandTreeViewItem(hierarchyItem);

            var textFieldDocumentElement = GetFirstDocumentElement();
            Assert.That(textFieldDocumentElement.childCount, Is.GreaterThan(0));
            BuilderExplorerItem lastChild = null;
            foreach (var child in textFieldDocumentElement.Children())
            {
                lastChild = BuilderTestsHelper.GetLinkedExplorerItem(child);
                Assert.That(lastChild.row().classList, Contains.Item(BuilderConstants.ExplorerItemHiddenClassName));
            }

            yield return UIETestEvents.Mouse.SimulateClick(lastChild);
            inspector.Query<ToggleButtonStrip>().ForEach(toggleButtonStrip =>
            {
                Assert.That(toggleButtonStrip.enabledInHierarchy, Is.False);
            });

            inspector.Query<PercentSlider>().ForEach(percentSlider =>
            {
                Assert.That(percentSlider.enabledInHierarchy, Is.False);
            });
        }

        /// <summary>
        /// Selecting an style selector or a the main StyleSheet in the StyleSheets pane should deselect any selected tree items in the Hierarchy.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectingStyleSelectorOrStyleSheetDeselectsHierarchyItems()
        {
            AddElementCodeOnly();
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();
            yield return AddSelector(StyleSheetsPaneTests.TestSelectorName);

            // Deselect
            yield return UIETestEvents.Mouse.SimulateClick(hierarchy);
#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var hierarchyTreeView = hierarchy.Q<TreeView>();
#else
            var hierarchyTreeView = hierarchy.Q<InternalTreeView>();
#endif
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Null);

            // Select hierarchy item
            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(VisualElement));
            yield return UIETestEvents.Mouse.SimulateClick(hierarchyItem);
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Not.Null);

            // Select test selector
            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, StyleSheetsPaneTests.TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(selector);
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Null);

            // Select hierarchy item
            yield return UIETestEvents.Mouse.SimulateClick(hierarchyItem);
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Not.Null);

            // Select Uss file name header
            var header = BuilderTestsHelper.GetHeaderItem(styleSheetsPane);
            yield return UIETestEvents.Mouse.SimulateClick(header);
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Null);
        }

        /// <summary>
        /// Selecting the StyleSheet pane should deselect any selected tree items in the Hierarchy.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectingStyleSheetPaneDeselectsHierarchyItems()
        {
            AddElementCodeOnly();
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            // Deselect
            yield return UIETestEvents.Mouse.SimulateClick(hierarchy);
#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var hierarchyTreeView = hierarchy.Q<TreeView>();
#else
            var hierarchyTreeView = hierarchy.Q<InternalTreeView>();
#endif
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Null);

            // Select hierarchy item
            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(VisualElement));
            yield return UIETestEvents.Mouse.SimulateClick(hierarchyItem);
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Not.Null);

            // Select test selector
            yield return UIETestEvents.Mouse.SimulateClick(styleSheetsPane);
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Null);
        }

        readonly string m_ExpectedUXMLString
            = WebUtility.UrlDecode("%3Cui%3AUXML+xmlns%3Aui%3D%22UnityEngine.UIElements%22+xmlns%3Auie%3D%22UnityEditor.UIElements%22%3E%0A++++%3Cui%3AVisualElement%3E%0A++++++++%3Cui%3AVisualElement+%2F%3E%0A++++%3C%2Fui%3AVisualElement%3E%0A%3C%2Fui%3AUXML%3E%0A");

        /// <summary>
        /// Can copy/paste the UXML for the element to/from a text file.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest, Ignore("Too unstable.")]
#endif
        public IEnumerator CopyPasteUXML()
        {
            AddElementCodeOnly();
            AddElementCodeOnly();
            yield return UIETestHelpers.Pause();

            var hierarchyItems = BuilderTestsHelper.GetExplorerItemsWithName(hierarchy, nameof(VisualElement));
            var hierarchyItem1 = hierarchyItems[0];
            var hierarchyItem2 = hierarchyItems[1];

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                hierarchyItem1.worldBound.center,
                hierarchyItem2.worldBound.center);

            var complexItem =  GetFirstExplorerItem();

            var newlineFixedExpectedUXML = m_ExpectedUXMLString;
            if (BuilderConstants.NewlineChar != BuilderConstants.newlineCharFromEditorSettings)
                newlineFixedExpectedUXML = newlineFixedExpectedUXML.Replace(
                    BuilderConstants.NewlineChar,
                    BuilderConstants.newlineCharFromEditorSettings);

            // Copy to UXML
            yield return UIETestEvents.Mouse.SimulateClick(complexItem);
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Copy);
            Assert.That(BuilderEditorUtility.systemCopyBuffer, Is.EqualTo(newlineFixedExpectedUXML));

            ForceNewDocument();
            BuilderEditorUtility.systemCopyBuffer = string.Empty;
            yield return UIETestEvents.Mouse.SimulateClick(hierarchy);
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Paste);
            var explorerItems = BuilderTestsHelper.GetExplorerItems(hierarchy);
            Assert.That(explorerItems, Is.Empty);

            BuilderEditorUtility.systemCopyBuffer = newlineFixedExpectedUXML;
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Paste);
            // var newItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(VisualElement));
#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var hierarchyTreeView = hierarchy.Q<TreeView>();
#else
            var hierarchyTreeView = hierarchy.Q<InternalTreeView>();
#endif
            hierarchyTreeView.ExpandItem(hierarchyTreeView.items.ToList()[1].id);

            explorerItems = BuilderTestsHelper.GetExplorerItems(hierarchy);
            Assert.That(explorerItems.Count, Is.EqualTo(2));
            Assert.That(BuilderTestsHelper.GetLinkedDocumentElement(explorerItems[1]).parent, Is.EqualTo(BuilderTestsHelper.GetLinkedDocumentElement(explorerItems[0])));
        }

        [UnityTest]
        public IEnumerator UssCopyBufferCannotBePastedInHierarchyPane()
        {
            // Load Test UXML File
            yield return LoadTestUXMLDocument(k_ChildTestUXMLPath);

            yield return null;

            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, ".unity-button");
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Copy);
            Assert.That(BuilderEditorUtility.IsUss(BuilderEditorUtility.systemCopyBuffer));

            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;

            yield return UIETestEvents.Mouse.SimulateClick(hierarchy, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var pasteMenuItem = menu.FindMenuAction("Paste");
            Assert.AreEqual(DropdownMenuAction.Status.Disabled, pasteMenuItem.status);
        }

        /// <summary>
        /// Dragging an element onto a template instance or C# type element in the Viewport re-parents it to the parent instance or C# element.
        /// Dragging an element onto a template instance or C# type element in the Hierarchy re-parents it to the parent instance or C# element.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator ReparentFlowWhenDraggingOntoCSharpTypeElement()
        {
            AddElementCodeOnly<TextField>();
            AddElementCodeOnly();
            yield return UIETestHelpers.Pause();

            var textFieldItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(TextField));
            var visualElementItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(VisualElement));
            var visualElementDocItem = BuilderTestsHelper.GetLinkedDocumentElement(visualElementItem);

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                visualElementItem.worldBound.center,
                textFieldItem.worldBound.center);
            Assert.That(visualElementDocItem.parent, Is.InstanceOf<TextField>());

            ForceNewDocument();
            AddElementCodeOnly<TextField>();
            AddElementCodeOnly();
            yield return UIETestHelpers.Pause();

            textFieldItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(TextField));
            visualElementItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(VisualElement));
            visualElementDocItem = BuilderTestsHelper.GetLinkedDocumentElement(visualElementItem);
            var textFieldDocItem = BuilderTestsHelper.GetLinkedDocumentElement(textFieldItem);

            // Need to make it taller to avoid the placement indicator triggering:
            textFieldDocItem.style.height = 200;
            yield return UIETestHelpers.Pause();

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                visualElementItem.worldBound.center,
                textFieldDocItem.worldBound.center);
            Assert.That(visualElementDocItem.parent, Is.InstanceOf<TextField>());
        }

        /// <summary>
        /// Dragging child elements of a template instance or C# type element within the element or outside does not work.
        /// </summary>
        [UnityTest]
        public IEnumerator DraggingChildElementsOfATemplateShouldNotWork()
        {
            AddElementCodeOnly<TextField>();
            AddElementCodeOnly();

            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(TextField));
            yield return UIETestHelpers.ExpandTreeViewItem(hierarchyItem);

            yield return UIETestHelpers.Pause();
            var textField = viewport.documentRootElement[0];
            var textFieldLabel = textField.Q<Label>();
            var visualElement = viewport.documentRootElement[1];
            var textFieldLabelExplorer  = BuilderTestsHelper.GetLinkedExplorerItem(textFieldLabel);
            var visualElementExplorer  = BuilderTestsHelper.GetLinkedExplorerItem(visualElement);

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                visualElementExplorer.worldBound.center,
                textFieldLabelExplorer.worldBound.center);
            Assert.That(visualElement.parent, Is.EqualTo(viewport.documentRootElement));

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                textFieldLabelExplorer.worldBound.center,
                visualElementExplorer.worldBound.center);
            Assert.That(textFieldLabel.parent, Is.EqualTo(textField));
        }

        /// <summary>
        /// With an element selected, you can use the standard short-cuts and Edit menu to copy/paste/duplicate/delete it.  The copied element is pasted at the same level of the hierarchy as the source element. If the source element's parent is deleted, the copied element is pasted at the root.
        /// </summary>
        ///
        /// Instability failure details:
        /* StandardShortCuts (1.280s)
            ---
            Expected: 2
              But was:  1
            ---
            at Unity.UI.Builder.EditorTests.HierarchyPaneTests+<StandardShortCuts>d__12.MoveNext () [0x0011d] in C:\Prime\Repos\Builder\Builder2020.1\Packages\com.unity.ui.builder\Tests\Editor\IntegrationTests\HierarchyPaneTests.cs:358
            at UnityEngine.TestTools.TestEnumerator+<Execute>d__5.MoveNext () [0x0004c] in C:\Prime\Repos\Builder\Builder2020.1\Library\PackageCache\com.unity.test-framework@1.1.11\UnityEngine.TestRunner\NUnitExtensions\Attributes\TestEnumerator.cs:31
        */
        [UnityTest, Ignore("This is unstable. I got it to fail consistently by just having a floating UI Builder window open at the same time.")]
        public IEnumerator StandardShortCuts()
        {
            yield return AddVisualElement();

            var explorerItems = BuilderTestsHelper.GetExplorerItems(hierarchy);
            Assert.That(explorerItems.Count, Is.EqualTo(1));
            yield return UIETestEvents.Mouse.SimulateClick(explorerItems[0]);

            // Rename
            const string renameString = "renameString";
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Rename);
            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, renameString);
            yield return UIETestEvents.Mouse.SimulateClick(viewport);

            explorerItems = BuilderTestsHelper.GetExplorerItems(hierarchy);
            var explorerItemLabel = explorerItems[0].Q<Label>();
            Assert.That(explorerItemLabel.text, Is.EqualTo("#" + renameString));

            yield return UIETestEvents.Mouse.SimulateClick(explorerItems[0]);

            // Duplicate
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Duplicate);
            explorerItems = BuilderTestsHelper.GetExplorerItems(hierarchy);
            Assert.That(explorerItems.Count, Is.EqualTo(2));

            // Copy/Paste
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Copy);
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Paste);

            explorerItems = BuilderTestsHelper.GetExplorerItems(hierarchy);
            Assert.That(explorerItems.Count, Is.EqualTo(3));

            // Delete
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builder, KeyCode.Delete);

            explorerItems = BuilderTestsHelper.GetExplorerItems(hierarchy);
            Assert.That(explorerItems.Count, Is.EqualTo(2));

            // Pasted as children of the parent of the currently selected element.

            AddElementCodeOnly<TextField>();
            var textField = viewport.documentRootElement.Q<TextField>();
            Assert.That(textField.childCount, Is.EqualTo(2));

            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Paste);
            Assert.That(textField.childCount, Is.EqualTo(2));
        }

        /// <summary>
        /// Right-clicking on a TemplateContainer within an open UXML file should allow to "Open as Sub-Document"
        /// and then returning back to the parent through clicking on "Return to Parent Document" on header.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator SubDocumentFunctionalityViaRightClickMenu()
        {
            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            // Load Test UXML File
            yield return LoadTestUXMLDocument(k_ParentTestUXMLPath);

            // Check that Breadcrumbs toolbar is NOT present
            var toolbar = viewport.Q<BuilderToolbar>();
            var breadcrumbsToolbar = toolbar.Q<Toolbar>(BuilderToolbar.BreadcrumbsToolbarName);
            var breadcrumbs = toolbar.Q<ToolbarBreadcrumbs>(BuilderToolbar.BreadcrumbsName);

            Assert.IsNotNull(breadcrumbsToolbar);
            Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, breadcrumbsToolbar.style.display);
            Assert.AreEqual(0, breadcrumbs.childCount);

            // Check that child is instantiated
            string nameOfChildSubDocument = "#ChildTestUXMLDocument";
            var childInHierarchy = BuilderTestsHelper.GetExplorerItemsWithName(hierarchy, nameOfChildSubDocument);
            Assert.AreEqual(1, childInHierarchy.Count);
            Assert.NotNull(childInHierarchy[0]);

            // Simulate right click on child TemplateContainer
            yield return UIETestEvents.Mouse.SimulateClick(childInHierarchy[0], MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var subdocumentClick = menu.FindMenuAction(BuilderConstants.ExplorerHierarchyPaneOpenSubDocument);
            Assert.That(subdocumentClick, Is.Not.Null);

            subdocumentClick.Execute();
            yield return UIETestHelpers.Pause(1);

            // Get parent document
            var parentRoot = BuilderTestsHelper.GetHeaderItem(hierarchy);
            Assert.NotNull(parentRoot);

            // Breadcrumbs is displaying
            Assert.AreEqual(2, breadcrumbs.childCount);
            Assert.AreEqual(breadcrumbsToolbar.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.Flex);

            // Click back to get to Parent
            yield return UIETestEvents.Mouse.SimulateClick(parentRoot, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var parentTestDocumentName = "ParentTestUXMLDocument";
            var parentTestFullString = BuilderConstants.ExplorerHierarchyReturnToParentDocument + BuilderConstants.SingleSpace + BuilderConstants.OpenBracket + parentTestDocumentName + BuilderConstants.CloseBracket;
            var parentClick = menu.FindMenuAction(parentTestFullString);
            Assert.That(parentClick, Is.Not.Null);
            parentClick.Execute();

            yield return UIETestHelpers.Pause(1);
            Assert.AreEqual(2, builder.documentRootElement.childCount); // test element now in test file

            Assert.AreEqual(0, breadcrumbs.childCount);
            Assert.AreEqual(breadcrumbsToolbar.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.None);
        }

#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator UnloadSubDocumentsOnFileOpen()
        {
            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            // Load Test UXML File
            yield return LoadTestUXMLDocument(k_ParentTestUXMLPath);

            // Open child
            string nameOfChildSubDocument = "#ChildTestUXMLDocument";
            var childInHierarchy = BuilderTestsHelper.GetExplorerItemsWithName(hierarchy, nameOfChildSubDocument);

            // Simulate right click on child TemplateContainer
            yield return UIETestEvents.Mouse.SimulateClick(childInHierarchy[0], MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var subdocumentClick = menu.FindMenuAction(BuilderConstants.ExplorerHierarchyPaneOpenSubDocument);
            Assert.That(subdocumentClick, Is.Not.Null);

            subdocumentClick.Execute();
            yield return UIETestHelpers.Pause(1);

            // Main part: opening NewDocument makes it the only document
            Assert.AreEqual(2, builder.document.openUXMLFiles.Count);
            builder.rootVisualElement.Q<BuilderToolbar>().NewDocument();
            Assert.AreEqual(1, builder.document.openUXMLFiles.Count);
        }

#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator OpenChildrenInPlace()
        {
            // Define all relevant file names
            var nameOfChildSubDocument = "#ChildTestUXMLDocument";
            var nameofGrandChildSubDocument = "#GrandChildTestUXMLDocument";
            var parentTestDocumentName = "ParentTestUXMLDocument";
            var parentTestFullString = BuilderConstants.ExplorerHierarchyReturnToParentDocument + BuilderConstants.SingleSpace + BuilderConstants.OpenBracket + parentTestDocumentName + BuilderConstants.CloseBracket;
            var childTestDocumentName = "ChildTestUXMLDocument";
            var childTestFullString = BuilderConstants.ExplorerHierarchyReturnToParentDocument + BuilderConstants.SingleSpace + BuilderConstants.OpenBracket + childTestDocumentName + BuilderConstants.CloseBracket;

            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            // Load Test UXML File
            yield return LoadTestUXMLDocument(k_ParentTestUXMLPath);
            hierarchy.elementHierarchyView.ExpandAllItems();
            Assert.AreEqual(1, builder.document.openUXMLFiles.Count);

            // Open child in isolation
            yield return OpenChildTemplateContainerAsSubDocument(menu, nameOfChildSubDocument);

            yield return UIETestHelpers.Pause(1);
            Assert.AreEqual(2, builder.document.openUXMLFiles.Count);

            // Open grand-child in-place
            yield return OpenChildTemplateContainerAsSubDocument(menu, nameofGrandChildSubDocument, true);
            Assert.AreEqual(3, builder.document.openUXMLFiles.Count);

            // Go back to root document through right click menu
            yield return ReturnToParentDocumentThroughEntryItem(menu, childTestFullString, nameofGrandChildSubDocument);
            Assert.AreEqual(2, builder.document.openUXMLFiles.Count);
            yield return ReturnToParentDocumentThroughEntryItem(menu, parentTestFullString);
            Assert.AreEqual(1, builder.document.openUXMLFiles.Count);

            // Open child in-place
            yield return OpenChildTemplateContainerAsSubDocument(menu, nameOfChildSubDocument, true);
            Assert.AreEqual(2, builder.document.openUXMLFiles.Count);

            // Open child in-isolation
            yield return OpenChildTemplateContainerAsSubDocument(menu, nameofGrandChildSubDocument);
            Assert.AreEqual(3, builder.document.openUXMLFiles.Count);
        }

        [UnityTest]
        public IEnumerator HoveringHierarchyItemHighlightsViewportElement()
        {
            const string testButtonName = "test-button";
            AddElementCodeOnly<Button>(testButtonName);

            yield return UIETestHelpers.Pause();
            var hierarchyCreatedItem = GetHierarchyExplorerItemByElementName(testButtonName);

            // Overlay starts disabled
            Assert.AreEqual(0, builder.highlightOverlayPainter.overlayCount);

            // Put mouse on top of hierarchy element
            yield return UIETestEvents.Mouse.SimulateMouseMove(builder, viewport.worldBound.center, hierarchyCreatedItem.worldBound.center);

            // Check if overlay was activated
            Assert.AreEqual(1, builder.highlightOverlayPainter.overlayCount);
        }

        [UnityTest]
        public IEnumerator DeletingHierarchyItemRemovesViewportHighlight()
        {
            const string testButtonName = "test-button";
            AddElementCodeOnly<Button>(testButtonName);

            yield return UIETestHelpers.Pause();
            var hierarchyCreatedItem = GetHierarchyExplorerItemByElementName(testButtonName);

            // Put mouse on top of hierarchy element 
            yield return UIETestEvents.Mouse.SimulateMouseMove(builder, viewport.worldBound.center, hierarchyCreatedItem.worldBound.center);

            // Check if overlay was activated
            Assert.AreEqual(1, builder.highlightOverlayPainter.overlayCount);

            // Select and delete hierarchy element
            yield return UIETestEvents.Mouse.SimulateClick(hierarchyCreatedItem);
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builder, KeyCode.Delete);

            // Check if overlay was deactivated
            Assert.AreEqual(0, builder.highlightOverlayPainter.overlayCount);
        }
    }
}
