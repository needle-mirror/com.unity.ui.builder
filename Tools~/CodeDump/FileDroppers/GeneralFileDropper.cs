namespace CodeDump
{
    public class GeneralFileDropper : FileDropper
    {
        public GeneralFileDropper(IConfig config, IPathsConverter pathsConverter)
            : base(config, pathsConverter)
        {
        }

        protected override bool ShouldCopyMetaFile()
        {
            return false;
        }
    }
}
