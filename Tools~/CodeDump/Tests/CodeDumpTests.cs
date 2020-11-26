using System;
using System.IO;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace CodeDump.Tests
{
    [TestFixture]
    public class CodeDumpTests
    {
        private Options options;
        private string currentDir;
        private ShaderPathConverter shaderPathConverter;
        private PathsConverter projectPathConverter;
        private List<IPathsConverter> pathConverterList;

        [SetUp]
        public void Setup()
        {
            //TODO Change to the absolute path of the Tests folder
            currentDir = "/Users/hugob/Documents/Git/Packages/" + "com.unity.ui/Tools/CodeDump/Tests";

            var packageRoot = Path.Combine(currentDir, "PackageTestDirectory");
            var coreRootPath = Path.Combine(currentDir, "CoreTestDirectory");
            options = new Options {silent = true, packageRootPath = packageRoot, coreRootPath = coreRootPath};

            BlackList.Initialize(Path.Combine(currentDir, "blacklist.json"), options);

            shaderPathConverter = new ShaderPathConverter(Path.Combine(currentDir, "package-to-core-shader-tests.json"));
            projectPathConverter = new PathsConverter(Path.Combine(currentDir, "package-to-core-projects-tests.json"));
            pathConverterList = new List<IPathsConverter>() {projectPathConverter, shaderPathConverter};
        }

        [TestCase]
        public void IsCurrentDirPathValid()
        {
            // If false, change currentDir
            Assert.IsTrue(Directory.Exists(currentDir));
        }

        [TestCase]
        public void BlacklistWorksWithEndWith()
        {
            var projectFileDropperPackageToCore = new ProjectFileDropper(options, projectPathConverter);
            projectFileDropperPackageToCore.Run();

            var metaFilePath = Path.Combine(options.coreRootPath, "CoreDirectory/DeepDirectory/ShouldNotBeCopied.cs.meta");
            Assert.IsFalse(File.Exists(metaFilePath));
        }

        [TestCase]
        public void TestGeneralFileDropperCoreToPackage()
        {
            var projectFileDropperPackageToCore = new ProjectFileDropper(options, projectPathConverter);
            projectFileDropperPackageToCore.Run();

            // Directory.Delete(Path.Combine(options.packageRootPath, "PackageDirectory"), true);

            var opt = options;
            opt.syncCoreToPackage = true;
            var projectFileDropperCoreToPackage = new ProjectFileDropper(opt, projectPathConverter);
            projectFileDropperCoreToPackage.Run();

            var testClassDestPath = Path.Combine(options.packageRootPath, "PackageDirectory/Test.cs");
            Assert.IsTrue(File.Exists(testClassDestPath));

            var testDeepClassDestPath = Path.Combine(options.packageRootPath, "PackageDirectory/DeepDirectory/DeepFile.cs");
            Assert.IsTrue(File.Exists(testDeepClassDestPath));

            var manifestPath = Path.Combine(options.packageRootPath, "PackageDirectory/manifest.json");
            Assert.IsTrue(File.Exists(manifestPath));
        }

        [TestCase]
        public void TestGeneralFileDropperPackageToCore()
        {
            // Directory.Delete(Path.Combine(options.coreRootPath, "CoreDirectory"), true);

            var projectFileDropper = new ProjectFileDropper(options, projectPathConverter);
            projectFileDropper.Run();

            var testClassDestPath = Path.Combine(options.coreRootPath, "CoreDirectory/Test.cs");
            Assert.IsTrue(File.Exists(testClassDestPath));

            var testDeepClassDestPath = Path.Combine(options.coreRootPath, "CoreDirectory/DeepDirectory/DeepFile.cs");
            Assert.IsTrue(File.Exists(testDeepClassDestPath));

            var manifestPath = Path.Combine(options.coreRootPath, "CoreDirectory/manifest.json");
            Assert.IsTrue(File.Exists(manifestPath));
            Assert.IsFalse(IsPackageInManifest(manifestPath, "com.unity.ui"));
        }

        [TestCase]
        public void TestShaderFileDropperPackageToCore()
        {
            // Directory.Delete(Path.Combine(options.coreRootPath, "Shaders"), true);
            var shaderFileDropper = new ShaderFileDropper(options, shaderPathConverter);
            shaderFileDropper.Run();

            var destShaderFile = Path.Combine(options.coreRootPath, "Shaders/UIE-Runtime.shader");
            Assert.IsTrue(File.Exists(destShaderFile));
            Assert.IsTrue(DidShaderChangeName(shaderPathConverter));
        }

        [TestCase]
        public void TestShaderFileDropperCoreToPackage()
        {
            var shaderFileDropper = new ShaderFileDropper(options, shaderPathConverter);
            shaderFileDropper.Run();

            // Directory.Delete(Path.Combine(options.packageRootPath, "Shaders"), true);

            var opt = options;
            opt.syncCoreToPackage = true;
            var shaderFileDropperCoreToPackage = new ShaderFileDropper(opt, shaderPathConverter);
            shaderFileDropperCoreToPackage.Run();

            var packageShaderPath = Path.Combine(options.packageRootPath, "Shaders/UIE-Runtime.shader");
            Assert.IsTrue(File.Exists(packageShaderPath));

            var line = File.ReadAllLines(packageShaderPath).FirstOrDefault();
            Assert.IsTrue(line.Contains("PackageName"));
        }

        [TestCase]
        public void HashValidatorShouldBeDeterministic()
        {
            var rootPath = Path.Combine(currentDir, "CoreTestDirectory");
            var hashValidator = new HashValidator(options, rootPath);

            hashValidator.RegenerateHashes(pathConverterList);

            Assert.IsTrue(hashValidator.IsHashValid(pathConverterList));
        }

        private bool DidShaderChangeName(ShaderPathConverter spc)
        {
            foreach (var correspondingFilePath in spc.GetCorrespondingPaths())
            {
                var shaderCorrespondingFilePath = correspondingFilePath as ShaderCorrespondingPath;
                var filePath = Path.Combine(options.coreRootPath, shaderCorrespondingFilePath.GetCorePath());

                using (var input = File.OpenText(filePath))
                {
                    string line = input.ReadLine();
                    if (!line.Contains(shaderCorrespondingFilePath.GetCoreShaderName())
                        || line.Contains(shaderCorrespondingFilePath.GetPackageShaderName()))
                        return false;
                }
            }

            return true;
        }

        private bool IsPackageInManifest(string srcFile, string packageName)
        {
            using (var input = File.OpenText(srcFile))
            {
                string line;
                while (null != (line = input.ReadLine()))
                    if (line.Contains(packageName))
                        return true;
            }

            return false;
        }
    }
}
