using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class CanvasSelectionTests : BuilderIntegrationTest
    {
        /// <summary>
        /// Can click to select element.
        /// </summary>
        [UnityTest]
        public IEnumerator CanClickToSelectElement()
        {
            AddElementCodeOnly<Button>();
            var element = GetFirstDocumentElement();

            yield return UIETestEvents.Mouse.SimulateClick(StyleSheetsPane);
            Assert.That(ViewportPane.documentElement.FindSelectedElements().Count, Is.Zero);

            yield return UIETestEvents.Mouse.SimulateClick(element);
            Assert.That(ViewportPane.documentElement.FindSelectedElements()[0], Is.EqualTo(element));
        }

        /// <summary>
        /// Selected element has a blue border around it.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectedElementHasBlueBorder()
        {
            AddElementCodeOnly<Button>();
            var element = GetFirstDocumentElement();
            var selector = ViewportPane.Q("selection-indicator");

            BuilderWindow.selection.ClearSelection(null);
            yield return UIETestHelpers.Pause();
            Assert.That(selector, Style.Display(DisplayStyle.None));
            Assert.IsFalse(selector.worldBound.Overlaps(element.worldBound));

            BuilderWindow.selection.AddToSelection(null, element);
            yield return UIETestHelpers.Pause();
            Assert.That(selector, Style.Display(DisplayStyle.Flex));
            Assert.IsTrue(selector.worldBound.Overlaps(element.worldBound));
        }

        /// <summary>
        /// If there are multiple elements selected, no blue selection border or header should be displayed.
        /// </summary>
        [UnityTest]
        public IEnumerator MultipleSelectedElementsHaveNoBlueBorderOrHeaderDisplayed()
        {
            const string buttonOneName = "ButtonOne";
            const string buttonTwoName = "ButtonTwo";

            AddElementCodeOnly<Button>(buttonOneName);
            AddElementCodeOnly<Button>(buttonTwoName);
            var selector = ViewportPane.Q("selection-indicator");
            var header = ViewportPane.Q("selection-indicator").Q("header");

            BuilderWindow.selection.ClearSelection(null);
            yield return UIETestHelpers.Pause();
            Assert.That(selector.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(header.layout.size.magnitude, Is.Zero);

            BuilderWindow.selection.AddToSelection(null, ViewportPane.Q(buttonOneName));
            yield return UIETestHelpers.Pause();
            Assert.That(selector.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(header.layout.size.magnitude, Is.Not.Zero);

            BuilderWindow.selection.AddToSelection(null, ViewportPane.Q(buttonTwoName));
            yield return UIETestHelpers.Pause();
            Assert.That(selector.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(header.layout.size.magnitude, Is.Zero);
        }

        /// <summary>
        /// Selected element has a title header displaying the type of the element if it has no name, otherwise the name.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectedElementHasATitleOfItsNameOtherwiseItsType()
        {
            AddElementCodeOnly<Button>();
            var element = GetFirstDocumentElement();
            BuilderWindow.selection.Select(null, element);
            yield return UIETestHelpers.Pause();

            var header = ViewportPane.Q("selection-indicator").Q<Label>("header-label");
            Assert.That(header.text, Is.EqualTo(element.typeName));

            ForceNewDocument();
            AddElementCodeOnly<Button>("MyButton");
            element = GetFirstDocumentElement();
            BuilderWindow.selection.Select(null, element);
            yield return UIETestHelpers.Pause();

            header = ViewportPane.Q("selection-indicator").Q<Label>("header-label");
            Assert.That(header.text, Is.EqualTo("#" + element.name));
        }

        /// <summary>
        /// Selecting an element inside a template instance or C# type selects the parent instance or C# element.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectingElementInsideTemplateInstanceOrCSharpSelectsIt()
        {
            //Template Instance
            yield return LoadTestUXMLDocument(k_ParentTestUXMLPath);

            var element = ViewportPane.Q<TemplateContainer>().Children().First();
            yield return UIETestEvents.Mouse.SimulateClick(element);

            Assert.That(ViewportPane.FindSelectedElements()[0].typeName,
                Is.EqualTo(ViewportPane.Q<TemplateContainer>().typeName));

            //C# type
            ForceNewDocument();
            AddElementCodeOnly<TextField>();
            var textInput = ViewportPane.Q("unity-text-input");
            yield return UIETestEvents.Mouse.SimulateClick(textInput);
            Assert.That(ViewportPane.FindSelectedElements()[0].typeName, Is.EqualTo("TextField"));
        }
    }
}
