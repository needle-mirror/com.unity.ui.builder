using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace CodeDump
{
    internal class BlackListDefinition
    {
        [JsonProperty] public string name;
        [JsonProperty] public bool isDir = false;
        [JsonProperty] public bool isInCore = false;
        [JsonProperty] public bool endWith = false;
    }

    public static class BlackList
    {
        private static HashSet<string> s_DirBlacklist;
        private static HashSet<string> s_FileBlacklist;
        private static List<string> s_EndWith;

        public static void Initialize(string inputFile, IConfig options)
        {
            if (!File.Exists(inputFile))
                throw new Exception("Input file not found");

            var json = File.ReadAllText(inputFile);

            var list = JsonConvert.DeserializeObject<List<BlackListDefinition>>(json);
            InitializeMembers();

            foreach (var elm in list)
            {
                var fullName = elm.isInCore
                    ? Path.Combine(options.coreRootPath, elm.name)
                    : Path.Combine(options.packageRootPath, elm.name);

                if (elm.endWith)
                {
                    s_EndWith.Add(elm.name);
                }
                else if (elm.isDir)
                {
                    s_DirBlacklist.Add(fullName);
                    s_DirBlacklist.Add(fullName + ".meta");
                }
                else
                {
                    s_FileBlacklist.Add(fullName);
                    s_FileBlacklist.Add(fullName + ".meta");
                }
            }
        }

        public static bool IsFileBlackListed(string fileName)
        {
            return s_FileBlacklist.Contains(fileName) || IsEndingWith(fileName);
        }

        public static bool IsDirBlackListed(string dirName)
        {
            return s_DirBlacklist.Contains(dirName) || IsEndingWith(dirName);
        }

        private static bool IsEndingWith(string name)
        {
            foreach (var ending in s_EndWith)
            {
                if (name.EndsWith(ending))
                    return true;
            }

            return false;
        }

        private static void InitializeMembers()
        {
            s_DirBlacklist = new HashSet<string>();
            s_FileBlacklist = new HashSet<string>();
            s_EndWith = new List<string>();
        }
    }
}
