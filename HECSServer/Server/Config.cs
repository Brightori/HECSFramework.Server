using HECSFramework.Core;
using HECSFramework.Core.Generator;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace HECSFramework.Server
{
    [DataContract, Serializable]
    public partial class Config
    {
        [DataMember] public int ServerTickMilliseconds { get; private set; } = 100;
        [DataMember] public string ServerName { get; private set; } = "HECSServer";
        [DataMember] public string ServerPassword { get; private set; } = "ClausUmbrella";
        [DataMember] public bool DebugLogLevelEnabled { get; private set; } = false;
        [DataMember] public bool StatisticsEnabled { get; private set; } = false;
        [DataMember] public bool ExtendedStatisticsEnabled { get; private set; } = false;
        [DataMember] public int StatisticsLoggingIntervalMilliseconds { get; private set; } = 10000;
        [DataMember] public int DisconnectTimeOut { get; private set; } = 6000;

        public static Config Instance => lazy.Value;
        private static Lazy<Config> lazy = new Lazy<Config>(() => new Config());

        private Config()
        {
        }

        public static Config Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "config.json");
            var path2 = Path.Combine(AppContext.BaseDirectory, "ProjectConfig.cs");
            
            if (!File.Exists(path))
            {
                HECSDebug.Log($"Config file not found at path: {path}.");
                SaveToFile();
                return new Config();
            }

            if (!File.Exists(path2))
            {
                File.WriteAllText(path2, GetProjectPart());
            }
            
            var text = File.ReadAllText(path);
            var loaded = JsonConvert.DeserializeObject<Config>(text);
            //lazy = new Lazy<Config>(loaded);
            return loaded;
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