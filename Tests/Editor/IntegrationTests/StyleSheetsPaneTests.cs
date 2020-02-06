using System;
using System.Collections;
using System.Linq;
using System.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    internal class StyleSheetsPaneTests : BuilderIntegrationTest
    {
        public const string k_TestSelectorName = ".test";
        public const string k_TestSelectorName2 = ".test2";

        /// <summary>
        /// Global > Can delete element via Delete key.
        /// Global > Can cut/copy/duplicate/paste element via keyboard shortcut. The copied element and its children are pasted as children of the parent of the currently selected element. If nothing is selected, they are pasted at the root.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectorCopyPasteDuplicateDelete()
        {
            yield return AddSelector(k_TestSelectorName);

            var explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(1));

            yield return UIETestEvents.Mouse.SimulateClick(explorerItems[0]);

            // Duplicate
            UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Duplicate);

            yield return UIETestHelpers.Pause(1);
            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(2));

            // Copy
            UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Copy);
            UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Paste);

            yield return UIETestHelpers.Pause(1);
            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(3));

            // Delete
            UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Delete);

            yield return UIETestHelpers.Pause(1);
            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// In the toolbar of the StyleSheets pane there's a field that lets you create new selectors.
        /// 1. After the field is focused, the explanation text is replaced with a default `.`
        /// and the cursor is set right after the `.` to let you quickly add a class-based selector.
        /// 2. To commit and add your new selector, you can click on the **Add** button or press **Enter**.
        /// </summary>
        [UnityTest]
        public IEnumerator CreateSelectorFieldBehaviour()
        {
            var createSelectorField = StyleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Blur();
            Assert.That(createSelectorField.text, Is.EqualTo(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage));

            createSelectorField.visualInput.Focus();
            Assert.That(createSelectorField.text, Is.EqualTo("."));
            Assert.That(createSelectorField.cursorIndex, Is.EqualTo(1));

            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, k_TestSelectorName);
            UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Return);
            createSelectorField.visualInput.Blur();

            var newSelector = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_TestSelectorName);
            Assert.That(newSelector, Is.Not.Null);

            createSelectorField.visualInput.Focus();
            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, k_TestSelectorName2);

            var addButton = StyleSheetsPane.Query<Button>().Where(b => b.text.Equals("Add")).First();
            yield return UIETestEvents.Mouse.SimulateClick(addButton);
            newSelector = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_TestSelectorName2);
            Assert.That(newSelector, Is.Not.Null);
        }

        /// <summary>
        ///  If the selector string contains invalid characters, an error message will display and the new selector will not be created - keeping the focus on the rename field.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectorNameValidation()
        {
            var createSelectorField = StyleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Focus();
            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, "invalid%%selector@$name");
            UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Return);

            yield return UIETestHelpers.Pause(2);
            Assert.That(createSelectorField.text, Is.EqualTo(".invalid%%selector@$name"));

            // 1 because title is BuilderExplorerItem as well. So 1 means empty in this context
            Assert.That(StyleSheetsPane.Query<BuilderExplorerItem>().ToList().Count, Is.EqualTo(1));

            // Test that we haven't lost filed focus and can type valid name.
            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, k_TestSelectorName);
            UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Return);

            var explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// In the StyleSheets pane, you can select selectors by clicking on the row or a style class pill.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectSelectorWithRowAndPillClick()
        {
            yield return AddSelector(k_TestSelectorName);
            var stylesTreeView = StyleSheetsPane.Q<TreeView>();

            Assert.That(stylesTreeView.GetSelectedItem(), Is.Null);

            //Select by clicking on the row
            var createdSelector = GetStyleSelectorNodeWithName(k_TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(createdSelector);
            Assert.That(stylesTreeView.GetSelectedItem(), Is.Not.Null);

            //Deselect
            yield return UIETestEvents.Mouse.SimulateClick(StyleSheetsPane);
            Assert.That(stylesTreeView.GetSelectedItem(), Is.Null);

            //Select by clicking on the style class pill
            yield return UIETestEvents.Mouse.SimulateClick(createdSelector.Q<Label>());
            Assert.That(stylesTreeView.GetSelectedItem(), Is.Not.Null);
        }

        /// <summary>
        /// Can drag a style class pill from the StyleSheets pane onto an element in the Viewport to add the class.
        /// Selectors get draggable style class pills for each selector part that is a style class name.
        /// </summary>
        [UnityTest]
        public IEnumerator DragStylePillToViewport()
        {
            yield return AddVisualElement();
            var documentElement = GetFirstViewportElement();

            yield return AddSelector(k_TestSelectorName + " " + k_TestSelectorName2);
            var createdSelector = GetStyleSelectorNodeWithName(k_TestSelectorName);

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                createdSelector.Q<Label>().worldBound.center,
                documentElement.worldBound.center);

            Assert.That(documentElement.classList.Count, Is.EqualTo(1));
            Assert.That(documentElement.classList[0], Is.EqualTo(k_TestSelectorName.TrimStart('.')));

            var secondClassNameLabel = BuilderTestsHelper.GetLabelWithName(createdSelector, k_TestSelectorName2);
            yield return UIETestHelpers.Pause(100);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                secondClassNameLabel.worldBound.center,
                documentElement.worldBound.center);

            Assert.That(documentElement.classList.Count, Is.EqualTo(2));
            Assert.That(documentElement.classList, Contains.Item(k_TestSelectorName2.TrimStart('.')));
        }

        /// <summary>
        /// Can drag a style class pill from the StyleSheets pane onto an element in the Hierarchy to add the class.
        /// </summary>
        [UnityTest]
        public IEnumerator DragStylePillToHierarchy()
        {
            yield return AddVisualElement();
            var hierarchyCreatedItem = GetFirstExplorerVisualElementNode(nameof(VisualElement));

            yield return AddSelector(k_TestSelectorName);
            var createdSelector = GetStyleSelectorNodeWithName(k_TestSelectorName);

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                createdSelector.Q<Label>().worldBound.center,
                hierarchyCreatedItem.worldBound.center);

            var documentElement =
                (VisualElement) hierarchyCreatedItem.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName);

            Assert.That(documentElement.classList.Count, Is.EqualTo(1));
            Assert.That(documentElement.classList[0], Is.EqualTo(k_TestSelectorName.TrimStart('.')));
        }

        /// <summary>
        /// Dragging a style class onto an element inside a template instance or C# type in the Viewport adds it to the parent instance or C# element.
        /// </summary>
        [UnityTest]
        public IEnumerator DragStylePillOntoTemplateElementInViewport()
        {
            yield return AddTextFieldElement();
            var documentElement = GetFirstViewportElement();

            yield return AddSelector(k_TestSelectorName);
            var createdSelector = GetStyleSelectorNodeWithName(k_TestSelectorName);

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                createdSelector.Q<Label>().worldBound.center,
                documentElement.worldBound.center);

            Assert.That(documentElement.classList, Contains.Item(k_TestSelectorName.TrimStart('.')));
        }

        /// <summary>
        /// Dragging a style class onto an element inside a template instance or C# type in the Hierarchy does nothing.
        /// </summary>
        [UnityTest]
        public IEnumerator DragStylePillOntoTemplateElementInHierarchy()
        {
            yield return AddTextFieldElement();
            var documentElement = GetFirstViewportElement();

            yield return AddSelector(k_TestSelectorName);
            var createdSelector = GetStyleSelectorNodeWithName(k_TestSelectorName);

            yield return UIETestHelpers.Pause(1);
            var hierarchyTreeView = HierarchyPane.Q<TreeView>();
            hierarchyTreeView.ExpandItem(hierarchyTreeView.items.ToList()[1].id);

            var textFieldLabel = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(Label)).Q<Label>();

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                createdSelector.Q<Label>().worldBound.center,
                  textFieldLabel.worldBound.center);

            Assert.That(documentElement.classList,  Is.Not.Contain(k_TestSelectorName.TrimStart('.')));
        }

        /// <summary>
        ///  While the text field is selected, you should see a large tooltip displaying the selector cheatsheet.
        /// </summary>
        [Test]
        public void SelectorCheatsheetTooltip()
        {
            var builderTooltipPreview = BuilderWindow.rootVisualElement.Q<BuilderTooltipPreview>("stylesheets-pane-tooltip-preview");
            var builderTooltipPreviewEnabler =
                builderTooltipPreview.Q<VisualElement>(BuilderTooltipPreview.s_EnabledElementName);

            var createSelectorField = StyleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Focus();
            Assert.That(builderTooltipPreviewEnabler, Style.Display(DisplayStyle.Flex));

            createSelectorField.visualInput.Blur();
            Assert.That(builderTooltipPreviewEnabler, Style.Display(DisplayStyle.None));
        }

        readonly string m_ExpectedSelectorString
            = WebUtility.UrlDecode($"{k_TestSelectorName}%20%7B%0A%20%20%20%20display:%20flex;%0A%20%20%20%20visibility:%20hidden;%0A%7D%0A");

        /// <summary>
        ///  With a selector selected, you can use standard short-cuts or the Edit menu to copy/paste/duplicate/delete it. You can also copy/paste the USS for the selector to/from a text file.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectorToAndFromUSSConversion()
        {
            // Create and new selector and select
            yield return AddSelector(k_TestSelectorName);
            var selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            // Set style
            var displayFoldout = InspectorPane.Query<PersistedFoldout>().Where(f => f.text.Equals("Display")).First();
            displayFoldout.value = true;

            var displayStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Display")).First();
            yield return UIETestEvents.Mouse.SimulateClick(displayStrip.Q<Button>("flex"));

            var visibilityStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Visibility")).First();
            yield return UIETestEvents.Mouse.SimulateClick(visibilityStrip.Q<Button>("hidden"));

            // Copy to USS
            yield return UIETestEvents.Mouse.SimulateClick(selector);
            UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Copy);
            Assert.That(BuilderEditorUtility.SystemCopyBuffer, Is.EqualTo(m_ExpectedSelectorString));

            // Paste from USS
            ForceNewDocument();
            BuilderEditorUtility.SystemCopyBuffer = string.Empty;
            yield return UIETestEvents.Mouse.SimulateClick(StyleSheetsPane);
            UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Paste);
            var explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(0));

            BuilderEditorUtility.SystemCopyBuffer = m_ExpectedSelectorString;
            UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Paste);
            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(1));

            // Foldout out state should be persisted, so we assume it is open already.
            displayFoldout = InspectorPane.Query<PersistedFoldout>().Where(f => f.text.Equals("Display")).First();
            displayStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Display")).First();
            Assert.True(displayStrip.Q<Button>("flex").pseudoStates.HasFlag(PseudoStates.Checked));

            visibilityStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Visibility")).First();
            Assert.True(visibilityStrip.Q<Button>("hidden").pseudoStates.HasFlag(PseudoStates.Checked));
        }

        /// <summary>
        ///  Selecting an element or a the main document (VisualTreeAsset) should deselect any selected tree items in the StyleSheets pane.
        /// </summary>
        [UnityTest]
        public IEnumerator StyleSheetsItemsDeselect()
        {
            var styleSheetsTreeView = StyleSheetsPane.Q<TreeView>();
            Assert.That(styleSheetsTreeView.GetSelectedItem(), Is.Null);

            // Create and new selector and select
            yield return AddSelector(k_TestSelectorName);
            var selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(selector);
            Assert.That(styleSheetsTreeView.GetSelectedItem(), Is.Not.Null);

            yield return AddVisualElement();
            var documentElement = GetFirstViewportElement();
            yield return UIETestEvents.Mouse.SimulateClick(documentElement);
            Assert.That(styleSheetsTreeView.GetSelectedItem(), Is.Null);
        }
    }
}