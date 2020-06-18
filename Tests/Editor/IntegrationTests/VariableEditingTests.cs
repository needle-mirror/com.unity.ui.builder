using System.Collections;
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
        private const string k_Selector_1 = ".test-button-1";
        private const string k_LengthVarName = "--var-length";
        private const int k_LengthVarValue = 80;

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

#if UNITY_2019_3_OR_NEWER
        [UnityTest]
        public IEnumerator ShowHideVariableInfoPopup()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_NewVariableUxmlFilePath);
            BuilderWindow.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            // Select test-selector-1
            var selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_Selector_1);

            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var textFoldout = InspectorPane.Query<PersistedFoldout>().Where(f => f.text.Equals("Text")).First();
            textFoldout.value = true;

            var colorField = FindStyleField<ColorField>(textFoldout, "Color");
            var handler = StyleVariableUtilities.GetVarHandler(colorField);

            yield return UIETestEvents.Mouse.SimulateClick(colorField.labelElement);

            Assert.IsTrue(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableEditingClassName));
            Assert.IsFalse(colorField.ClassListContains(BuilderConstants.ReadOnlyStyleClassName));
            Assert.IsTrue(handler.variableInfoTooltip.visible);
            Assert.IsTrue(handler.variableInfoTooltip.IsFocused());
            Assert.IsNotNull(handler.variableInfoTooltip.currentHandler);
            Assert.AreEqual(handler.variableInfoTooltip.currentHandler, handler);

            // Click anywhere else to remove focus
            selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_Selector_1);
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            Assert.IsFalse(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableEditingClassName));
            Assert.IsFalse(handler.variableInfoTooltip.isShowing);
            Assert.IsNull(handler.variableInfoTooltip.currentHandler);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShowHideVariableField()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_NewVariableUxmlFilePath);
            BuilderWindow.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            // Select test-selector-1
            var selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_Selector_1);

            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var textFoldout = InspectorPane.Query<PersistedFoldout>().Where(f => f.text.Equals("Text")).First();
            textFoldout.value = true;

            var colorField = FindStyleField<ColorField>(textFoldout, "Color");
            var handler = StyleVariableUtilities.GetVarHandler(colorField);

            Assert.IsNull(handler.variableField);

            yield return UIETestEvents.Mouse.SimulateDoubleClick(colorField.labelElement);

            Assert.IsTrue(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableEditingClassName));
            Assert.IsFalse(colorField.ClassListContains(BuilderConstants.ReadOnlyStyleClassName));
            // Check VariableEditingHandler.variableInfoTooltip == null as double click may be to fast so that the tooltip is not shown for the first time.
            Assert.IsTrue(handler.variableInfoTooltip == null || handler.variableInfoTooltip.resolvedStyle.display == DisplayStyle.None);
            Assert.IsNotNull(handler.variableField);

            yield return UIETestHelpers.Pause(1);

            var inputField = handler.variableField.Q(TextField.textInputUssName);

            Assert.AreEqual(handler.variableField.resolvedStyle.display, DisplayStyle.Flex);
            Assert.IsTrue(HasFocus(inputField));

            selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_Selector_1);

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
            BuilderWindow.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            // Select test-selector-1
            var selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_Selector_1);

            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var textFoldout = InspectorPane.Query<PersistedFoldout>().Where(f => f.text.Equals("Text")).First();
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
            BuilderWindow.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            // Select test-selector-1
            var selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_Selector_1);

            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var textFoldout = InspectorPane.Query<PersistedFoldout>().Where(f => f.text.Equals("Text")).First();
            textFoldout.value = true;

            // Show variable field for the Size field
            var sizeField = FindStyleField<DimensionStyleField>(textFoldout, "Size");

            Assert.IsFalse(sizeField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableClassName));

            yield return UIETestEvents.Mouse.SimulateDoubleClick(sizeField.labelElement);

            yield return UIETestHelpers.Pause(1);

            // Enter variable name
            var handler = StyleVariableUtilities.GetVarHandler(sizeField);
            var textField = handler.variableField.Q<TextField>();

            textField.value = k_LengthVarName;

            selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_Selector_1);

            // Click anywhere else to remove focus
            yield return UIETestEvents.Mouse.SimulateClick(selector);

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
            BuilderWindow.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            // Select test-selector-1
            var selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_Selector_1);

            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var textFoldout = InspectorPane.Query<PersistedFoldout>().Where(f => f.text.Equals("Text")).First();
            textFoldout.value = true;

            // Show variable field for the Color field
            var colorField = FindStyleField<ColorField>(textFoldout, "Color");

            Assert.IsTrue(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableClassName));

            yield return UIETestEvents.Mouse.SimulateDoubleClick(colorField.labelElement);

            yield return UIETestHelpers.Pause(1);

            // Enter variable name
            var handler = StyleVariableUtilities.GetVarHandler(colorField);
            var textField = handler.variableField.Q<TextField>();

            textField.value = "";

            selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_Selector_1);

            // Click anywhere else to remove focus
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            Assert.IsFalse(colorField.ClassListContains(BuilderConstants.InspectorLocalStyleVariableClassName));

            var inspector = handler.inspector;

            var styleProperty = BuilderInspectorStyleFields.GetStyleProperty(inspector.currentRule, handler.styleName);

            Assert.IsNull(styleProperty);

            yield return null;
        }
#endif

    }
}