using Commands;
using HECSFramework.Core;
using HECSFramework.Server;

namespace Systems
{
    public partial class DataSenderSystem
    {
        partial void PopulateAliveConnections()
        {
            aliveConnections.Clear();
            foreach (var kvp in connectionsHolder.ClientConnectionsGUID)
            {
                if (!EntityManager.TryGetEntityByID(kvp.Key, out var client) || !client.IsAlive || !client.GetClientTagComponent().IsReadyToSync) 
                    continue;

                aliveConnections.Add((kvp.Key, kvp.Value));
            }
        }

        partial void ComponentStatistics(IComponent component)
        {
            if (Config.Instance.StatisticsEnabled)
                EntityManager.Command(new StatisticsCommand { StatisticsType = StatisticsType.ComponentSent, Value = component.GetType().ToString() });
        }

        partial void CommandStatistics<T>(T command)
        {
            if (Config.Instance.StatisticsEnabled)
                EntityManager.Command(new StatisticsCommand { StatisticsType = StatisticsType.CommandSent, Value = command.GetType().ToString() });
        }
    }
}