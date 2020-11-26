namespace CodeDump
{
    public class ProjectFileDropper : FileDropper
    {
        private static readonly string k_PackageName = "com.unity.ui";

        public ProjectFileDropper(IConfig config, IPathsConverter pathsConverter)
            : base(config, pathsConverter)
        {
        }

        protected override void UpdateLineForCoreDestination(ref string line)
        {
            //TODO modify the manifest by serializing it, modifying it and then deserializing it.
            // if (line.Contains(k_PackageName))
            //     line = "";
        }

        protected override void UpdateLineForPackageDestination(ref string line)
        {
            //Sync from core to package for manifest.json is not supported.
        }

        protected override bool ShouldModifyFile(string srcFileName)
        {
            return false;
            // return srcFileName.EndsWith("manifest.json");
        }
    }
}
