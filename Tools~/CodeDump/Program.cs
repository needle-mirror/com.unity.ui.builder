using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace CodeDump
{
    public interface IConfig
    {
        string packageRootPath { get; set; }
        string coreRootPath { get; set; }
        bool silent { get; set; }
        bool syncCoreToPackage { get; set; }
        bool validate { get; set; }
    }
    public class Options : IConfig
    {
        [Option('p', "package-path", Required = false, HelpText = "Root path to package repository.")]
        public string packageRootPath { get; set; }

        [Option('c', "core-path", Required = false, HelpText = "Root path to Unity core repository.")]
        public string coreRootPath { get; set; }

        [Option("silent", Required = false, HelpText = "Runs the script without logs.")]
        public bool silent { get; set; }

        [Option('v', "validate-hash", Required = false, HelpText = "Verify files have not change since last code drop.")]
        public bool validate { get; set; }

        [Option('s', "sync-core-to-package", Required = false, HelpText = "When set to true, the script will copy files from core to the package.")]
        public bool syncCoreToPackage { get; set; }
    }


    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                var externalFolderPath = Path.Combine(o.coreRootPath, "External/MirroredPackageSources/com.unity.ui.builder/");
                var jsonRootDir = Path.Combine(Directory.GetCurrentDirectory(), "JsonFiles");

                BlackList.Initialize(Path.Combine(jsonRootDir, "blacklist.json"), o);

                var generalPathsConverter = new PathsConverter(Path.Combine(jsonRootDir, "general.json"));
                var shaderPathsConverter = new ShaderPathConverter(Path.Combine(jsonRootDir, "shaders.json"));
                var projectPathsConverter = new PathsConverter(Path.Combine(jsonRootDir, "projects.json"));
                var resourcesPathsConverter = new PathsConverter(Path.Combine(jsonRootDir, "resources.json"));

                var pathConverterList = new List<IPathsConverter>()
                {generalPathsConverter, shaderPathsConverter, projectPathsConverter};

                var hashValidator = new HashValidator(o, externalFolderPath);
                if (o.validate && !o.syncCoreToPackage && !hashValidator.IsHashValid(pathConverterList))
                    throw new Exception("Some files have change since last code drop");

                var fileDropperList = new List<FileDropper>()
                {
                    new GeneralFileDropper(o, generalPathsConverter),
                    new ShaderFileDropper(o, shaderPathsConverter),
                    new ProjectFileDropper(o, projectPathsConverter),
                    new ResourcesFileDropper(o, resourcesPathsConverter)
                };

                foreach (var fileDropper in fileDropperList)
                    fileDropper.Run();

                if (!o.syncCoreToPackage)
                    hashValidator.RegenerateHashes(pathConverterList);
            });
        }
    }
}
