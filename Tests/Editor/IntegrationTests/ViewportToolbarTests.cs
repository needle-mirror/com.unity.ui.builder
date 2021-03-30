using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class ViewportToolbarTests : BuilderIntegrationTest
    {
        /// <summary>
        /// Selecting **File > New** clears the selection, the Viewport canvas, the StyleSheets pane, the Hierarchy, and all undo/redo stack operations for the previous document. A prompt is displayed if there are unsaved changes.
        /// </summary>
        [UnityTest]
        public IEnumerator CreatingNewDocumentClearsSelectionAndExplorer()
        {
            var toolbar = viewport.Q<BuilderToolbar>();

            // Make sure File menu exists
            var menuItem = toolbar.Query<ToolbarMenu>().Where(menu => menu.Q<TextElement>().text.Equals("File")).ToList().First();
            Assert.That(menuItem, Is.Not.Null);

            AddElementCodeOnly();
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();
            yield return AddSelector(StyleSheetsPaneTests.TestSelectorName);

            ForceNewDocument();
            Assert.That(viewport.documentRootElement.childCount, Is.EqualTo(0));
            Assert.That(BuilderTestsHelper.GetExplorerItems(styleSheetsPane).Count, Is.EqualTo(0));
            Assert.That(BuilderTestsHelper.GetExplorerItems(hierarchy).Count, Is.EqualTo(0));
            Assert.True(builder.selection.isEmpty);
        }

        /// <summary>
        /// Can select a zoom level from the **100%** dropdown. Can also zoom via the mouse scroll wheel and Alt + RightClick + Mouse Move.
        /// </summary>
        [UnityTest]
        public IEnumerator ZoomWithMouseScrollAndRightClick()
        {
            var toolbar = viewport.Q<BuilderToolbar>();
            var zoomMenuItem = toolbar.Query<ToolbarMenu>().Where(menu => menu.Q<TextElement>().text.Equals("100%")).ToList().First();
            Assert.That(zoomMenuItem, Is.Not.Null);

            // Zoom With Scroll Wheel
            yield return UIETestEvents.Mouse.SimulateClick(viewport);
            yield return UIETestEvents.Mouse.SimulateScroll(viewport, Vector2.one * 100, viewport.worldBound.center);

            yield return UIETestEvents.Mouse.SimulateMouseEvent(builder,
                EventType.MouseDown,
                viewport.worldBound.center,
                MouseButton.RightMouse,
                EventModifiers.Alt);

            yield return UIETestEvents.Mouse.SimulateMouseMove(builder,
                viewport.worldBound.center,
                viewport.worldBound.center + Vector2.one * 20,
                MouseButton.RightMouse,
                EventModifiers.Alt);

            yield return UIETestHelpers.Pause(1);
            Assert.That(zoomMenuItem.text, Is.Not.EqualTo("100%"));
        }

        /// <summary>
        /// Can reset the view and make sure the canvas fits the viewport with the **Fit Canvas** button.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator CanResetViewAndFitCanvas()
        {
            var toolbar = base.viewport.Q<BuilderToolbar>();
            var fitCanvasButton = toolbar.Q<ToolbarButton>(BuilderToolbar.FitCanvasButtonName);
            Assert.That(fitCanvasButton, Is.Not.Null);

            var canvas = base.viewport.Q<BuilderCanvas>();
            var viewport = base.viewport.Q<VisualElement>("viewport");

            // A new document, when opened, is centered by default, thus we need to offset it before
            // clicking on the FitCanvas button
            yield return UIETestEvents.Mouse.SimulateMouseEvent(builder,
                EventType.MouseDown,
                canvas.worldBound.center,
                MouseButton.MiddleMouse);

            yield return UIETestEvents.Mouse.SimulateMouseMove(builder,
                base.viewport.worldBound.center,
                canvas.worldBound.center + Vector2.one * 100,
                MouseButton.MiddleMouse);

            yield return UIETestEvents.Mouse.SimulateMouseEvent(builder,
                EventType.MouseUp,
                canvas.worldBound.center,
                MouseButton.MiddleMouse);

            Assert.That(canvas.worldBound.center, Is.Not.EqualTo(viewport.worldBound.center));

            yield return UIETestEvents.Mouse.SimulateClick(fitCanvasButton);

            Assert.True(Mathf.Abs(canvas.worldBound.center.x - viewport.worldBound.center.x) < 1f);
            Assert.True(Mathf.Abs(canvas.worldBound.center.y - viewport.worldBound.center.y) < 1f);
        }

        /// <summary>
        ///  Pressing **Preview** toggles _Preview_ mode, where you can no longer select elements by clicking them in the Viewport. Instead, Viewport elements receive regular mouse and focus events.
        /// </summary>
        [UnityTest]
        public IEnumerator PreviewModeBehaviour()
        {
            var toolbar = viewport.Q<BuilderToolbar>();
            var previewToggle = toolbar.Q<ToolbarToggle>(BuilderToolbar.PreviewToggleName);
            Assert.That(previewToggle, Is.Not.Null);

            // Fit Canvas to make sure added button will be visible
            var fitCanvasButton = toolbar.Q<ToolbarButton>(BuilderToolbar.FitCanvasButtonName);
            Assert.That(fitCanvasButton, Is.Not.Null);
            yield return UIETestEvents.Mouse.SimulateClick(fitCanvasButton);

            AddElementCodeOnly<Button>();
            yield return UIETestEvents.Mouse.SimulateClick(viewport);
            Assert.True(builder.selection.isEmpty);

            var button = (Button)GetFirstDocumentElement();
            yield return UIETestEvents.Mouse.SimulateClick(button);
            Assert.True(button.pseudoStates == 0);
            Assert.That(builder.selection.selection.First(), Is.EqualTo(button));

            yield return UIETestEvents.Mouse.SimulateClick(viewport);
            Assert.True(builder.selection.isEmpty);
            yield return UIETestEvents.Mouse.SimulateClick(previewToggle);
            yield return UIETestEvents.Mouse.SimulateClick(button);
            Assert.True(builder.selection.isEmpty);
            Assert.True(button.pseudoStates.HasFlag(PseudoStates.Hover));
        }
    }
}
