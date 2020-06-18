using UnityEditor;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.UIElements.StyleSheets;
#else
using UnityEditor.StyleSheets;
#endif

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheetImporter : StyleSheetImporterImpl
    {
        public BuilderStyleSheetImporter()
        {

        }

        public override UnityEngine.Object DeclareDependencyAndLoad(string path)
        {
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }
    }
}