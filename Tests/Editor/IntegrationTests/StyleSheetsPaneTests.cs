using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class StyleSheetsPaneTests : BuilderIntegrationTest
    {
        public const string TestSelectorName = ".test";
        public const string TestSelectorName2 = ".test2";

        /// <summary>
        /// Global > Can delete element via Delete key.
        /// Global > Can cut/copy/duplicate/paste element via keyboard shortcut. The copied element and its children are pasted as children of the parent of the currently selected element. If nothing is selected, they are pasted at the root.
        /// </summary>
        ///
        /// Instability failure details:
        /* SelectorCopyPasteDuplicateDelete (1.790s)
            ---
            Expected: 2
              But was:  1
            ---
            at Unity.UI.Builder.EditorTests.StyleSheetsPaneTests+<SelectorCopyPasteDuplicateDelete>d__2.MoveNext () [0x00123] in C:\work\com.unity.ui.builder\Tests\Editor\IntegrationTests\StyleSheetsPaneTests.cs:44
            at UnityEngine.TestTools.TestEnumerator+<Execute>d__5.MoveNext () [0x0004c] in C:\work\1230407\Library\PackageCache\com.unity.test-framework@1.1.13\UnityEngine.TestRunner\NUnitExtensions\Attributes\TestEnumerator.cs:31
        */
        [UnityTest, Ignore("This is unstable. I got it to fail consistently by just having a floating UI Builder window open at the same time.")]
        public IEnumerator SelectorCopyPasteDuplicateDelete()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);

            var explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(1));

            yield return UIETestEvents.Mouse.SimulateClick(explorerItems[0]);

            // Duplicate
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Duplicate);

            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(2));

            // Copy
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Copy);
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Paste);

            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(3));

            var styleSheetElement = builder.documentRootElement.parent.Q(k_TestEmptyUSSFileNameNoExt);
            Assert.That(styleSheetElement, Is.Not.Null);
            Assert.That(styleSheetElement.childCount, Is.EqualTo(3));
            Assert.That(
                styleSheetElement.GetProperty(BuilderConstants.ElementLinkedStyleSheetVEPropertyName) as StyleSheet,
                Is.EqualTo(builder.document.firstStyleSheet));

            var selectedSelectorElement = styleSheetElement[2];
            var selectedSelector = selectedSelectorElement.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            Assert.That(selectedSelector, Is.Not.Null);
            Assert.That(selection.selection.Any(), Is.True);
            Assert.That(selection.selection.First(), Is.EqualTo(selectedSelectorElement));
            Assert.That(selectedSelectorElement.GetClosestStyleSheet(), Is.EqualTo(builder.document.firstStyleSheet));

            // Delete
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builder, KeyCode.Delete);

            yield return UIETestHelpers.Pause(1);
            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// StyleSheets > With a selector selected, you can use standard short-cuts or the Edit menu to copy/paste/duplicate/delete it. You can also copy/paste the USS for the selector to/from a text file.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DeleteSelectorViaRightClickMenu()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);

            var explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(1));

            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            yield return UIETestEvents.Mouse.SimulateClick(explorerItems[0], MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var deleteMenuItem = menu.FindMenuAction("Delete");
            Assert.That(deleteMenuItem, Is.Not.Null);

            deleteMenuItem.Execute();

            yield return UIETestHelpers.Pause(1);

            var newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// In the toolbar of the StyleSheets pane there's a field that lets you create new selectors.
        /// 1. After the field is focused, the explanation text is replaced with a default `.`
        /// and the cursor is set right after the `.` to let you quickly add a class-based selector.
        /// 2. You can commit and add your selector to the *active* StyleSheet by pressing **Enter**.
        /// </summary>
        [UnityTest]
        public IEnumerator CreateSelectorFieldBehaviour()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            var createSelectorField = styleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Blur();
            Assert.That(createSelectorField.text, Is.EqualTo(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage));

            createSelectorField.visualInput.Focus();
            Assert.That(createSelectorField.text, Is.EqualTo("."));
            Assert.That(createSelectorField.cursorIndex, Is.EqualTo(1));

            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, TestSelectorName);
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builder, KeyCode.Return);
            createSelectorField.visualInput.Blur();

            var newSelector = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName);
            Assert.That(newSelector, Is.Not.Null);
        }

        /// <summary>
        ///  If the selector string contains invalid characters, an error message will display and the new selector will not be created - keeping the focus on the rename field.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectorNameValidation()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            var createSelectorField = styleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Focus();
            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, "invalid%%selector@$name");
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builder, KeyCode.Return);

            yield return UIETestHelpers.Pause(2);
            Assert.That(createSelectorField.text, Is.EqualTo(".invalid%%selector@$name"));

            // 1 because title is BuilderExplorerItem as well. So 1 means empty in this context
            Assert.That(styleSheetsPane.Query<BuilderExplorerItem>().ToList().Count, Is.EqualTo(1));

            // Test that we haven't lost field focus and can type valid name.
            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, TestSelectorName);
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builder, KeyCode.Return);

            var explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SelectorRenameWithCommandAndContextMenu()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);
            var createdSelector = GetStyleSelectorNodeWithName(TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(createdSelector);

            // Rename with command.
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Rename);
            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, TestSelectorName2);
            yield return UIETestEvents.Mouse.SimulateClick(styleSheetsPane);

            yield return UIETestHelpers.Pause(2);
            Assert.That(BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName).Count, Is.EqualTo(0));
            Assert.That(BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName2).Count, Is.EqualTo(1));

            // Test invalid selector rename.
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Rename);
            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, "invalid%%selector@$name");
            yield return UIETestEvents.Mouse.SimulateClick(styleSheetsPane);

            yield return UIETestHelpers.Pause(2);
            Assert.That(BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName2).Count, Is.EqualTo(1));

            // Try renaming with contextual menu option.
            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            createdSelector = GetStyleSelectorNodeWithName(TestSelectorName2);
            yield return UIETestEvents.Mouse.SimulateClick(createdSelector, MouseButton.RightMouse);
            var renameClick = menu.FindMenuAction("Rename");
            Assert.That(renameClick, Is.Not.Null);

            renameClick.Execute();
            yield return UIETestHelpers.Pause(1);

            // Rename back to original selector name.
            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(styleSheetsPane);

            yield return UIETestHelpers.Pause(2);
            Assert.That(BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName2).Count, Is.EqualTo(0));
            Assert.That(BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName).Count, Is.EqualTo(1));
        }

        /// <summary>
        /// In the StyleSheets pane, you can select selectors by clicking on the row or a style class pill.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectSelectorWithRowAndPillClick()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);

