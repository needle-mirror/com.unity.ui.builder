using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    class VariableInfo
    {
        public string name { get; set; }
        public StylePropertyValue value { get; set; }
#if !UI_BUILDER_PACKAGE || UNITY_2021_2_OR_NEWER
        public bool isEditorVar => value.sheet ? value.sheet.IsUnityEditorStyleSheet() : false;
#endif
        public string description { get; set; }
    }
}
