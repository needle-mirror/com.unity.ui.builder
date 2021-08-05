using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NUnit.Framework;
using Unity.UI.Builder.EditorTests;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class VariableEditingTests : BuilderIntegrationTest
    {
        const string k_NewVariableUxmlFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/VariableTestUXMLDocument.uxml";
        const int k_Delay = 250;
        private const string k_Selector_1 = ".test-button-1";
#if !UI_BUILDER_PACKAGE || UNITY_2020_3 || UNITY_2020_3_12 || UNITY_2021_2_OR_NEWER
        private const string k_Button_1_Font = "test-button-1__font";
#else
        private const string k_Button_1_Font = "test-button-1__font_legacy";
#endif
        private const string k_Button_1 = "button-1";
        private const string k_LengthVarName = "--var-length";
        private const int k_LengthVarValue = 80;

        public VariableEditingHandler currentHandler { get; private set; }

        static VisualElement FindStyleField(VisualElement parent, string name)
        {
            return parent.Query().Where(t =>
            {
                if (t.childCount == 0)
                    return false;
                var label = t[0] as Label;
                return label != null ? label.text.Equals(name) : false;
            }).First();
        }

        static T FindStyleField<T>(VisualElement parent, string name) where T : VisualElement
        {
            return FindStyleField(parent, name) as T;
        }

        static bool HasFocus(VisualElement element)
        {
            return (element.pseudoStates | PseudoStates.Focus) == PseudoStates.Focus; // Use pseudo state because FocusController.focusedElement does not seem to be reliable.
        }

        IEnumerator WaitForUIUpdate()
        {
            // Pause for some delay until the ui gets updated
            // TODO: This should be improved to something more reliable
            return UIETestHelpers.Pause(k_Delay);
        }

#if UI_BUILDER_PACKAGE && UIE_PACKAGE && UNITY_2020_3
        [UnityTest, Ignore("Failing on 2020.3LTS.")]
#else
        [UnityTest]
#endif
        public IEnumerator ShowHideVariableInfoPopup()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_NewVariableUxmlFilePath);
            builder.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            hierarchy.elementHierarchyView.ExpandAllItems();
            styleSheetsPane.elementHierarchyView.ExpandRootItems();

            yield return UIETestHelpers.Pause(1);

            // Select button-1 control
            var button1Item = BuilderTestsHelper.GetExplorerItemWithName(hierarchy, BuilderConstants.UssSelectorNameSymbol + k_Button_1);
            button1Item.AddToClassList(k_Button_1_Font);
            yield return UIETestEvents.Mouse.SimulateClick(button1Item);

            var textFoldout = inspector.Query<PersistedFoldout>().Where(f => f.text.Equals("Text")).First();
            textFoldout.value = true;

            var colorField = FindStyleField<ColorField>(textFoldout, "Color");
            var handler = StyleVariableUtilities.GetVarHandler(colorField);

            handler.ShowVariableField();

            Assert.IsTrue(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableEditingClassName));
            Assert.AreEqual(handler.variableInfoTooltip.resolvedStyle.display, DisplayStyle.Flex);
            Assert.IsNotNull(handler.variableInfoTooltip.currentHandler);
            Assert.AreEqual(handler.variableInfoTooltip.currentHandler, handler);

            // Click anywhere else to remove focus
            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_Selector_1);
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            Assert.IsFalse(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableEditingClassName));
            Assert.AreEqual(handler.variableInfoTooltip.resolvedStyle.display, DisplayStyle.None);
            yield return null;
        }

        IEnumerator EditVariable(string fieldPath, bool editorExtensionMode = false)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_NewVariableUxmlFilePath);
            builder.LoadDocument(asset);
            builder.document.fileSettings.editorExtensionMode = editorExtensionMode;

            currentHandler = null;

            yield return UIETestHelpers.Pause(1);

            hierarchy.elementHierarchyView.ExpandRootItems();
            styleSheetsPane.elementHierarchyView.ExpandRootItems();

            yield return UIETestHelpers.Pause(1);

            // Select test-selector-1
            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_Selector_1);

            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var texts = fieldPath.Split('/');
            PersistedFoldout lastFoldout = null;
            VisualElement currentParent = inspector;
            for (var i = 0; i < (texts.Length - 1); i++)
            {
                var text = texts[i];
                lastFoldout = currentParent.Query<PersistedFoldout>().Where(f => f.text.Equals(text)).First();
                lastFoldout.value = true;
                currentParent = lastFoldout;
            }

            var propertyField = FindStyleField<BindableElement>(lastFoldout, texts.Last());
            currentHandler = StyleVariableUtilities.GetVarHandler(propertyField);

            currentHandler.ShowVariableField();

            // Wait a frame for the variable field to receive focus and dispatch focus in event
            yield return null;

            // Wait another frame for the completer popup to show up
            yield return null;
        }

        [UnityTest]
        public IEnumerator VariableSearch_ShowHidePopup()
        {
            yield return EditVariable("Text/Color");

            var propertyField = currentHandler.targetField;

            Assert.IsTrue(propertyField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableEditingClassName));
            Assert.IsFalse(propertyField.ClassListContains(BuilderConstants.ReadOnlyStyleClassName));
            Assert.AreEqual(currentHandler.completer.popup.resolvedStyle.display, DisplayStyle.Flex);

            // Click anywhere else to remove focus
            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_Selector_1);
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            Assert.IsFalse(propertyField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableEditingClassName));
            Assert.AreEqual(currentHandler.completer.popup.resolvedStyle.display, DisplayStyle.None);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VariableSearch_PopupKeyboardNavigation()
        {
            var builderWindow = this.builder;
            yield return EditVariable("Border/Color/Left");

            var listView = currentHandler.completer.popup.listView;
            Assert.AreEqual(listView.selectedIndex, -1);

            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builderWindow, KeyCode.DownArrow);
            Assert.AreEqual(listView.selectedIndex, 0);

            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builderWindow, KeyCode.DownArrow);
            Assert.AreEqual(listView.selectedIndex, 1);

            // Verify that we stop at the last index
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builderWindow, KeyCode.DownArrow);
#if UI_BUILDER_PACKAGE && (UNITY_2020_2_OR_NEWER && !UNITY_2021_2_OR_NEWER)
            Assert.AreEqual(2, listView.selectedIndex);