#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var stylesTreeView = styleSheetsPane.Q<TreeView>();
#else
            var stylesTreeView = styleSheetsPane.Q<InternalTreeView>();
#endif

            selection.ClearSelection(null);
            Assert.That(stylesTreeView.GetSelectedItem(), Is.Null);

            // Select by clicking on the row
            var createdSelector = GetStyleSelectorNodeWithName(TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(createdSelector);
            Assert.That(stylesTreeView.GetSelectedItem(), Is.Not.Null);

            // Deselect
            yield return UIETestEvents.Mouse.SimulateClick(styleSheetsPane);
            Assert.That(stylesTreeView.GetSelectedItem(), Is.Null);

#if !UNITY_2019_4
            // Select by clicking on the style class pill
            yield return UIETestEvents.Mouse.SimulateClick(createdSelector.Q<Label>());
            Assert.That(stylesTreeView.GetSelectedItem(), Is.Not.Null);
#endif
        }

        /// <summary>
        /// Can drag a style class pill from the StyleSheets pane onto an element in the Viewport to add the class.
        /// Selectors get draggable style class pills for each selector part that is a style class name.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DragStylePillToViewport()
        {
            AddElementCodeOnly<TextField>();

            // Ensure we can add selectors.
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName + " " + TestSelectorName2);
            var createdSelector = GetStyleSelectorNodeWithName(TestSelectorName);

            // Now it's safe to get a reference to an element in the canvas.
            var documentElement = GetFirstDocumentElement();

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                createdSelector.Q<Label>().worldBound.center,
                documentElement.worldBound.center);

            var currentClassCount = documentElement.classList.Count;
            Assert.That(documentElement.classList, Contains.Item(TestSelectorName.TrimStart('.')));

            var secondClassNameLabel = BuilderTestsHelper.GetLabelWithName(createdSelector, TestSelectorName2);
            yield return UIETestHelpers.Pause(100);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                secondClassNameLabel.worldBound.center,
                documentElement.worldBound.center);

            Assert.That(documentElement.classList.Count, Is.EqualTo(currentClassCount + 1));
            Assert.That(documentElement.classList, Contains.Item(TestSelectorName2.TrimStart('.')));
        }

        /// <summary>
        /// Can drag a style class pill from the StyleSheets pane onto an element in the Hierarchy to add the class.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DragStylePillToHierarchy()
        {
            AddElementCodeOnly();

            // Ensure we can add selectors.
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);
            var createdSelector = GetStyleSelectorNodeWithName(TestSelectorName);

            var hierarchyCreatedItem = GetFirstExplorerVisualElementNode(nameof(VisualElement));

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                createdSelector.Q<Label>().worldBound.center,
                hierarchyCreatedItem.worldBound.center);

            var documentElement =
                (VisualElement) hierarchyCreatedItem.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName);

            Assert.That(documentElement.classList.Count, Is.EqualTo(1));
            Assert.That(documentElement.classList[0], Is.EqualTo(TestSelectorName.TrimStart('.')));
        }

        /// <summary>
        /// Dragging a style class onto an element inside a template instance or C# type in the Viewport adds it to the parent instance or C# element.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DragStylePillOntoTemplateElementInViewport()
        {
            AddElementCodeOnly<TextField>();

            // Ensure we can add selectors.
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);
            var createdSelector = GetStyleSelectorNodeWithName(TestSelectorName);

            // Now it's safe to get a reference to an element in the canvas.
            var documentElement = GetFirstDocumentElement();

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                createdSelector.Q<Label>().worldBound.center,
                documentElement.worldBound.center);

            yield return UIETestHelpers.Pause(1);
            Assert.That(documentElement.classList, Contains.Item(TestSelectorName.TrimStart('.')));
        }

        /// <summary>
        /// Dragging a style class onto an element inside a template instance or C# type in the Hierarchy does nothing.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DragStylePillOntoTemplateElementInHierarchy()
        {
            AddElementCodeOnly<TextField>();

            // Ensure we can add selectors.
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);
            var createdSelector = GetStyleSelectorNodeWithName(TestSelectorName);

            yield return UIETestHelpers.Pause(1);
