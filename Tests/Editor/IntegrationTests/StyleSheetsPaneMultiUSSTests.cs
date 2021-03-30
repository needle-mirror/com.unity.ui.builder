using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class StyleSheetsPaneMultiUSSTests : BuilderIntegrationTest
    {
        const string k_ColorsTestUSSFileNameNoExt = "ColorsTestStyleSheet";
        const string k_LayoutTestUSSFileNameNoExt = "LayoutTestStyleSheet";

        const string k_ColorsTestUSSFileName = k_ColorsTestUSSFileNameNoExt + ".uss";
        const string k_LayoutTestUSSFileName = k_LayoutTestUSSFileNameNoExt + ".uss";

        const string k_ColorsTestUSSPath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_ColorsTestUSSFileName;
        const string k_LayoutTestUSSPath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_LayoutTestUSSFileName;

        protected override IEnumerator TearDown()
        {
            BuilderStyleSheetsUtilities.RestoreTestCallbacks();

            yield return base.TearDown();
            AssetDatabase.DeleteAsset(k_TestUSSFilePath);
        }

        StyleSheet GetStyleSheetFromExplorerItem(VisualElement explorerItem, string ussPath)
        {
            Assert.That(explorerItem, Is.Not.Null);
            var documentElement = explorerItem.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
            Assert.That(documentElement, Is.Not.Null);
            var styleSheet = documentElement.GetStyleSheet();
            Assert.That(styleSheet, Is.Not.Null);
            var styleSheetPath = AssetDatabase.GetAssetPath(styleSheet);
            Assert.That(styleSheetPath, Is.EqualTo(ussPath));

            return styleSheet;
        }

        /// <summary>
        /// If there is no USS in document, the Save Dialog Option to create a new USS file will be prompted and selector will be added to the newly created and added USS file.
        /// </summary>
        [UnityTest]
        public IEnumerator NewSelectorWithNoUSSCreatesNewUSS()
        {
            AddElementCodeOnly("TestElement");

            var createSelectorField = styleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Blur();
            Assert.That(createSelectorField.text, Is.EqualTo(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage));

            createSelectorField.visualInput.Focus();
            Assert.That(createSelectorField.text, Is.EqualTo("."));
            Assert.That(createSelectorField.cursorIndex, Is.EqualTo(1));

            bool hasSaveDialogBeenOpened = false;
            BuilderStyleSheetsUtilities.s_SaveFileDialogCallback = () =>
            {
                hasSaveDialogBeenOpened = true;
                return k_TestUSSFilePath;
            };

            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, ".new-selector");
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builder, KeyCode.Return);
            Assert.That(hasSaveDialogBeenOpened, Is.True);

            yield return UIETestHelpers.Pause(1);

            var unityButtonSelectors = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, ".new-selector");
            Assert.That(unityButtonSelectors.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Right-clicking anywhere in the TreeView should display the standard copy/paste/duplicate/delete menu with the additional options to:
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator RightClickingInStyleSheetsPaneOpensMenu()
        {
            AddElementCodeOnly("TestElement");

            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            yield return UIETestEvents.Mouse.SimulateClick(styleSheetsPane, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var newUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneCreateNewUSSMenu);
            var existingUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneAddExistingUSSMenu);
            var removeUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneRemoveUSSMenu);

            Assert.That(newUSS, Is.Not.Null);
            Assert.That(existingUSS, Is.Not.Null);
            Assert.That(removeUSS, Is.Not.Null);

            Assert.That(newUSS.status, Is.EqualTo(DropdownMenuAction.Status.Normal));
            Assert.That(existingUSS.status, Is.EqualTo(DropdownMenuAction.Status.Normal));
            Assert.That(removeUSS.status, Is.EqualTo(DropdownMenuAction.Status.Disabled));
        }

        [UnityTest]
        public IEnumerator CreateNewUSSViaPlusMenu()
        {
            AddElementCodeOnly("TestElement");

            var addMenu = styleSheetsPane.Q<ToolbarMenu>("add-uss-menu");
            var addMenuItems = addMenu.menu.MenuItems();
            Assert.AreEqual(addMenuItems.Count, 2);
            var actionMenuItem = addMenuItems[0] as DropdownMenuAction;
            Assert.AreEqual(actionMenuItem.name, BuilderConstants.ExplorerStyleSheetsPaneCreateNewUSSMenu);

            bool hasSaveDialogBeenOpened = false;
            BuilderStyleSheetsUtilities.s_SaveFileDialogCallback = () =>
            {
                hasSaveDialogBeenOpened = true;
                return k_TestUSSFilePath;
            };

            actionMenuItem.Execute();
            Assert.That(hasSaveDialogBeenOpened, Is.True);

            yield return UIETestHelpers.Pause(1);

            var newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, k_TestUSSFileName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AddExistingUSSViaPlusMenu()
        {
            AddElementCodeOnly("TestElement");

            var addMenu = styleSheetsPane.Q<ToolbarMenu>("add-uss-menu");
            var addMenuItems = addMenu.menu.MenuItems();
            Assert.AreEqual(addMenuItems.Count, 2);
            var actionMenuItem = addMenuItems[1] as DropdownMenuAction;
            Assert.AreEqual(actionMenuItem.name, BuilderConstants.ExplorerStyleSheetsPaneAddExistingUSSMenu);

            bool hasOpenDialogBeenOpened = false;
            BuilderStyleSheetsUtilities.s_OpenFileDialogCallback = () =>
            {
                hasOpenDialogBeenOpened = true;
                return k_ColorsTestUSSPath;
            };

            actionMenuItem.Execute();
            Assert.That(hasOpenDialogBeenOpened, Is.True);

            yield return UIETestHelpers.Pause(1);

            var newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, k_ColorsTestUSSFileName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// **Create New USS** - this will open a Save File Dialog allowing you to create a new USS Asset in your project.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator CreateNewUSSViaRightClickMenu()
        {
            AddElementCodeOnly("TestElement");

            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            yield return UIETestEvents.Mouse.SimulateClick(styleSheetsPane, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var newUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneCreateNewUSSMenu);
            Assert.That(newUSS, Is.Not.Null);

            bool hasSaveDialogBeenOpened = false;
            BuilderStyleSheetsUtilities.s_SaveFileDialogCallback = () =>
            {
                hasSaveDialogBeenOpened = true;
                return k_TestUSSFilePath;
            };

            newUSS.Execute();
            Assert.That(hasSaveDialogBeenOpened, Is.True);

            yield return UIETestHelpers.Pause(1);

            var newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, k_TestUSSFileName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// **Add Existing USS** - this will open the Open File Dialog allowing you to add an existing USS Asset to the UXML document.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator AddExistingUSSViaRightClickMenu()
        {
            AddElementCodeOnly("TestElement");

            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            yield return UIETestEvents.Mouse.SimulateClick(styleSheetsPane, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);
            var existingUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneAddExistingUSSMenu);
            Assert.That(existingUSS, Is.Not.Null);

            bool hasOpenDialogBeenOpened = false;
            BuilderStyleSheetsUtilities.s_OpenFileDialogCallback = () =>
            {
                hasOpenDialogBeenOpened = true;
                return k_ColorsTestUSSPath;
            };

            existingUSS.Execute();
            Assert.That(hasOpenDialogBeenOpened, Is.True);

            yield return UIETestHelpers.Pause(1);

            var newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, k_ColorsTestUSSFileName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// **Remove USS** (only enabled if right-clicking on a StyleSheet) - this will remove the StyleSheet from the UXML document.
        /// This should prompt to save unsaved changes.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator RemoveUSSViaRightClickMenu()
        {
            yield return CodeOnlyAddUSSToDocument(k_ColorsTestUSSPath);

            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            var newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, k_ColorsTestUSSFileName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(1));

            yield return UIETestEvents.Mouse.SimulateClick(newUSSExplorerItems[0], MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);
            var removeUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneRemoveUSSMenu);
            Assert.That(removeUSS, Is.Not.Null);
            Assert.That(removeUSS.status, Is.EqualTo(DropdownMenuAction.Status.Normal));

            bool checkedForUnsavedChanges = false;
            BuilderStyleSheetsUtilities.s_CheckForUnsavedChanges = BuilderPaneWindow => checkedForUnsavedChanges = true;
            removeUSS.Execute();
            Assert.That(checkedForUnsavedChanges, Is.True);

            yield return UIETestHelpers.Pause(1);

            newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, k_ColorsTestUSSFileName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Can right-click on USS in StyleSheets and select "Set as Active" USS to change the active StyleSheet.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator SetAsActiveUSSChangesActiveUSS()
        {
            // Active StyleSheet is null when no USS are added.
            Assert.That(builder.document.firstStyleSheet, Is.Null);
            Assert.That(builder.document.activeStyleSheet, Is.Null);

            yield return CodeOnlyAddUSSToDocument(k_ColorsTestUSSPath);
            yield return CodeOnlyAddUSSToDocument(k_LayoutTestUSSPath);

            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            // First StyleSheet should be active by default.
            var colorsExplorerItem = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_ColorsTestUSSFileName);
            Assert.That(colorsExplorerItem, Is.Not.Null);
            var colorStyleSheet = GetStyleSheetFromExplorerItem(colorsExplorerItem, k_ColorsTestUSSPath);
            Assert.That(builder.document.firstStyleSheet, Is.EqualTo(colorStyleSheet));
            Assert.That(builder.document.activeStyleSheet, Is.EqualTo(colorStyleSheet));

            // Activate second StyleSheet.
            var layoutExplorerItem = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_LayoutTestUSSFileName);
            Assert.That(layoutExplorerItem, Is.Not.Null);
            var layoutStyleSheet = GetStyleSheetFromExplorerItem(layoutExplorerItem, k_LayoutTestUSSPath);
            yield return UIETestEvents.Mouse.SimulateClick(layoutExplorerItem, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);
            var activateUSSMenuEntry = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneSetActiveUSS);
            Assert.That(activateUSSMenuEntry, Is.Not.Null);
            activateUSSMenuEntry.Execute();
            Assert.That(builder.document.firstStyleSheet, Is.EqualTo(colorStyleSheet));
            Assert.That(builder.document.activeStyleSheet, Is.EqualTo(layoutStyleSheet));

            // Check sub-title.
            Assert.AreEqual(layoutStyleSheet.name + BuilderConstants.UssExtension, styleSheetsPane.pane.subTitle);

            // Re-activate first StyleSheet.
            colorsExplorerItem = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_ColorsTestUSSFileName);
            Assert.That(colorsExplorerItem, Is.Not.Null);
            yield return UIETestEvents.Mouse.SimulateClick(colorsExplorerItem, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);
            activateUSSMenuEntry = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneSetActiveUSS);
            Assert.That(activateUSSMenuEntry, Is.Not.Null);
            activateUSSMenuEntry.Execute();
            Assert.That(builder.document.activeStyleSheet, Is.EqualTo(colorStyleSheet));
        }

        /// <summary>
        /// When pasting a selector in the StyleSheets pane, it will be added to the *active* StyleSheet.
        /// </summary>
        [UnityTest]
        public IEnumerator PastingAddsSelectorToActiveStyleSheet()
        {
            yield return CodeOnlyAddUSSToDocument(k_ColorsTestUSSPath);
            yield return CodeOnlyAddUSSToDocument(k_LayoutTestUSSPath);

            // Copy Selector.
            var unityButtonSelectors = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, ".unity-button");
            yield return UIETestEvents.Mouse.SimulateClick(unityButtonSelectors[0]);
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Copy);

            // Activate the second StyleSheet.
            var layoutExplorerItem = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_LayoutTestUSSFileName);
            Assert.That(layoutExplorerItem, Is.Not.Null);
            var layoutStyleSheet = GetStyleSheetFromExplorerItem(layoutExplorerItem, k_LayoutTestUSSPath);
            var previousNumberOfSelectors = layoutStyleSheet.complexSelectors.Length;
            BuilderStyleSheetsUtilities.SetActiveUSS(selection, styleSheetsPane.paneWindow, layoutStyleSheet);
            Assert.That(builder.document.activeStyleSheet, Is.EqualTo(layoutStyleSheet));

            // Paste Selector.
            yield return UIETestEvents.ExecuteCommand(builder, UIETestEvents.Command.Paste);
            Assert.That(layoutStyleSheet.complexSelectors.Length, Is.EqualTo(previousNumberOfSelectors + 2)); // 2 for the extra fake rule added to the stylesheet for the pasted selector. see BuilderSharedStyles.CreateNewSelectorElement
        }