#else
            Assert.AreEqual(1, listView.selectedIndex);
#endif

            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builderWindow, KeyCode.UpArrow);
#if UI_BUILDER_PACKAGE && (UNITY_2020_2_OR_NEWER && !UNITY_2021_2_OR_NEWER)
            Assert.AreEqual(1,listView.selectedIndex);
#else
            Assert.AreEqual(0, listView.selectedIndex);
#endif

            // Verify that we clear the selection when the Up Arrow key is pressed with the first item selected
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builderWindow, KeyCode.UpArrow);
#if UI_BUILDER_PACKAGE && (UNITY_2020_2_OR_NEWER && !UNITY_2021_2_OR_NEWER)
            Assert.AreEqual(listView.selectedIndex, 0);
#else
            Assert.AreEqual(listView.selectedIndex, -1);
#endif

            yield return null;
        }

        public IEnumerator CheckVariableCompatibleTypes(string fieldPath)
        {
            yield return EditVariable(fieldPath);

            var listView = currentHandler.completer.popup.listView;
            var compatibleValueTypes = VariableCompleter.GetCompatibleStyleValueTypes(currentHandler);

            Assert.Greater(listView.itemsSource.Count, 0);

            foreach (var item in listView.itemsSource)
            {
                VariableInfo varInfo = item as VariableInfo;

                Assert.NotNull(varInfo);
                Assert.True(compatibleValueTypes.Contains(varInfo.value.handle.valueType));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator VariableSearch_CompatibleColorTypes()
        {
            yield return CheckVariableCompatibleTypes("Border/Color/Left");
        }

        [UnityTest]
        public IEnumerator VariableSearch_CompatibleDimTypes()
        {
            yield return CheckVariableCompatibleTypes("Border/Width/Left");
        }

        [UnityTest]
        public IEnumerator VariableSearch_CompatibleEnumTypes()
        {
            yield return CheckVariableCompatibleTypes("Display/Display");
        }

        [UnityTest]
        public IEnumerator VariableSearch_CompatibleResourceTypes()
        {
            yield return CheckVariableCompatibleTypes("Text/Font");
        }

        public void CheckSearchResults(string[] results)
        {
            var listView = currentHandler.completer.popup.listView;

            Assert.AreEqual(results.Length, listView.itemsSource.Count);
            Assert.True((listView.itemsSource as List<VariableInfo>).All(i => results.Contains(i.name)));
        }

        [UnityTest]
        public IEnumerator VariableSearch_MatchingText()
        {
            yield return EditVariable("Border/Width/Left");

            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, "var");
            yield return WaitForUIUpdate();

            CheckSearchResults(new string[] {"--var-int", "--var-length", "--var-length-pers"});

            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, "-l");
            yield return WaitForUIUpdate();

            CheckSearchResults(new string[] {"--var-length", "--var-length-pers"});

            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, "ength-p");
            yield return WaitForUIUpdate();

            CheckSearchResults(new string[] {"--var-length-pers"});

            yield return null;
        }

        [UnityTest]
        public IEnumerator VariableSearch_NotMatchingText()
        {
            yield return EditVariable("Border/Width/Left");

            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, "xxx");
            yield return WaitForUIUpdate();

            var listView = currentHandler.completer.popup.listView;

            Assert.AreEqual(listView.itemsSource.Count, 0);

            yield return null;
        }

        [UnityTest]
        public IEnumerator VariableSearch_SelectedInfo()
        {
            yield return EditVariable("Border/Width/Left");

            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, "var");
            yield return WaitForUIUpdate();

            var listView = currentHandler.completer.popup.listView;
            var varInfoView = currentHandler.completer.popup.Q<VariableInfoView>();

            Assert.AreEqual(varInfoView.resolvedStyle.display, DisplayStyle.None);

            // Select the first element
            listView.selectedIndex = 1;

            yield return WaitForUIUpdate();

            var info = listView.selectedItem as VariableInfo;
            var styleSheet = info.value.sheet;
            var fullPath = AssetDatabase.GetAssetPath(styleSheet);
            string displayPath = Path.GetFileName(fullPath);
            var valueText = StyleSheetToUss.ValueHandleToUssString(info.value.sheet, new UssExportOptions(), "", info.value.handle);

            Assert.AreEqual(varInfoView.resolvedStyle.display, DisplayStyle.Flex);
            Assert.AreEqual(varInfoView.variableName, info.name);
            Assert.AreEqual(varInfoView.variableValue, valueText);
            Assert.AreEqual(varInfoView.sourceStyleSheet, displayPath);
            if (string.IsNullOrEmpty(info.description))
                Assert.True(string.IsNullOrEmpty(varInfoView.description));
            else
                Assert.AreEqual(varInfoView.description, info.description);
            Assert.AreEqual(varInfoView.Q("description-container").resolvedStyle.display, DisplayStyle.None);
            Assert.AreEqual(varInfoView.Q("preview").resolvedStyle.display, DisplayStyle.None);

            yield return null;
        }

        [UnityTest]
        public IEnumerator VariableSearch_SelectedInfoWithColorPreview()
        {
            yield return EditVariable("Border/Color/Left");

            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, "var");
            yield return WaitForUIUpdate();

            var listView = currentHandler.completer.popup.listView;
            var varInfoView = currentHandler.completer.popup.Q<VariableInfoView>();

            // Select the first element
            listView.selectedIndex = 0;

            yield return WaitForUIUpdate();

            var info = listView.selectedItem as VariableInfo;
            var color = info.value.sheet.ReadColor(info.value.handle);

            var preview = varInfoView.Q("preview");
            Assert.AreEqual(preview.resolvedStyle.display, DisplayStyle.Flex);
            Assert.AreEqual(preview.resolvedStyle.backgroundColor, color);

            yield return null;
        }

        [UnityTest]
        public IEnumerator VariableSearch_SelectedInfoWithImagePreview()
        {
            yield return EditVariable("Cursor/Cursor Image");

            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, "icon");
            yield return WaitForUIUpdate();

            var listView = currentHandler.completer.popup.listView;
            var varInfoView = currentHandler.completer.popup.Q<VariableInfoView>();

            // Select the first element
            listView.selectedIndex = 0;

            yield return WaitForUIUpdate();

            var info = listView.selectedItem as VariableInfo;

            var preview = varInfoView.Q("preview");
            var thumbnail = preview.Q<Image>("thumbnail");
            Assert.AreEqual(preview.resolvedStyle.display, DisplayStyle.Flex);
            Assert.IsNotNull(thumbnail.image);

            yield return null;
        }

