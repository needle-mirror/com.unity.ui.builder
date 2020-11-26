namespace CodeDump
{
    public class ResourcesFileDropper : FileDropper
    {
        internal static readonly string k_PackageResourcesPackagePath = "com.unity.ui.builder/";
        internal static readonly string k_PackageResourcesCorePath = "UIBuilderPackageResources/";

        public ResourcesFileDropper(IConfig config, IPathsConverter pathsConverter)
            : base(config, pathsConverter)
        {
        }

        protected override void UpdateLineForCoreDestination(ref string line)
        {
            var toReplace = $"resource(\"{k_PackageResourcesPackagePath}";
            var replaceBy = $"resource(\"{k_PackageResourcesCorePath}";
            if (line.Contains(toReplace))
            {
                line = line.Replace(toReplace, replaceBy);

                // Add extension.
                line = line.Replace("\");", ".png\");");
            }
        }

        protected override void UpdateLineForPackageDestination(ref string line)
        {
            var toReplace = $"resource(\"{k_PackageResourcesCorePath}";
            var replaceBy = $"resource(\"{k_PackageResourcesPackagePath}";
            if (line.Contains(toReplace))
                line = line.Replace(toReplace, replaceBy);
        }

        protected override bool ShouldModifyFile(string srcFileName)
        {
            return srcFileName.EndsWith(".uss");
        }
    }
}
