using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Commands;
using HECSFramework.Core;
using HECSFramework.Server;

namespace Helpers
{
    public class ServerStatisticsInfo : IServerStatisticsInfo
    {
        private const string Indent = "    ";

        private readonly ConcurrencyList<string> componentsSent = new ConcurrencyList<string>();
        private readonly ConcurrencyList<string> componentsReceived = new ConcurrencyList<string>();
        private readonly ConcurrencyList<string> commandsSent = new ConcurrencyList<string>();
        private readonly ConcurrencyList<string> commandsReceived = new ConcurrencyList<string>();

        private (int previous, int top) componentsSentCount;
        private (int previous, int top) componentsReceivedCount;
        private (int previous, int top) commandsSentCount;
        private (int previous, int top) commandsReceivedCount;
        
        public void Add(StatisticsCommand command)
        {
            var value = RemoveNamespaces(command.Value);
            switch (command.StatisticsType)
            {
                case StatisticsType.CommandSent: 
                    commandsSent.Add(value);
                    break;
                case StatisticsType.CommandReceived: 
                    commandsReceived.Add(value);
                    break;
                case StatisticsType.ComponentSent: 
                    componentsSent.Add(value);
                    break;
                case StatisticsType.ComponentReceived: 
                    componentsReceived.Add(value);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public void Reset()
        {
            componentsSent.Clear();
            componentsReceived.Clear();
            commandsSent.Clear();
            commandsReceived.Clear();
            componentsSentCount = (0, 0);
            componentsReceivedCount = (0, 0);
            commandsSentCount = (0, 0);
            commandsReceivedCount = (0, 0);
        }

        public void Update()
        {
            UpdateCount(ref componentsSentCount, componentsSent);
            UpdateCount(ref componentsReceivedCount, componentsReceived);
            UpdateCount(ref commandsSentCount, commandsSent);
            UpdateCount(ref commandsReceivedCount, commandsReceived);
        }

        private static void UpdateCount(ref (int previous, int top) count, ConcurrencyList<string> source)
        {
            count.top = Math.Max(source.Count - count.previous, count.top);
            count.previous = source.Count;
        }

        private string RemoveNamespaces(string command)
            => command.Replace("Commands.", "")
                .Replace("HECSServer.ServerShared.", "")
                .Replace("HECSFrameWork.", "")
                .Replace("Resolver", "")
                .Replace("Components.", "");

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("Components:{0}", Environment.NewLine);
            AppendInfo(builder, componentsSent, "sent", componentsSentCount.top);
            AppendInfo(builder, componentsReceived, "received", componentsReceivedCount.top);
            builder.AppendFormat("Commands:{0}", Environment.NewLine);
            AppendInfo(builder, commandsSent, "sent", commandsSentCount.top);
            AppendInfo(builder, commandsReceived, "received", commandsReceivedCount.top);
            return builder.ToString();
        }

        private void AppendInfo(StringBuilder builder, ConcurrencyList<string> info, string option, int max)
            => builder.AppendFormat("{0}Total {5}: {1} | Average per tick: {2:0.00} | Max per tick: {6} | Top used: {3}{4}", Indent, info.Count, AvgPerTick(info.Count), TopUsed(info), Environment.NewLine, option, max);

        private  float AvgPerTick(int count)
        {
            var serverTicksBetweenLogging = (float) Config.Instance.StatisticsLoggingIntervalMilliseconds / Config.Instance.ServerTickMilliseconds;
            return count / serverTicksBetweenLogging;
        }

        private string TopUsed(ConcurrencyList<string> source)
            => string.Join(", ", source.ToArray().GroupBy(a => a).OrderByDescending(a => a.Count()).Take(3).Select(a => $"{a.Key}({a.Count()})"));
    }

    public interface IServerStatisticsInfo
    {
        void Add(StatisticsCommand command);
        void Reset();
        void Update();
    }
}