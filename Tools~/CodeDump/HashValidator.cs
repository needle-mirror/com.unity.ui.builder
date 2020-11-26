using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CodeDump
{
    public class HashValidator
    {
        private readonly string k_HashFileName = "hash.txt";
        private string m_HashRootDirPath;
        private string m_HashFileFullPath;
        private IConfig m_Config;

        public HashValidator(IConfig config, string hashRootDirPath)
        {
            m_HashRootDirPath = hashRootDirPath;
            m_Config = config;

            m_HashFileFullPath = Path.Combine(config.coreRootPath, m_HashRootDirPath, k_HashFileName);
        }

        public void RegenerateHashes(List<IPathsConverter> pathsConverters)
        {
            var md5 = GetMD5ForPaths(pathsConverters);
            File.WriteAllText(m_HashFileFullPath, md5);
        }

        public bool IsHashValid(List<IPathsConverter> pathsConverters)
        {
            if (!File.Exists(m_HashFileFullPath))
                throw new Exception("Hash file does not exist or could not be found. Please generate the hash file before running the script.");

            var md5 = GetMD5ForPaths(pathsConverters);
            var lastMd5 = File.ReadAllText(m_HashFileFullPath);

            return md5.Equals(lastMd5);
        }

        private string GetMD5ForPaths(List<IPathsConverter> pathsConverters)
        {
            var dirPaths = new List<string>();
            var filePaths = new List<string>();

            foreach (var pathsConverter in pathsConverters)
            {
                foreach (var correspondingPath in pathsConverter.GetCorrespondingPaths())
                {
                    var corePath = Path.Combine(m_Config.coreRootPath, correspondingPath.GetCorePath());
                    if (Directory.Exists(corePath))
                        dirPaths.Add(corePath);
                    else if (File.Exists(corePath))
                        filePaths.Add(corePath);
                    else
                        throw new Exception("Path is invalid, directory or file could not be found.");
                }
            }
            return GetMd5(filePaths, dirPaths);
        }

        private string GetMd5(List<string> filePaths, List<string> dirPaths)
        {
            foreach (var dirPath in dirPaths)
                filePaths.AddRange(Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories).ToList());

            filePaths.Sort();
            using (var md5 = MD5.Create())
            {
                foreach (var filePath in filePaths)
                    ComputeMd5ForFile(filePath, md5);

                md5.TransformFinalBlock(new byte[0], 0, 0);

                return NormalizeHash(md5.Hash);
            }
        }

        private void ComputeMd5ForFile(string filePath, MD5 md5)
        {
            if (filePath.EndsWith(k_HashFileName))
                return;

            // hash path
            string relativePath = filePath.Substring(m_Config.coreRootPath.Length + 1);
            byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath);
            md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

            // hash contents
            byte[] contentBytes = File.ReadAllBytes(filePath);

            md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
        }

        private string NormalizeHash(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