#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var hierarchyTreeView = hierarchy.Q<TreeView>();
#else
            var hierarchyTreeView = hierarchy.Q<InternalTreeView>();
#endif
            hierarchyTreeView.ExpandItem(hierarchyTreeView.items.ToList()[1].id);

            var textFieldLabel = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, nameof(Label)).Q<Label>();

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                createdSelector.Q<Label>().worldBound.center,
                textFieldLabel.worldBound.center);

            var documentElement = GetFirstDocumentElement();
            Assert.That(documentElement.classList, Is.Not.Contain(TestSelectorName.TrimStart('.')));
        }

        /// <summary>
        ///  While the text field is selected, you should see a large tooltip displaying the selector cheatsheet.
        /// </summary>
        [Test]
        public void SelectorCheatsheetTooltip()
        {
            var builderTooltipPreview = builder.rootVisualElement.Q<BuilderTooltipPreview>("stylesheets-pane-tooltip-preview");
            var builderTooltipPreviewEnabler =
                builderTooltipPreview.Q<VisualElement>(BuilderTooltipPreview.s_EnabledElementName);

            var createSelectorField = styleSheetsPane.Q<TextField>(className: BuilderNewSelectorField.s_TextFieldUssClassName);

#if UNITY_2019_4
            // Everything StyleSheet is disabled now if there are no elements to contain the <Style> tag.
            Assert.That(createSelectorField.enabledInHierarchy, Is.False);
#else
            Assert.That(createSelectorField.enabledInHierarchy, Is.True);
#endif
            AddElementCodeOnly("TestElement");
            Assert.That(createSelectorField.enabledInHierarchy, Is.True);

            createSelectorField.visualInput.Focus();
            Assert.That(builderTooltipPreviewEnabler, Style.Display(DisplayStyle.Flex));

            createSelectorField.visualInput.Blur();
            Assert.That(builderTooltipPreviewEnabler, Style.Display(DisplayStyle.None));
        }

        // TODO: Convert to block-comment.
        readonly string m_ExpectedSelectorString
            = WebUtility.UrlDecode($"{TestSelectorName}%20%7B%0A%20%20%20%20display:%20none;%0A%20%20%20%20visibility:%20hidden;%0A%7D%0A");

        /// <summary>
        ///  With a selector selected, you can use standard short-cuts or the Edit menu to copy/paste/duplicate/delete it. You can also copy/paste the USS for the selector to/from a text file.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator SelectorToAndFromUSSConversion()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            // Create and new selector and select
            yield return AddSelector(TestSelectorName);
            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            // Set style
            var displayFoldout = inspector.Query<PersistedFoldout>().Where(f => f.text.Equals("Display")).ToList().First();
            displayFoldout.value = true;

            var displayStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Display")).ToList().First();
            yield return UIETestEvents.Mouse.SimulateClick(displayStrip.Q<Button>("none"));

            var visibilityStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Visibility")).ToList().First();
            yield return UIETestEvents.Mouse.SimulateClick(visibilityStrip.Q<Button>("hidden"));
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var newlineFixedExpectedUSS = m_ExpectedSelectorString;
            if (BuilderConstants.NewlineChar != BuilderConstants.newlineCharFromEditorSettings)
                newlineFixedExpectedUSS = newlineFixedExpectedUSS.Replace(
                    BuilderConstants.NewlineChar,
                    BuilderConstants.newlineCharFromEditorSettings);

            // Copy to USS
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Copy);
            Assert.That(BuilderEditorUtility.systemCopyBuffer, Is.EqualTo(newlineFixedExpectedUSS));

            // Paste from USS
            ForceNewDocument();
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();
            BuilderEditorUtility.systemCopyBuffer = string.Empty;
            yield return UIETestEvents.Mouse.SimulateClick(styleSheetsPane);
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Paste);
            var explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(0));

            BuilderEditorUtility.systemCopyBuffer = newlineFixedExpectedUSS;
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Paste);
            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(1));

            // Foldout out state should be persisted, so we assume it is open already.
            displayFoldout = inspector.Query<PersistedFoldout>().Where(f => f.text.Equals("Display")).ToList().First();
            displayStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Display")).ToList().First();
            Assert.True(displayStrip.Q<Button>("none").pseudoStates.HasFlag(PseudoStates.Checked));

            visibilityStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Visibility")).ToList().First();
            Assert.True(visibilityStrip.Q<Button>("hidden").pseudoStates.HasFlag(PseudoStates.Checked));
        }

        [UnityTest]
        public IEnumerator UxmlCopyBufferCannotBePastedInStylesheetPane()
        {
            // Load Test UXML File
            yield return LoadTestUXMLDocument(k_TestNoUSSDocumentUXMLFilePath);

            yield return null;

            var label = builder.documentRootElement.Q("no-uss-label");
            yield return UIETestEvents.Mouse.SimulateClick(label);

            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Copy);
            Assert.That(BuilderEditorUtility.IsUxml(BuilderEditorUtility.systemCopyBuffer));

            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;

            yield return UIETestEvents.Mouse.SimulateClick(styleSheetsPane, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var pasteMenuItem = menu.FindMenuAction("Paste");
            Assert.AreEqual(DropdownMenuAction.Status.Disabled, pasteMenuItem.status);
        }

        /// <summary>
        ///  Selecting an element or a the main document (VisualTreeAsset) should deselect any selected tree items in the StyleSheets pane.
        /// </summary>
        [UnityTest]
        public IEnumerator StyleSheetsItemsDeselect()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var styleSheetsTreeView = styleSheetsPane.Q<TreeView>();
