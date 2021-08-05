#if !UNITY_2019_4

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
            BuilderProjectSettings.enableEditorExtensionModeByDefault = false;
        }

        [UnityTest]
        public IEnumerator DocumentSettingsEditorExtensionToggleValue()
        {
            builder.selection.Select(null, builder.documentRootElement);
            yield return UIETestHelpers.Pause();

            var documentSettings = inspector.Q(BuilderInspectorCanvas.ContainerName);
            var extensionsModeToggle = documentSettings.Q<Toggle>(BuilderInspectorCanvas.EditorExtensionsModeToggleName);

            // Should be false by default
            Assert.That(builder.document.fileSettings.editorExtensionMode, Is.EqualTo(false));
            Assert.That(builder.document.fileSettings.editorExtensionMode, Is.EqualTo(extensionsModeToggle.value));

            extensionsModeToggle.value = true;
            Assert.That(builder.document.fileSettings.editorExtensionMode, Is.EqualTo(extensionsModeToggle.value));
        }

        [UnityTest]
        public IEnumerator CanvasEditorExtensionsLabelAppearsWhenInEditorExtensionsMode()
        {
            builder.selection.Select(null, builder.documentRootElement);
            yield return UIETestHelpers.Pause();

            var documentSettings = inspector.Q(BuilderInspectorCanvas.ContainerName);
            var extensionsModeToggle = documentSettings.Q<Toggle>(BuilderInspectorCanvas.EditorExtensionsModeToggleName);

            // Should be false by default
            Assert.That(builder.document.fileSettings.editorExtensionMode, Is.EqualTo(false));
            Assert.That(canvas.editorExtensionsLabel, Style.Display(DisplayStyle.None));

            extensionsModeToggle.value = true;
            Assert.That(canvas.editorExtensionsLabel, Style.Display(DisplayStyle.Flex));
        }

        [UnityTest]
        public IEnumerator LibraryDoesNotContainsEditorOnlyControlsWhenInRuntimeMode()
        {
            builder.selection.Select(null, builder.documentRootElement);
            yield return UIETestHelpers.Pause();

            var documentSettings = inspector.Q(BuilderInspectorCanvas.ContainerName);
            var extensionsModeToggle = documentSettings.Q<Toggle>(BuilderInspectorCanvas.EditorExtensionsModeToggleName);

            // Should be false by default
            Assert.That(builder.document.fileSettings.editorExtensionMode, Is.EqualTo(false));
#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            var controlsTreeView = library.Q<TreeView>();
#else
            var controlsTreeView = library.Q<InternalTreeView>();
#endif

            var hasEditorOnly = false;
            foreach (var item in controlsTreeView.items)
            {
                var libraryTreeItem = (BuilderLibraryTreeItem)item;
                if (libraryTreeItem.isEditorOnly)
                {
                    hasEditorOnly = true;
                    break;
                }
            }
            Assert.That(hasEditorOnly, Is.EqualTo(false));

            extensionsModeToggle.value = true;
            yield return UIETestHelpers.Pause();

#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
            controlsTreeView = library.Q<TreeView>();
#else
            controlsTreeView = library.Q<InternalTreeView>();
#endif
            foreach (var item in controlsTreeView.items)
            {
                var libraryTreeItem = (BuilderLibraryTreeItem)item;
                if (libraryTreeItem.isEditorOnly)
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
            Assert.That(builder.document.fileSettings.editorExtensionMode, Is.EqualTo(false));
            BuilderProjectSettings.enableEditorExtensionModeByDefault = true;

            ForceNewDocument();
            Assert.That(builder.document.fileSettings.editorExtensionMode, Is.EqualTo(true));
        }
    }
}
#endif
