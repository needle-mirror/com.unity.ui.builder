#if UNITY_2020_1_OR_NEWER

using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class EditorExtensionsModeTests : BuilderIntegrationTest
    {
        [TearDown]
        public void ModeTearDown()
        {
            BuilderProjectSettings.EnableEditorExtensionModeByDefault = false;
        }

        [UnityTest]
        public IEnumerator DocumentSettingsEditorExtensionToggleValue()
        {
            BuilderWindow.selection.Select(null, BuilderWindow.documentRootElement);
            yield return UIETestHelpers.Pause();

            var documentSettings = InspectorPane.Q(BuilderInspectorCanvas.ContainerName);
            var extensionsModeToggle = documentSettings.Q<Toggle>(BuilderInspectorCanvas.EditorExtensionsModeToggleName);

            // Should be false by default
            Assert.That(BuilderWindow.document.UXMLFileSettings.EditorExtensionMode, Is.EqualTo(false));
            Assert.That(BuilderWindow.document.UXMLFileSettings.EditorExtensionMode, Is.EqualTo(extensionsModeToggle.value));

            extensionsModeToggle.value = true;
            Assert.That(BuilderWindow.document.UXMLFileSettings.EditorExtensionMode, Is.EqualTo(extensionsModeToggle.value));
        }

        [UnityTest]
        public IEnumerator CanvasEditorExtensionsLabelAppearsWhenInEditorExtensionsMode()
        {
            BuilderWindow.selection.Select(null, BuilderWindow.documentRootElement);
            yield return UIETestHelpers.Pause();

            var documentSettings = InspectorPane.Q(BuilderInspectorCanvas.ContainerName);
            var extensionsModeToggle = documentSettings.Q<Toggle>(BuilderInspectorCanvas.EditorExtensionsModeToggleName);

            // Should be false by default
            Assert.That(BuilderWindow.document.UXMLFileSettings.EditorExtensionMode, Is.EqualTo(false));
            Assert.That(Canvas.EditorExtensionsLabel, Style.Display(DisplayStyle.None));

            extensionsModeToggle.value = true;
            Assert.That(Canvas.EditorExtensionsLabel, Style.Display(DisplayStyle.Flex));
        }

        [UnityTest]
        public IEnumerator LibraryDoesNotContainsEditorOnlyControlsWhenInRuntimeMode()
        {
            BuilderWindow.selection.Select(null, BuilderWindow.documentRootElement);
            yield return UIETestHelpers.Pause();

            var documentSettings = InspectorPane.Q(BuilderInspectorCanvas.ContainerName);
            var extensionsModeToggle = documentSettings.Q<Toggle>(BuilderInspectorCanvas.EditorExtensionsModeToggleName);

            // Should be false by default
            Assert.That(BuilderWindow.document.UXMLFileSettings.EditorExtensionMode, Is.EqualTo(false));
            var controlsTreeView = LibraryPane.Q<TreeView>();

            var hasEditorOnly = false;
            foreach (var item in controlsTreeView.items)
            {
                var libraryTreeItem = (BuilderLibraryTreeItem)item;
                if (libraryTreeItem.IsEditorOnly)
                {
                    hasEditorOnly = true;
                    break;
                }
            }
            Assert.That(hasEditorOnly, Is.EqualTo(false));

            extensionsModeToggle.value = true;
            yield return UIETestHelpers.Pause();

            controlsTreeView = LibraryPane.Q<TreeView>();
            foreach (var item in controlsTreeView.items)
            {
                var libraryTreeItem = (BuilderLibraryTreeItem)item;
                if (libraryTreeItem.IsEditorOnly)
                {
                    hasEditorOnly = true;
                    break;
                }
            }

            Assert.That(hasEditorOnly, Is.EqualTo(true));
        }

        [Test]
        public void NewDocumentEditorExtensionsModeValueMatchesWithSettingsDefaultValue()
        {
            Assert.That(BuilderWindow.document.UXMLFileSettings.EditorExtensionMode, Is.EqualTo(false));
            BuilderProjectSettings.EnableEditorExtensionModeByDefault = true;

            ForceNewDocument();
            Assert.That(BuilderWindow.document.UXMLFileSettings.EditorExtensionMode, Is.EqualTo(true));
        }
    }
}
#endif
