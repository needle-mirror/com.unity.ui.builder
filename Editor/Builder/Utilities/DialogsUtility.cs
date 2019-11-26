using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal static class DialogsUtility
    {
        public static bool DisplayDialog(string title, string message)
        {
            return DisplayDialog(title, message, BuilderConstants.DialogOkOption);
        }
        
        public static bool DisplayDialog(string title, string message, string ok)
        {
            return DisplayDialog(title, message, ok, string.Empty);
        }
        
        public static bool DisplayDialog(string title, string message, string ok, string cancel)
        {
            if (Application.isBatchMode)
                return true;

            return EditorUtility.DisplayDialog(title, message, ok, cancel);
        }
    }
}