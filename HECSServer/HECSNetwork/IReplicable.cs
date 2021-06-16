namespace HECSServer.HECSNetwork
{
    public interface IReplicable
    {
        int LevelOfReplication { get; }
    }

    public interface INotReplicable
    {
    }
}
