namespace Unity.UI.Builder
{
    static class BuilderExternalPackages
    {
        public static bool isVectorGraphicsInstalled
        {
            get
            {
#if VECTOR_GRAPHICS
                return true;
#else
                return false;
#endif
            }
        }
    }
}
