using UnityEditor;
using UnityEngine;
#if !UI_BUILDER_PACKAGE
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.Linq;
#endif

namespace Unity.UI.Builder
{
    static class BuilderExternalPackages
    {
        public static bool isVectorGraphicsInstalled
        {
            get
            {
#if PACKAGE_VECTOR_GRAPHICS
                return true;
#elif !UI_BUILDER_PACKAGE
                return PackageInfo.GetAllRegisteredPackages().Any(x => x.name == "com.unity.vectorgraphics" && x.version == "1.0.0");
#else
                return false;
#endif
            }
        }

        public static bool is2DSpriteEditorInstalled
        {
            get
            {
#if PACKAGE_2D_SPRITE_EDITOR && (!UI_BUILDER_PACKAGE || UNITY_2021_1_OR_NEWER)
                return true;
#elif !UI_BUILDER_PACKAGE
                return PackageInfo.GetAllRegisteredPackages().Any(x => x.name == "com.unity.2d.sprite" && x.version == "1.0.0");
#else
                return false;
#endif
            }
        }

        public static void Open2DSpriteEditor(Object value)
        {
#if !UI_BUILDER_PACKAGE || (PACKAGE_2D_SPRITE_EDITOR && UNITY_2021_1_OR_NEWER)
            SpriteUtilityWindow.ShowSpriteEditorWindow(value);
#endif
        }
    }
}
