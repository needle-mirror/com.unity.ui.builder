using UnityEditor;
using UnityEngine;

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
#else
                return false;
#endif
            }
        }

        public static void Open2DSpriteEditor(Object value)
        {
#if PACKAGE_2D_SPRITE_EDITOR && (!UI_BUILDER_PACKAGE || UNITY_2021_1_OR_NEWER)
            SpriteUtilityWindow.ShowSpriteEditorWindow(value);
#endif
        }
    }
}
