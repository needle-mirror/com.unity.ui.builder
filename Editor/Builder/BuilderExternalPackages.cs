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
#if PACKAGE_2D_SPRITE_EDITOR && !UNITY_2019_4 && !UNITY_2020_1 && !UNITY_2020_2 && !UNITY_2020_3
                return true;
#else
                return false;
#endif
            }
        }

        public static void Open2DSpriteEditor(Object value)
        {
#if PACKAGE_2D_SPRITE_EDITOR && !UNITY_2019_4 && !UNITY_2020_1 && !UNITY_2020_2 && !UNITY_2020_3
            SpriteUtilityWindow.ShowSpriteEditorWindow(value);
#endif
        }
    }
}
