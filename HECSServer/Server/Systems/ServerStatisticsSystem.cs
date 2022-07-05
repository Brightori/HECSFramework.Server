using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Server;
using Helpers;

namespace Systems
{
    public class ServerStatisticsSystem : BaseSystem, IServerStatisticsSystem
    {
        private readonly ServerStatisticsInfo statistics = new ServerStatisticsInfo();
        private ConnectionsHolderComponent connections;
        private NetworkClientHolderComponent networkClient;
        private DateTime lastUpdateTime;

        public Queue<string> Snapshots { get; } = new Queue<string>();

        public override void InitSystem()
        {
            connections = EntityManager.GetSingleComponent<ConnectionsHolderComponent>();
            networkClient = EntityManager.GetSingleComponent<NetworkClientHolderComponent>();
        }

        public void CommandGlobalReact(StatisticsCommand command)
        {
            statistics.Add(command);
        }

        public void UpdateLocal()
        {
            statistics.Update();
            if (DateTime.Now - lastUpdateTime < TimeSpan.FromMilliseconds(Config.Instance.StatisticsLoggingIntervalMilliseconds))
                return;
            
            lastUpdateTime = DateTime.Now;
            var snapshot = CollectStatistics();
            Snapshots.Enqueue(snapshot);
            HECSDebug.LogDebug(snapshot);
            Clear();
        }

        private void Clear()
        {
            statistics.Reset();
            while (Snapshots.Count > 100) 
                Snapshots.Dequeue();
        }

        private string CollectStatistics()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0}Server Info: {1:hh:mm:ss}{2}", Environment.NewLine, DateTime.Now, Environment.NewLine);
            builder.Append(statistics);
            WriteNetworkData(builder);
            WriteNetManagerData(builder);
            return builder.ToString();
        }

        private void WriteNetworkData(StringBuilder builder)
        {
            builder.AppendFormat("Peers: {0}{1}", connections.ClientConnectionsGUID.Count, Environment.NewLine);
            // TODO: количество нетворк компонентов
            // TODO: вотчи для замера длительности операций
        }

        private void WriteNetManagerData(StringBuilder builder)
        {
            if (connections.ClientConnectionsGUID.Count == 0) return;

            builder.AppendFormat("NetManager: Sent: {0} packets, {1:0.00}kb | Received: {2} packets, {3:0.00}kb{4}",
                networkClient.Manager.Statistics.PacketsSent,
                networkClient.Manager.Statistics.BytesSent/1024f,
                networkClient.Manager.Statistics.PacketsReceived,
                networkClient.Manager.Statistics.BytesReceived/1024f,
                Environment.NewLine);

            builder.AppendFormat("{0}{1}", GetQueuesData(), Environment.NewLine);
            
            if (!Config.Instance.ExtendedStatisticsEnabled) return;
            var peersInfo = string.Join(" | ", connections.ClientConnectionsGUID.Select(a => $"{a.Key}: {a.Value.Statistics.PacketsSent} packets, {a.Value.Statistics.BytesSent / 1024f:0.00}kb"));
            builder.AppendFormat("User info sent: {0}{1}", peersInfo, Environment.NewLine);

            foreach (var kvp in connections.ClientConnectionsGUID) kvp.Value.NetManager.Statistics.Reset();
            networkClient.Manager.Statistics.Reset();
        }

        //todo тут изменеи
        private string GetQueuesData()
        {
            //var peers = connections.NetManager.ConnectedPeerList;
            //var channels = peers.SelectMany(a => a.).Where(a => a != null).Distinct().Select(a => a.PacketsInQueue).OrderBy(a => a);
            //return $"Packets in channel queues: {string.Join(", ", channels)}";
            return string.Empty;
        }
        
        public void CommandGlobalReact(RawStatisticsCommand command)
        {
            switch (command.ResolverDataContainer.Type)
            {
                case 0:
                    TypesMap.GetComponentInfo(command.ResolverDataContainer.TypeHashCode, out var info);
                    EntityManager.Command(new StatisticsCommand{StatisticsType = StatisticsType.ComponentReceived, Value = info.ComponentName});
                    break;
                case 1:
                    var system = TypesMap.GetSystemFromFactory(command.ResolverDataContainer.TypeHashCode);
                    EntityManager.Command(new StatisticsCommand{StatisticsType = StatisticsType.ComponentReceived, Value = system.GetType().Name});
                    break;
                case 2:
                    EntityManager.Command(new StatisticsCommand{StatisticsType = StatisticsType.CommandReceived, Value = EntityManager.ResolversMap.GetCommandName(command.ResolverDataContainer)});
                    break;
            }
        }
    }

    public interface IServerStatisticsSystem : ISystem, IUpdatable, IReactGlobalCommand<StatisticsCommand>, IReactGlobalCommand<RawStatisticsCommand>
    {
        Queue<string> Snapshots { get; }
    }
}