#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DragDropToReorderSelectors()
        {
#if !UNITY_2019_4
            int selectionCount = 2;
#else
            int selectionCount = 1;
#endif

            yield return CodeOnlyAddUSSToDocument(k_ColorsTestUSSPath);
            yield return CodeOnlyAddUSSToDocument(k_LayoutTestUSSPath);

            var colorsUSS = builder.document.activeOpenUXMLFile.openUSSFiles[0].styleSheet;

            Assert.AreEqual(".unity-button", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[0]));
            Assert.AreEqual(".unity-label", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[1]));
            Assert.AreEqual("#builder-test", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[2]));

            var unityButtonSelectorItem = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, ".unity-button")[0];
            yield return UIETestEvents.Mouse.SimulateClick(unityButtonSelectorItem);

#if !UNITY_2019_4
            var unityLabelSelectorItem = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, ".unity-label")[0];
            yield return UIETestEvents.Mouse.SimulateClick(unityLabelSelectorItem, MouseButton.LeftMouse, EventModifiers.Shift);
#endif

            Assert.AreEqual(selectionCount, selection.selectionCount);

            var builderTestSelectorItem = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, "#builder-test")[0];
            var reorderZoneBelow = builderTestSelectorItem.Q("reorder-zone-below");
            Assert.NotNull(reorderZoneBelow);

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                unityButtonSelectorItem.worldBound.center,
                reorderZoneBelow.worldBound.center);