#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
        [UnityTest, Ignore("Missing functionality on 2021.1 and older.")]
#else
        [UnityTest]
#endif
        public IEnumerator VariableSearch_SelectedInfoWithDescription()
        {
            yield return EditVariable("Border/Width/Left", true);

            yield return UIETestEvents.KeyBoard.SimulateTyping(builder, "unity-metrics-single");
            yield return WaitForUIUpdate();

            var listView = currentHandler.completer.popup.listView;
            var varInfoView = currentHandler.completer.popup.Q<VariableInfoView>();

            // Select the first element
            listView.selectedIndex = 0;

            yield return WaitForUIUpdate();

            Assert.False(string.IsNullOrEmpty(varInfoView.description));
            Assert.AreEqual(varInfoView.Q("description-container").resolvedStyle.display, DisplayStyle.Flex);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ShowHideVariableField()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_NewVariableUxmlFilePath);
            builder.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            hierarchy.elementHierarchyView.ExpandRootItems();
            styleSheetsPane.elementHierarchyView.ExpandRootItems();

            yield return UIETestHelpers.Pause(1);

            // Select test-selector-1
            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_Selector_1);

            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var textFoldout = inspector.Query<PersistedFoldout>().Where(f => f.text.Equals("Text")).First();
            textFoldout.value = true;

            var colorField = FindStyleField<ColorField>(textFoldout, "Color");
            var handler = StyleVariableUtilities.GetVarHandler(colorField);

            Assert.IsNull(handler.variableField);

            handler.ShowVariableField();

            Assert.IsTrue(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableEditingClassName));
            Assert.IsFalse(colorField.ClassListContains(BuilderConstants.ReadOnlyStyleClassName));
            Assert.IsNotNull(handler.variableField);

            yield return UIETestHelpers.Pause(1);

            var inputField = handler.variableField.Q(TextField.textInputUssName);

            Assert.AreEqual(handler.variableField.resolvedStyle.display, DisplayStyle.Flex);
            Assert.IsTrue(HasFocus(inputField));

            selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_Selector_1);

            // Click anywhere else to remove focus
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            Assert.IsFalse(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableEditingClassName));
            Assert.AreEqual(handler.variableField.resolvedStyle.display, DisplayStyle.None);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HasVariableIndicator()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_NewVariableUxmlFilePath);
            builder.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            hierarchy.elementHierarchyView.ExpandRootItems();
            styleSheetsPane.elementHierarchyView.ExpandRootItems();

            yield return UIETestHelpers.Pause(1);

            // Select test-selector-1
            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_Selector_1);

            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var textFoldout = inspector.Query<PersistedFoldout>().Where(f => f.text.Equals("Text")).First();
            textFoldout.value = true;

            var colorField = FindStyleField(textFoldout, "Color");

            Assert.IsTrue(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableClassName));

            var sizeField = FindStyleField(textFoldout, "Size");

            Assert.IsFalse(sizeField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableClassName));

            yield return null;
        }

        [UnityTest]
        public IEnumerator SetVariable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_NewVariableUxmlFilePath);
            builder.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            hierarchy.elementHierarchyView.ExpandRootItems();
            styleSheetsPane.elementHierarchyView.ExpandRootItems();

            yield return UIETestHelpers.Pause(1);

            // Select test-selector-1
            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_Selector_1);

            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var textFoldout = base.inspector.Query<PersistedFoldout>().Where(f => f.text.Equals("Text")).First();
            textFoldout.value = true;

            // Show variable field for the Size field
            var sizeField = FindStyleField<DimensionStyleField>(textFoldout, "Size");
            var handler = StyleVariableUtilities.GetVarHandler(sizeField);

            Assert.IsFalse(sizeField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableClassName));
            handler.ShowVariableField();

            // Enter variable name
            var textField = handler.variableField.Q<TextField>();

            textField.value = k_LengthVarName;

            selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_Selector_1);

            // Click anywhere else to remove focus
            yield return UIETestEvents.Mouse.SimulateClick(selector);
            yield return UIETestHelpers.Pause(1);

            Assert.IsTrue(sizeField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableClassName));

            var inspector = handler.inspector;

            var styleProperty = BuilderInspectorStyleFields.GetStyleProperty(inspector.currentRule, handler.styleName);

            Assert.IsTrue(styleProperty != null && styleProperty.IsVariable());
            Assert.AreEqual(inspector.styleSheet.ReadVariable(styleProperty), k_LengthVarName);

            yield return null;
        }

        [UnityTest]
        public IEnumerator UnsetVariable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_NewVariableUxmlFilePath);
            builder.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            hierarchy.elementHierarchyView.ExpandRootItems();
            styleSheetsPane.elementHierarchyView.ExpandRootItems();

            yield return UIETestHelpers.Pause(1);

            // Select test-selector-1
            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_Selector_1);

            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var textFoldout = base.inspector.Query<PersistedFoldout>().Where(f => f.text.Equals("Text")).First();
            textFoldout.value = true;

            // Show variable field for the Color field
            var colorField = FindStyleField<ColorField>(textFoldout, "Color");
            var handler = StyleVariableUtilities.GetVarHandler(colorField);

            Assert.IsTrue(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableClassName));
            handler.ShowVariableField();

            // Enter variable name
            var textField = handler.variableField.Q<TextField>();

            textField.value = "";

            selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, k_Selector_1);

            // Click anywhere else to remove focus
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            Assert.IsFalse(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableClassName));

            var inspector = handler.inspector;

            var styleProperty = BuilderInspectorStyleFields.GetStyleProperty(inspector.currentRule, handler.styleName);

            Assert.IsNull(styleProperty);

            yield return null;
        }
    }
}
