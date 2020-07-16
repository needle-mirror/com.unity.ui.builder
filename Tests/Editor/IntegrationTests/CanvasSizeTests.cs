using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class CanvasSizeTests : BuilderIntegrationTest
    {
        Builder m_NewBuilder;
        const int k_TestCanvasSizeValue = 299;

        [UnityTearDown]
        protected override IEnumerator TearDown()
        {
            if (m_NewBuilder != null)
                m_NewBuilder.Close();
            yield return base.TearDown();
            DeleteTestUXMLFile();
        }

        /// <summary>
        /// Canvas has a minimum size.
        /// </summary>
        [Test]
        public void CanvasHasMinimumSize()
        {
            InspectorPane.Q<IntegerField>("canvas-width").value = 1;
            Assert.That(ViewportPane.canvas.width, Is.EqualTo(BuilderConstants.CanvasMinWidth));

            InspectorPane.Q<IntegerField>("canvas-height").value = 1;
            Assert.That(ViewportPane.canvas.height, Is.EqualTo(BuilderConstants.CanvasMinHeight));
        }

        /// <summary>
        /// Canvas size is restored after Domain Reload or Window reload.
        /// It is reset when opening/creating a new document.
        /// </summary>
        [Test]
        public void CanvasSizeRestoredOnDomainOrWindowReloadAndResetsOnDocInit()
        {
            BuilderWindow.canvas.ResetSize();
            var newWidth = k_TestCanvasSizeValue;

            InspectorPane.Q<IntegerField>("canvas-width").value = newWidth;
            Assert.That(BuilderWindow.canvas.width, Is.EqualTo(newWidth));

            BuilderWindow.Close();
            m_NewBuilder = BuilderTestsHelper.MakeNewBuilderWindow();
            Assert.That(m_NewBuilder.canvas.width, Is.EqualTo(newWidth));

            m_NewBuilder.rootVisualElement.Q<BuilderToolbar>().NewDocument(false);
            Assert.That(m_NewBuilder.canvas.width, Is.Not.EqualTo(newWidth));
        }

        /// <summary>
        /// Canvas size is remembered per-document.
        /// </summary>
        [UnityTest]
        public IEnumerator CanvasSizeRememberedPerDocument()
        {
            CreateTestUXMLFile();
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestUXMLFilePath);
            var toolbar = ViewportPane.Q<BuilderToolbar>();
            toolbar.LoadDocument(asset);
            yield return UIETestHelpers.Pause();
            BuilderWindow.canvas.ResetSize();
            BuilderWindow.canvas.width = k_TestCanvasSizeValue;
            Assert.That(BuilderWindow.canvas.width, Is.EqualTo(k_TestCanvasSizeValue));

            ForceNewDocument();
            Assert.That(BuilderWindow.canvas.width, Is.Not.EqualTo(k_TestCanvasSizeValue));

            toolbar.LoadDocument(asset);
            Assert.That(BuilderWindow.canvas.width, Is.EqualTo(k_TestCanvasSizeValue));
        }

        /// <summary>
        /// Can be resized via handles on all 4 sides.
        /// </summary>
        [UnityTest, Ignore("Unstable on 2020.2 & trunk, we need to revisit this.")]
        public IEnumerator CanBeResizedViaHandles()
        {
            var rightHandle = BuilderWindow.viewport.Q(null, "unity-builder-canvas__side--right");
            BuilderWindow.canvas.ResetSize();
            var defaultWidth = BuilderWindow.canvas.width;
            var defaultHeight = BuilderWindow.canvas.height;

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                rightHandle.worldBound.center,
                new Vector2(rightHandle.worldBound.center.x - 50, rightHandle.worldBound.center.y));
            Assert.That(defaultWidth, Is.GreaterThan(BuilderWindow.canvas.width));

            BuilderWindow.canvas.ResetSize();
            var leftHandle = BuilderWindow.viewport.Q(null, "unity-builder-canvas__side--left");
            defaultWidth = BuilderWindow.canvas.width;
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                leftHandle.worldBound.center,
                new Vector2(leftHandle.worldBound.center.x + 50, rightHandle.worldBound.center.y));
            Assert.That(defaultWidth, Is.GreaterThan(BuilderWindow.canvas.width));

            BuilderWindow.canvas.ResetSize();
            var bottomHandle = BuilderWindow.viewport.Q(null, "unity-builder-canvas__side--bottom");
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                bottomHandle.worldBound.center,
                new Vector2(bottomHandle.worldBound.center.x, bottomHandle.worldBound.center.y - 50));
            Assert.That(defaultHeight, Is.GreaterThan(BuilderWindow.canvas.height));

            BuilderWindow.canvas.ResetSize();
            var topHandle = BuilderWindow.viewport.Q(null, "unity-builder-canvas__side--top");
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                topHandle.worldBound.center,
                new Vector2(topHandle.worldBound.center.x, bottomHandle.worldBound.center.y + 50));
            Assert.That(defaultHeight, Is.GreaterThan(BuilderWindow.canvas.height));
        }

        /// <summary>
        /// Right-clicking an element in the Canvas opens the Copy/Paste/Duplicate/Delete/Rename context menu.
        /// </summary>
        [UnityTest]
        public IEnumerator RightClickingAnElementOpensContextMenu()
        {
            AddElementCodeOnly<VisualElement>();
            var element = GetFirstDocumentElement();

            var panel = BuilderWindow.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;

            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            yield return UIETestEvents.Mouse.SimulateClick(element, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);
        }
    }
}
