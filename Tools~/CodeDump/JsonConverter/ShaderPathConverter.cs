using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace CodeDump
{
    internal class ShaderCorrespondingPath : CorrespondingPath
    {
        [JsonProperty] private string PackageShaderName;
        [JsonProperty] private string CoreShaderName;
        [JsonProperty] private List<Tuple<string, string>> IncludePaths = new List<Tuple<string, string>>();

        public string GetPackageShaderName()
        {
            return PackageShaderName;
        }

        public string GetCoreShaderName()
        {
            return CoreShaderName;
        }

        public List<Tuple<string, string>> GetIncludePaths()
        {
            return IncludePaths;
        }
    }

    public class ShaderPathConverter : IPathsConverter
    {
        private List<ShaderCorrespondingPath> m_CorrespondingShaderPaths;

        public IList<ICorrespondingPath> GetCorrespondingPaths()
        {
            return m_CorrespondingShaderPaths.ToList<ICorrespondingPath>();
        }

        public ShaderPathConverter(string inputFile)
        {
            if (!File.Exists(inputFile))
                throw new Exception("Input file not found");

            var json = File.ReadAllText(inputFile);
            m_CorrespondingShaderPaths = JsonConvert.DeserializeObject<List<ShaderCorrespondingPath>>(json);
        }
    }
}
