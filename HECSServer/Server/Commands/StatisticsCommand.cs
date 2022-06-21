using HECSFramework.Core;

namespace Commands
{
    public struct StatisticsCommand : IGlobalCommand
    {
        public StatisticsType StatisticsType { get; set; }
        public string Value { get; set; }
    }
    
    public struct RawStatisticsCommand : IGlobalCommand
    {
        public ResolverDataContainer ResolverDataContainer { get; set; }
    }
    
    public enum StatisticsType {Undefined = 0, CommandSent = 1, CommandReceived = 2, ComponentSent = 3, ComponentReceived = 4}
}