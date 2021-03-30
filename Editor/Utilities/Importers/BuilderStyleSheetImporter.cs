using UnityEditor;

#if UNITY_2019_4 || UNITY_2020_1
using UnityEditor.StyleSheets;
#else
using UnityEditor.UIElements.StyleSheets;
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
            return BuilderPackageUtilities.LoadAssetAtPath<UnityEngine.Object>(path);
        }
    }
}