#if !UNITY_2019_4
            Assert.AreEqual("#builder-test", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[0]));
            Assert.AreEqual(".unity-button", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[1]));
            Assert.AreEqual(".unity-label", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[2]));
#else
            Assert.AreEqual(".unity-label", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[0]));
            Assert.AreEqual("#builder-test", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[1]));
            Assert.AreEqual(".unity-button", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[2]));
#endif

            Assert.AreEqual(selectionCount, selection.selectionCount);
        }

#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DragToReparentToMoveSelectorsBetweenStyleSheets()
        {
#if !UNITY_2019_4
            int selectionCount = 2;
#else
            int selectionCount = 1;
#endif

            yield return CodeOnlyAddUSSToDocument(k_ColorsTestUSSPath);
            yield return CodeOnlyAddUSSToDocument(k_LayoutTestUSSPath);

            var colorsUSS = builder.document.activeOpenUXMLFile.openUSSFiles[0].styleSheet;
            var layoutUSS = builder.document.activeOpenUXMLFile.openUSSFiles[1].styleSheet;

            Assert.AreEqual(".unity-button", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[0]));
            Assert.AreEqual(".unity-label", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[1]));
            Assert.AreEqual("#builder-test", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[2]));

            var unityButtonSelectorItem = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, ".unity-button")[0];
            yield return UIETestEvents.Mouse.SimulateClick(unityButtonSelectorItem);

