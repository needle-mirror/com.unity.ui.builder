using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class CanvasElementInteractionTests : BuilderIntegrationTest
    {
        const int k_OffsetValue = 25;
        const string k_TestStyleName = "test-style";
        const string k_ElementLength = "100px";

        /// <summary>
        /// Absolute position elements have all four side handles and all four corner handles visible.
        /// </summary>
        [UnityTest]
        public IEnumerator AbsolutePositionElementsHaveAllHandlesVisible()
        {
            AddElementCodeOnly<Button>();
            var element = GetFirstDocumentElement();

            element.style.position = Position.Absolute;
            yield return UIETestHelpers.Pause();

            var handles = new[]
            {
                "top-handle", "left-handle", "bottom-handle", "right-handle",
                "top-left-handle", "top-right-handle", "bottom-left-handle", "bottom-right-handle",
            };

            foreach (var handle in handles)
                Assert.That(builder.viewport.Q(handle, "unity-builder-resizer"), Style.Display(DisplayStyle.Flex));
        }

        /// <summary>
        /// Absolute position elements have four anchor handles visible to set or unset the `left`/`right`/`top`/`bottom` inline styles.
        /// </summary>
        [UnityTest]
        public IEnumerator AbsolutePositionElementsHaveFourAnchorHandlesVisible()
        {
            AddElementCodeOnly<Button>();
            var element = GetFirstDocumentElement();

            element.style.position = Position.Absolute;
            yield return UIETestHelpers.Pause();

            var anchorHandleClasses = new[]
            {
                "unity-builder-anchorer--bottom", "unity-builder-anchorer--right",
                "unity-builder-anchorer--top", "unity-builder-anchorer--left",
            };

            foreach (var item in anchorHandleClasses)
                Assert.That(builder.viewport.Q(null, item), Style.Display(DisplayStyle.Flex));
        }

        /// <summary>
        /// Absolute position elements can be moved by clicking and dragging, changing `top`/`right`/`left`/`bottom` inline styles depending on anchor state.
        /// </summary>
        [UnityTest]
        public IEnumerator AbsolutePositionElementsCanBeMovedByClickAndDrag()
        {
            AddElementCodeOnly<Button>();
            var element = GetFirstDocumentElement();
            yield return UIETestEvents.Mouse.SimulateClick(element);

            // It is important to change its Position through inspector
            inspector.Query<PersistedFoldout>().Where(f => f.text.Equals("Position")).ToList().First()
                .Q<EnumField>().value = Position.Absolute;

            yield return UIETestHelpers.Pause();

            // Moving an element a bit to trigger activation of Top and Left anchors
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                element.worldBound.center, new Vector2(element.worldBound.center.x + k_OffsetValue,
                    element.worldBound.center.y + k_OffsetValue));

            var bottomAnchor = builder.viewport.Q(null, "unity-builder-anchorer--bottom");
            yield return UIETestEvents.Mouse.SimulateClick(bottomAnchor);
            var rightAnchor = builder.viewport.Q(null, "unity-builder-anchorer--right");
            yield return UIETestEvents.Mouse.SimulateClick(rightAnchor);

            var topStyleOld = element.resolvedStyle.top;
            var leftStyleOld = element.resolvedStyle.left;
            var bottomStyleOld = element.resolvedStyle.bottom;
            var rightStyleOld = element.resolvedStyle.right;

            var oldPos = element.worldBound.center;

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                element.worldBound.center, new Vector2(element.worldBound.center.x + k_OffsetValue,
                    element.worldBound.center.y + k_OffsetValue));

            Assert.That(element.worldBound.center.magnitude, Is.GreaterThan(oldPos.magnitude));
            Assert.That(element.resolvedStyle.top, Is.Not.EqualTo(topStyleOld));
            Assert.That(element.resolvedStyle.left, Is.Not.EqualTo(leftStyleOld));
            Assert.That(element.resolvedStyle.bottom, Is.Not.EqualTo(bottomStyleOld));
            Assert.That(element.resolvedStyle.right, Is.Not.EqualTo(rightStyleOld));
        }

        /// <summary>
        /// Resize and position handles change different styles depending on anchor state (ie. if left
        /// and right styles are set, changing the width changes the right style - otherwise,
        /// changing the width changes the width style).
        /// </summary>
        [UnityTest]
        public IEnumerator ResizeAndPositionHandlesChangeStyles()
        {
            AddElementCodeOnly<Button>();
            var element = GetFirstDocumentElement();
            yield return UIETestEvents.Mouse.SimulateClick(element);

            element.style.width = 50;
            element.style.height = 50;
            element.style.position = Position.Absolute;
            yield return UIETestHelpers.Pause();

            // Left anchor is set
            var handle = builder.viewport.Q(null, "unity-builder-resizer__side--right");

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                handle.worldBound.center, new Vector2(handle.worldBound.center.x + k_OffsetValue,
                    handle.worldBound.center.y));

            Assert.That(element.resolvedStyle.width, Is.GreaterThan(k_OffsetValue));

            // Left and Right anchors are set
            var oldRightValue = element.resolvedStyle.right;
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                handle.worldBound.center, new Vector2(handle.worldBound.center.x + k_OffsetValue,
                    handle.worldBound.center.y));

            Assert.That(element.style.right.value.value, Is.LessThan(oldRightValue));

            // Top anchor is set
            handle = builder.viewport.Q(null, "unity-builder-resizer__side--bottom");
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                handle.worldBound.center, new Vector2(handle.worldBound.center.x,
                    handle.worldBound.center.y + k_OffsetValue));

            Assert.That(element.resolvedStyle.height, Is.GreaterThan(k_OffsetValue));

            // Top and Bottom anchors are set
            var oldBottomValue = element.resolvedStyle.bottom;

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                handle.worldBound.center, new Vector2(handle.worldBound.center.x,
                    handle.worldBound.center.y + k_OffsetValue));

            Assert.That(element.style.bottom.value.value, Is.LessThan(oldBottomValue));
        }

        /// <summary>
        /// When changing Width or Height in the Inspector, the corresponding resize handles in the canvas are highlighted.
        /// </summary>
        [UnityTest, Ignore("Unstable on 2020.2 & trunk, we need to revisit this.")]
        public IEnumerator ChangingWidthOrHeightHighlightsHandles()
        {
            AddElementCodeOnly<Button>();
            var element = GetFirstDocumentElement();
            yield return UIETestEvents.Mouse.SimulateClick(element);

            element.style.left = k_OffsetValue;
            element.style.top = k_OffsetValue;
            element.style.position = Position.Absolute;
            yield return UIETestHelpers.Pause();

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                element.worldBound.center, new Vector2(element.worldBound.center.x + k_OffsetValue,
                    element.worldBound.center.y + k_OffsetValue));

            var rightHandle = builder.viewport.Q("right-handle", "unity-builder-resizer");
            Assert.That(rightHandle.pseudoStates, Is.Not.EqualTo(PseudoStates.Hover));
            inspector.Q<TemplateContainer>("size-section").Query<DimensionStyleField>()
                .Where(t => t.label.Equals("Width")).ToList().First().value = k_ElementLength;

            yield return UIETestHelpers.Pause();
            Assert.That(rightHandle.pseudoStates, Is.EqualTo(PseudoStates.Hover));

            var bottomHandle = builder.viewport.Q("bottom-handle", "unity-builder-resizer");

            Assert.That(bottomHandle.pseudoStates, Is.Not.EqualTo(PseudoStates.Hover));

            inspector.Q<TemplateContainer>("size-section").Query<DimensionStyleField>()
                .Where(t => t.label.Equals("Height")).ToList().First().value = k_ElementLength;

            yield return UIETestHelpers.Pause();
            Assert.That(bottomHandle.pseudoStates, Is.EqualTo(PseudoStates.Hover));
        }

        /// <summary>
        /// When hovering over elements in the Canvas, the corresponding entry in the Hierarchy is highlighted.
        /// </summary>
        [UnityTest]
        public IEnumerator HoveringOverElementsHighlightsThemInHierarchy()
        {
            AddElementCodeOnly<Button>();
            var element = GetFirstDocumentElement();
            var linkedElement = BuilderTestsHelper.GetLinkedExplorerItem(element);
            var oldColor = linkedElement.parent.parent.resolvedStyle.backgroundColor;

            // SimulateMove doesn't want to move without SimulateClick or additional move event... weird
            yield return UIETestEvents.Mouse.SimulateClick(viewport.canvas);

            yield return UIETestEvents.Mouse.SimulateMouseEvent(builder, EventType.MouseMove,
                element.worldBound.center);

            Assert.That(linkedElement.parent.parent.resolvedStyle.backgroundColor, Is.Not.EqualTo(oldColor));
        }

        /// <summary>
        /// When hovering over elements in the Canvas, all StyleSheets pane entries of style selectors
        /// that match this element are highlighted.
        /// </summary>
        [UnityTest]
        public IEnumerator HoveringOverElementsHighlightsMatchingStyleSheets()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();
            yield return AddSelector(k_TestStyleName);
            yield return UIETestHelpers.Pause();

            AddElementCodeOnly<Button>();
            var element = viewport.canvas.Q<Button>();

            element.AddToClassList(k_TestStyleName);
            var selectorBackground = styleSheetsPane.Q(null, "unity-scroll-view__content-container").Children().ToList()[1];
            var oldColor = selectorBackground.resolvedStyle.backgroundColor;
            yield return UIETestEvents.Mouse.SimulateClick(inspector);

            yield return UIETestEvents.Mouse.SimulateMouseMove(builder,
                inspector.worldBound.center, element.worldBound.center);

            Assert.That(selectorBackground.resolvedStyle.backgroundColor, Is.Not.EqualTo(oldColor));
        }
    }
}
