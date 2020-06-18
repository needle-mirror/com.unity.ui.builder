using System;
using UnityEditor;

namespace Unity.UI.Builder
{
    static class BuilderProjectSettings
    {
        const string k_EditorExtensionModeKey = "UIBuilder.EditorExtensionModeKey";

        public static bool EnableEditorExtensionModeByDefault
        {
            get
            {
                var value = EditorUserSettings.GetConfigValue(k_EditorExtensionModeKey);
                if (string.IsNullOrEmpty(value))
                    return false;

                return Convert.ToBoolean(value);
            }
            set => EditorUserSettings.SetConfigValue(k_EditorExtensionModeKey, value.ToString());
        }

        internal static void Reset()
        {
            EditorUserSettings.SetConfigValue(k_EditorExtensionModeKey, null);
        }
    }
}
