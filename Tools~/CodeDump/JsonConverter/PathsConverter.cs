using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace CodeDump
{
    public interface ICorrespondingPath
    {
        string GetPackagePath();
        string GetCorePath();
        bool GetIsDir();
        bool GetIsRecursive();
        string GetIgnoreExtension();
        List<string> GetIgnoreExtensions();
    }
    public class CorrespondingPath : ICorrespondingPath
    {
        [JsonProperty] private string PackagePath;
        [JsonProperty] private string CorePath;
        [JsonProperty] private bool IsDir;
        [JsonProperty] private bool IsRecursive = true;
        [JsonProperty] private string IgnoreExtension = string.Empty;
        [JsonProperty] private List<string> IgnoreExtensions = new List<string>();

        public string GetPackagePath()
        {
            return PackagePath;
        }

        public string GetCorePath()
        {
            return CorePath;
        }

        public bool GetIsDir()
        {
            return IsDir;
        }

        public bool GetIsRecursive()
        {
            return IsRecursive;
        }

        public string GetIgnoreExtension()
        {
            return IgnoreExtension;
        }

        public List<string> GetIgnoreExtensions()
        {
            return IgnoreExtensions;
        }
    }

    public interface IPathsConverter
    {
        IList<ICorrespondingPath> GetCorrespondingPaths();
    }

    public class PathsConverter : IPathsConverter
    {
        private List<CorrespondingPath> m_CorrespondingPaths;

        public IList<ICorrespondingPath> GetCorrespondingPaths()
        {
            return m_CorrespondingPaths.ToList<ICorrespondingPath>();
        }

        public PathsConverter(string inputFile)
        {
            if (!File.Exists(inputFile))
                throw new Exception("Input file not found");

            var json = File.ReadAllText(inputFile);
            m_CorrespondingPaths = JsonConvert.DeserializeObject<List<CorrespondingPath>>(json);
        }
    }
}
