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
            inspector.Q<IntegerField>("canvas-width").value = 1;
            Assert.That(viewport.canvas.width, Is.EqualTo(BuilderConstants.CanvasMinWidth));

            inspector.Q<IntegerField>("canvas-height").value = 1;
            Assert.That(viewport.canvas.height, Is.EqualTo(BuilderConstants.CanvasMinHeight));
        }

        /// <summary>
        /// Canvas size is restored after Domain Reload or Window reload.
        /// It is reset when opening/creating a new document.
        /// </summary>
        [Test]
        public void CanvasSizeRestoredOnDomainOrWindowReloadAndResetsOnDocInit()
        {
            builder.canvas.ResetSize();
            var newWidth = k_TestCanvasSizeValue;

            inspector.Q<IntegerField>("canvas-width").value = newWidth;
            Assert.That(builder.canvas.width, Is.EqualTo(newWidth));

            builder.Close();
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
            var toolbar = viewport.Q<BuilderToolbar>();
            toolbar.LoadDocument(asset);
            yield return UIETestHelpers.Pause();
            builder.canvas.ResetSize();
            builder.canvas.width = k_TestCanvasSizeValue;
            Assert.That(builder.canvas.width, Is.EqualTo(k_TestCanvasSizeValue));

            ForceNewDocument();
            Assert.That(builder.canvas.width, Is.Not.EqualTo(k_TestCanvasSizeValue));

            toolbar.LoadDocument(asset);
            Assert.That(builder.canvas.width, Is.EqualTo(k_TestCanvasSizeValue));
        }

        /// <summary>
        /// Can be resized via handles on all 4 sides.
        /// </summary>
        [UnityTest, Ignore("Unstable on 2020.2 & trunk, we need to revisit this.")]
        public IEnumerator CanBeResizedViaHandles()
        {
            var rightHandle = builder.viewport.Q(null, "unity-builder-canvas__side--right");
            builder.canvas.ResetSize();
            var defaultWidth = builder.canvas.width;
            var defaultHeight = builder.canvas.height;

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                rightHandle.worldBound.center,
                new Vector2(rightHandle.worldBound.center.x - 50, rightHandle.worldBound.center.y));
            Assert.That(defaultWidth, Is.GreaterThan(builder.canvas.width));

            builder.canvas.ResetSize();
            var leftHandle = builder.viewport.Q(null, "unity-builder-canvas__side--left");
            defaultWidth = builder.canvas.width;
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                leftHandle.worldBound.center,
                new Vector2(leftHandle.worldBound.center.x + 50, rightHandle.worldBound.center.y));
            Assert.That(defaultWidth, Is.GreaterThan(builder.canvas.width));

            builder.canvas.ResetSize();
            var bottomHandle = builder.viewport.Q(null, "unity-builder-canvas__side--bottom");
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                bottomHandle.worldBound.center,
                new Vector2(bottomHandle.worldBound.center.x, bottomHandle.worldBound.center.y - 50));
            Assert.That(defaultHeight, Is.GreaterThan(builder.canvas.height));

            builder.canvas.ResetSize();
            var topHandle = builder.viewport.Q(null, "unity-builder-canvas__side--top");
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(builder,
                topHandle.worldBound.center,
                new Vector2(topHandle.worldBound.center.x, bottomHandle.worldBound.center.y + 50));
            Assert.That(defaultHeight, Is.GreaterThan(builder.canvas.height));
        }

        /// <summary>
        /// Right-clicking an element in the Canvas opens the Copy/Paste/Duplicate/Delete/Rename context menu.
        /// </summary>
        [UnityTest]
        public IEnumerator RightClickingAnElementOpensContextMenu()
        {
            AddElementCodeOnly<VisualElement>();
            var element = GetFirstDocumentElement();

            var panel = builder.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;

            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            yield return UIETestEvents.Mouse.SimulateClick(element, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);
        }
    }
}