#else
            var styleSheetsTreeView = styleSheetsPane.Q<InternalTreeView>();
#endif
            Assert.That(styleSheetsTreeView.GetSelectedItem(), Is.Null);

            // Create and new selector and select
            yield return AddSelector(TestSelectorName);
            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            Assert.That(styleSheetsTreeView.GetSelectedItem(), Is.Not.Null);

            AddElementCodeOnly();
            var documentElement = GetFirstDocumentElement();
            yield return UIETestEvents.Mouse.SimulateClick(documentElement);
            Assert.That(styleSheetsTreeView.GetSelectedItem(), Is.Null);
        }

#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator ParentUSSFilesAppearWithinSubdocument()
        {
            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            // Load Test UXML File
            yield return LoadTestUXMLDocument(k_ParentTestUXMLPath);

            // Open child as subdocument
            string nameOfChildSubDocument = "#ChildTestUXMLDocument";
            var childInHierarchy = BuilderTestsHelper.GetExplorerItemsWithName(hierarchy, nameOfChildSubDocument);

            // Simulate right click on child TemplateContainer
            yield return UIETestEvents.Mouse.SimulateClick(childInHierarchy[0], MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var subdocumentClick = menu.FindMenuAction(BuilderConstants.ExplorerHierarchyPaneOpenSubDocument);
            Assert.That(subdocumentClick, Is.Not.Null);

            subdocumentClick.Execute();
            yield return UIETestHelpers.Pause(1);

            styleSheetsPane.elementHierarchyView.ExpandRootItems();
            var selectorFromActive = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, "#builder-test")[0]; // this one belongs to current

            yield return UIETestEvents.Mouse.SimulateClick(selectorFromActive);

            var arbitraryStyleRow = inspector.Q<PersistedFoldout>("inspector-style-section-foldout-display").Q<BuilderStyleRow>();
            var isActive = arbitraryStyleRow.enabledInHierarchy == true;

            Assert.AreEqual(true, isActive);

            var selectorFromParent = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, ".unity-label")[1]; // this one belongs to parent

            Assert.AreNotEqual(selectorFromActive, selectorFromParent);

            yield return UIETestEvents.Mouse.SimulateClick(selectorFromParent);

            arbitraryStyleRow = inspector.Q<PersistedFoldout>("inspector-style-section-foldout-display").Q<BuilderStyleRow>();
            isActive = arbitraryStyleRow.enabledInHierarchy == true;

            Assert.AreEqual(false, isActive);
        }
    }
}
