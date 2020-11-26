using System;
using System.IO;

namespace CodeDump
{
    public class ShaderFileDropper : FileDropper
    {
        public ShaderFileDropper(IConfig config, IPathsConverter pathsConverter)
            : base(config, pathsConverter)
        {
        }

        private ShaderCorrespondingPath currentShaderCorrespondingPath;

        public override void Run()
        {
            foreach (var correspondingPath in pathsConverter.GetCorrespondingPaths())
            {
                currentShaderCorrespondingPath = correspondingPath as ShaderCorrespondingPath;
                if (currentShaderCorrespondingPath == null)
                    throw new Exception("Could not cast ICorrespondingPath to ShaderCorrespondingPath");

                CopyCorrespondingPath(correspondingPath);
            }
        }

        protected override void UpdateLineForPackageDestination(ref string line)
        {
            ReplaceShaderName(ref line, true);
            ReplaceIncludePath(ref line, true);
        }

        protected override void UpdateLineForCoreDestination(ref string line)
        {
            ReplaceShaderName(ref line, false);
            ReplaceIncludePath(ref line, false);
        }

        protected override bool ShouldModifyFile(string srcFileName)
        {
            return srcFileName.EndsWith(".shader") || srcFileName.EndsWith(".cginc");
        }

        private void ReplaceShaderName(ref string line, bool syncCoreToPackage)
        {
            var srcShaderName = syncCoreToPackage
                ? currentShaderCorrespondingPath.GetCoreShaderName()
                : currentShaderCorrespondingPath.GetPackageShaderName();


            var destShaderName = syncCoreToPackage
                ? currentShaderCorrespondingPath.GetPackageShaderName()
                : currentShaderCorrespondingPath.GetCoreShaderName();

            if (srcShaderName != null && line.Contains(srcShaderName))
                line = line.Replace(srcShaderName, destShaderName);
        }

        private void ReplaceIncludePath(ref string line, bool syncCoreToPackage)
        {
            foreach (var includePath in currentShaderCorrespondingPath.GetIncludePaths())
            {
                var srcPath = syncCoreToPackage
                    ? includePath.Item2
                    : includePath.Item1;


                var destPath = syncCoreToPackage
                    ? includePath.Item1
                    : includePath.Item2;

                if (srcPath != null && line.Contains(srcPath))
                {
                    line = line.Replace(srcPath, destPath);
                    return;
                }
            }
        }
    }
}
