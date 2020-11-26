using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CodeDump
{
    public abstract class FileDropper
    {
        protected IConfig config;
        protected IPathsConverter pathsConverter;

        protected FileDropper(IConfig config, IPathsConverter pathsConverter)
        {
            this.config = config;
            this.pathsConverter = pathsConverter;
        }

        public virtual void Run()
        {
            foreach (var correspondingPath in pathsConverter.GetCorrespondingPaths())
                CopyCorrespondingPath(correspondingPath);
        }

        protected void CopyCorrespondingPath(ICorrespondingPath correspondingPath)
        {
            var sourceFullPath = config.syncCoreToPackage
                ? Path.Combine(config.coreRootPath, correspondingPath.GetCorePath())
                : Path.Combine(config.packageRootPath, correspondingPath.GetPackagePath());
            var destFullPath = config.syncCoreToPackage
                ? Path.Combine(config.packageRootPath, correspondingPath.GetPackagePath())
                : Path.Combine(config.coreRootPath, correspondingPath.GetCorePath());

            if (correspondingPath.GetIsDir())
                DirectoryCopy(sourceFullPath, destFullPath, correspondingPath, correspondingPath.GetIsRecursive());
            else
            {
                FileCopy(new FileInfo(sourceFullPath) , destFullPath, correspondingPath);
                //var metaFileSrcPath = sourceFullPath + ".meta";
                //if (!sourceFullPath.EndsWith(".cs.meta") && File.Exists(metaFileSrcPath))
                    //FileCopy(new FileInfo(metaFileSrcPath), destFullPath + ".meta", correspondingPath);
            }

            if (!config.silent)
                Console.WriteLine($"Successfully copied: {sourceFullPath}");
        }

        protected virtual void UpdateLineForCoreDestination(ref string line) {}

        protected virtual void UpdateLineForPackageDestination(ref string line) {}

        protected virtual bool ShouldModifyFile(string srcFileName)
        {
            return false;
        }

        protected virtual bool ShouldCopyMetaFile()
        {
            return true;
        }

        protected void DirectoryCopy(string sourceDirName, string destDirName, ICorrespondingPath correspondingPath, bool isRecursive = true)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    $"Source directory does not exist or could not be found: {sourceDirName}");
            }

            if (BlackList.IsDirBlackListed(dir.FullName))
                return;

            DirectoryInfo[] dirs = dir.GetDirectories();

            if (Directory.Exists(destDirName))
                ClearDirectory(destDirName);

            if (isRecursive)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, correspondingPath);
                }
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
                FileCopy(file, Path.Combine(destDirName, file.Name), correspondingPath);

            if (isRecursive)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryInfo tempdir = new DirectoryInfo(temppath);

                    if (Directory.Exists(destDirName) && tempdir.GetFileSystemInfos().Length == 0)
                    {
                        Directory.Delete(temppath, true);
                        Console.WriteLine($"Cleaned up empty folder:       {temppath}");

                        var dirmeta = temppath + ".meta";
                        if (File.Exists(dirmeta))
                        {
                            File.Delete(dirmeta);
                            Console.WriteLine($"Cleaned up empty folder .meta: {dirmeta}");
                        }
                    }
                }
            }
        }

        protected bool FileCopy(FileInfo srcFile, string destFullPath, ICorrespondingPath correspondingPath)
        {
            var directoryName = Path.GetDirectoryName(destFullPath);
            Directory.CreateDirectory(directoryName);

            if (BlackList.IsFileBlackListed(srcFile.FullName))
                return false;

            var ignoreExtension = correspondingPath.GetIgnoreExtension();
            if (!string.IsNullOrEmpty(ignoreExtension) && (srcFile.FullName.EndsWith(ignoreExtension) || srcFile.FullName.EndsWith(ignoreExtension + ".meta")))
                return false;
            var ignoreExtensions = correspondingPath.GetIgnoreExtensions();
            foreach (var ext in ignoreExtensions)
                if (!string.IsNullOrEmpty(ext) && (srcFile.FullName.EndsWith(ext) || srcFile.FullName.EndsWith(ext + ".meta")))
                    return false;

            if (srcFile.FullName.EndsWith(".meta") && !ShouldCopyMetaFile())
                return false;

            if (ShouldModifyFile(srcFile.Name))
                FileCopyAndModify(srcFile, destFullPath);
            else if (srcFile.FullName.EndsWith(".cs") || srcFile.FullName.EndsWith(".uss") || srcFile.FullName.EndsWith(".uxml") || srcFile.FullName.EndsWith(".meta"))
                FileCopy(srcFile, destFullPath);
            else
                srcFile.CopyTo(destFullPath, true);

            return true;
        }

        private void FileCopyAndModify(FileInfo srcFile, string destFullPath)
        {
            using (var input = File.OpenText(srcFile.FullName))
            using (var output = new StreamWriter(destFullPath, false))
            {
                output.NewLine = "\n";
                string line;
                while (null != (line = input.ReadLine()))
                {
                    var unmodifiedLine = line;

                    if (config.syncCoreToPackage)
                        UpdateLineForPackageDestination(ref line);
                    else
                        UpdateLineForCoreDestination(ref line);

                    if (line.Length <= 0 && unmodifiedLine.Length > 0)
                        continue;

                    output.WriteLine(line);
                }
            }
        }

        private void FileCopy(FileInfo srcFile, string destFullPath)
        {
            using (var input = File.OpenText(srcFile.FullName))
            using (var output = new StreamWriter(destFullPath, false))
            {
                output.NewLine = "\n";
                string line;
                while (null != (line = input.ReadLine()))
                {
                    output.WriteLine(line);
                }
            }
        }

        public static void ClearDirectory(string dirFullPath)
        {
            if (!Directory.Exists(dirFullPath))
                throw new DirectoryNotFoundException(
                    $"Source directory does not exist or could not be found: {dirFullPath}");

            var directory = new DirectoryInfo(dirFullPath);
            foreach (var file in directory.EnumerateFiles())
            {
                if (!BlackList.IsFileBlackListed(file.FullName))
                    file.Delete();
            }

            foreach (var dir in directory.EnumerateDirectories())
            {
                if (!BlackList.IsFileBlackListed(dir.FullName))
                    ClearDirectory(dir.FullName);
            }

            if (directory.GetFiles().Length == 0 && directory.GetDirectories().Length == 0)
                directory.Delete();
        }
    }
}
