using HECSFramework.Core;

namespace Components
{
    [Documentation(Doc.Server, Doc.HECS, "This component contains info like parent server world and etc")]
    public sealed class RoomInfoComponent : BaseComponent, IDisposable
    {
        public World ServerWorld;

        public void Dispose()
        {
            if (ServerWorld != null)
            {
                ServerWorld.GetSingleComponent<ServerWorldComponent>().RemoveRoomWorld(Owner.World);
            }
        }
    }
}