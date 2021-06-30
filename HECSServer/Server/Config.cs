using HECSFramework.Core.Generator;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Systems;

namespace HECSFramework.Server
{
    [DataContract, Serializable]
    internal partial class Config
    {
        [DataMember] public int ServerTickMilliseconds { get; private set; } = 80;
        [DataMember] public string ServerName { get; private set; } = "HECSServer";
        [DataMember] public string ServerPassword { get; private set; } = "ClausUmbrella";
        [DataMember] public bool DebugLogLevelEnabled { get; private set; } = false;
        
        public static Config Instance => lazy.Value;
        private static Lazy<Config> lazy = new Lazy<Config>(() => new Config());

        private Config()
        {
        }

        public static void Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "config.json");
            var path2 = Path.Combine(AppContext.BaseDirectory, "ProjectConfig.cs");
            
            if (!File.Exists(path))
            {
                Debug.Log($"Config file not found at path: {path}.");
                SaveToFile();
                return;
            }

            if (!File.Exists(path2))
            {
                File.WriteAllText(path2, GetProjectPart());
            }
            
            var text = File.ReadAllText(path);
            var loaded = JsonConvert.DeserializeObject<Config>(text);
            lazy = new Lazy<Config>(loaded);
        }

        private static string GetProjectPart()
        {
            var tree = new TreeSyntaxNode();
            tree.Add(new NameSpaceSyntax("HECSFramework.Server"));
            tree.Add(new LeftScopeSyntax());
            tree.Add(new TabSimpleSyntax(1, "internal partial class Config"));
            tree.Add(new LeftScopeSyntax(1));
            tree.Add(new RightScopeSyntax(1));
            tree.Add(new RightScopeSyntax());
            return tree.ToString();
        }

        public static void SaveToFile()
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "config.json"), JsonConvert.SerializeObject(Instance));
        }
    }

    [DataContract, Serializable]
    internal class StatisticsData
    {
        [DataMember]
        public bool StatisticsEnabled { get; private set; }
        [DataMember]
        public bool ExtendedStatisticsEnabled { get; private set; }
        [DataMember]
        public int StatisticsLoggingIntervalMilliseconds { get; private set; }
    }
}
