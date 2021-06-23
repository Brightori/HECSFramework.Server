using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Systems;

namespace HECSFramework.Server
{
    [DataContract, Serializable]
    internal class Config
    {
        [DataMember] public int ServerTickMilliseconds { get; private set; } = 50;
        [DataMember] public StatisticsData StatisticsData { get; private set; }
        [DataMember] public ChannelData ChannelData { get; private set; }
        [DataMember] public AuthorizationData AuthorizationData { get; private set; }
        [DataMember] public RigidbodyData RigidbodyData { get; private set; } 
        [DataMember] public bool DebugLogLevelEnabled { get; private set; } 
        
        public static Config Instance => lazy.Value;
        private static Lazy<Config> lazy = new Lazy<Config>(() => new Config());

        private Config()
        {
        }

        public static void Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (!File.Exists(path))
            {
                Debug.Log($"Config file not found at path: {path}.");
                return;
            }
            
            var text = File.ReadAllText(path);
            var loaded = JsonConvert.DeserializeObject<Config>(text);
            lazy = new Lazy<Config>(loaded);
        }

        public static void SaveToFile()
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "config.json"), JsonConvert.SerializeObject(Instance));
        }
    }

    [DataContract, Serializable]
    internal class ChannelData
    {
        [DataMember] private static readonly IReadOnlyDictionary<int, float> ChannelRadius =
            new Dictionary<int, float>
            {
                {0, 0},
                {1, 5},
                {2, 6},
                {3, 7},
                {4, 8},
                {5, 9}
            };
        
        [DataMember] public bool AgoraEnabled { get; private set; } = true;

        public float ConnectRadius(int usersCount)
            => ChannelRadius.TryGetValue(usersCount, out float value) ? value : ChannelRadius.Last().Value;
    }

    [DataContract, Serializable]
    internal class AuthorizationData
    {
        [DataMember] 
        public IReadOnlyCollection<string> SpeakerPasswords { get; private set; } = new HashSet<string>
        {
            "0846",
            "ilovekefir",
            "159753",
            "8844"
        };
    }
    
    [DataContract, Serializable]
    internal class RigidbodyData
    {
        [DataMember]
        public float KeepOwnershipDistanceThreshold { get; private set; }
        [DataMember]
        public float ConnectedClientDelaySeconds { get; private set; }
        [DataMember]
        public float OwnershipChangeDelaySeconds { get; private set; }
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
