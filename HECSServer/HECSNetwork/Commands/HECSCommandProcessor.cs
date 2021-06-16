namespace HECSServer.HECSNetwork
{
    public abstract class HECSCommandProcessor : IHECSCommandProcessor
    {
        public abstract void Process(HECSNetMessage message);
    }


    public interface IHECSCommandProcessor
    {
        void Process(HECSNetMessage message);
    }
}
