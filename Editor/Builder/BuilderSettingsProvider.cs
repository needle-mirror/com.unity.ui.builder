using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    public class BuilderSettingsProvider : SettingsProvider
    {
        const string k_EditorExtensionsModeToggleName = "editor-extensions-mode-toggle";

#if UNITY_2020_1_OR_NEWER
        [SettingsProvider]
#endif
        public static SettingsProvider PreferenceSettingsProvider()
        {
            return new BuilderSettingsProvider();
        }

        public static string Name => $"Project/{BuilderConstants.BuilderWindowTitle}";
        bool HasSearchInterestHandler(string searchContext) => true;

        public BuilderSettingsProvider() : base(Name, SettingsScope.Project)
        {
            hasSearchInterestHandler = HasSearchInterestHandler;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var builderTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.SettingsUIPath + "/BuilderSettingsView.uxml");
            builderTemplate.CloneTree(rootElement);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.SettingsUIPath + "/BuilderSettingsView.uss");
            rootElement.styleSheets.Add(styleSheet);

            var editorExtensionsModeToggle = rootElement.Q<Toggle>(k_EditorExtensionsModeToggleName);
            editorExtensionsModeToggle.SetValueWithoutNotify(BuilderProjectSettings.EnableEditorExtensionModeByDefault);
            editorExtensionsModeToggle.RegisterValueChangedCallback(e =>
            {
                BuilderProjectSettings.EnableEditorExtensionModeByDefault = e.newValue;
            });

            base.OnActivate(searchContext, rootElement);
        }
    }
}