#if !UNITY_2019_4
            var unityLabelSelectorItem = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, ".unity-label")[0];
            yield return UIETestEvents.Mouse.SimulateClick(unityLabelSelectorItem, MouseButton.LeftMouse, EventModifiers.Shift);
#endif

            Assert.AreEqual(selectionCount, selection.selectionCount);

            var builderTestSelectorItem = BuilderTestsHelper.GetExplorerItemsWithName(styleSheetsPane, "#builder-test")[1];
            var reorderZoneAbove = builderTestSelectorItem.Q("reorder-zone-above");
            Assert.NotNull(reorderZoneAbove);

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                unityButtonSelectorItem.worldBound.center,
                reorderZoneAbove.worldBound.center);

            Assert.AreEqual(selectionCount, selection.selectionCount);

#if !UNITY_2019_4
            Assert.AreEqual(1, colorsUSS.complexSelectors.Length);
            Assert.AreEqual("#builder-test", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[0]));

            Assert.AreEqual(5, layoutUSS.complexSelectors.Length);
            Assert.AreEqual(".unity-button", StyleSheetToUss.ToUssSelector(layoutUSS.complexSelectors[0]));
            Assert.AreEqual(".unity-label", StyleSheetToUss.ToUssSelector(layoutUSS.complexSelectors[1]));
            Assert.AreEqual(".unity-button", StyleSheetToUss.ToUssSelector(layoutUSS.complexSelectors[2]));
            Assert.AreEqual(".unity-label", StyleSheetToUss.ToUssSelector(layoutUSS.complexSelectors[3]));
            Assert.AreEqual("#builder-test", StyleSheetToUss.ToUssSelector(layoutUSS.complexSelectors[4]));
#else
            // Cannot count selectors because we now create fake selectors for variables.
            //Assert.AreEqual(2, colorsUSS.complexSelectors.Length);
            Assert.AreEqual(".unity-label", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[0]));
            Assert.AreEqual("#builder-test", StyleSheetToUss.ToUssSelector(colorsUSS.complexSelectors[1]));

            //Assert.AreEqual(4, layoutUSS.complexSelectors.Length);
            Assert.AreEqual(".unity-button", StyleSheetToUss.ToUssSelector(layoutUSS.complexSelectors[0]));
            Assert.AreEqual(".unity-label", StyleSheetToUss.ToUssSelector(layoutUSS.complexSelectors[1]));
            Assert.AreEqual(".unity-button", StyleSheetToUss.ToUssSelector(layoutUSS.complexSelectors[2]));
            Assert.AreEqual("#builder-test", StyleSheetToUss.ToUssSelector(layoutUSS.complexSelectors[3]));
#endif
        }
    }
}
