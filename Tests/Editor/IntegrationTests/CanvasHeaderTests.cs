using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class CanvasHeaderTests : BuilderIntegrationTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            CreateTestUXMLFile();
            yield return null;
        }

        protected override IEnumerator TearDown()
        {
            yield return base.TearDown();
            DeleteTestUXMLFile();
        }

        /// <summary>
        /// Click on Canvas Header Displays Document Settings.
        /// </summary>
        [UnityTest]
        public IEnumerator ClickOnCanvasHeaderDisplaysDocumentSettings()
        {
            viewport.FitCanvas();

            var documentSettings = inspector.Q(BuilderInspectorCanvas.ContainerName);
            Assert.That(documentSettings, Style.Display(DisplayStyle.None));

            yield return UIETestEvents.Mouse.SimulateClick(canvas.header);
            Assert.That(documentSettings, Style.Display(DisplayStyle.Flex));
        }

        /// <summary>
        /// The currently open UXML asset name, or <unsaved asset>`, is displayed in the Canvas header, grayed out.
        /// </summary>
        [UnityTest]
        public IEnumerator UnsavedAssetHeaderTitleText()
        {
            Assert.True(canvas.titleLabel.text.Contains("<unsaved file>"));

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestUXMLFilePath);
            var toolbar = viewport.Q<BuilderToolbar>();
            toolbar.LoadDocument(asset);

            yield return UIETestHelpers.Pause();
            Assert.True(canvas.titleLabel.text.Contains(k_TestUXMLFileName));
        }

        /// <summary>
        /// Header tooltip contains project relative path to the open UXML asset.
        /// </summary>
        [UnityTest]
        public IEnumerator HeaderTooltipContainsUXMLAssetPath()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestUXMLFilePath);
            var toolbar = viewport.Q<BuilderToolbar>();
            toolbar.LoadDocument(asset);

            yield return UIETestHelpers.Pause();
            Assert.That(canvas.titleLabel.tooltip, Is.EqualTo(k_TestUXMLFilePath));
        }

        /// <summary>
        /// If there are unsaved changes, a `*` is appended to the asset name.
        /// </summary>
#if UNITY_2019_4 && (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
        [UnityTest, Ignore("Test broken on 2019.4 on linux.")]
#else
        [UnityTest]
#endif
        public IEnumerator DocumentUnsavedChangesShouldAddIndicationToTheToolbar()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestUXMLFilePath);
            var toolbar = viewport.Q<BuilderToolbar>();
            toolbar.LoadDocument(asset);

            yield return UIETestHelpers.Pause();
            Assert.That(canvas.titleLabel.text, Is.EqualTo(k_TestUXMLFileName));

            yield return AddVisualElement();
            Assert.That(canvas.titleLabel.text, Is.EqualTo($"{k_TestUXMLFileName}*"));
        }
    }
}
