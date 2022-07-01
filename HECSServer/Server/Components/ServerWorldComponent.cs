using Commands;
using HECSFramework.Core;
using HECSFramework.Core.Helpers;

namespace Components
{
    public sealed class ServerWorldComponent : BaseComponent, IInitable, IWorldSingleComponent
    {
        private List<World> rooms = new List<World>(8);
        public ReadonlyList<World> Rooms;

        public void Init()
        {
            Rooms = new ReadonlyList<World>(rooms);
        }

        public void AddRoomWorld(World room)
        {
            if (!room.TryGetSingleComponent<RoomInfoComponent>(out var roomInfoComponent))
            {
                var enity = new Entity("RoomInfo", room.Index);
                enity.AddHecsComponent(new RoomInfoComponent()).ServerWorld = Owner.World;
                enity.Init();
            }
            else
                roomInfoComponent.ServerWorld = Owner.World;

            lock (rooms)
            {
                rooms.AddOrRemoveElement(room, true);
            }

            Owner.World.Command(new AddOrRemoveNewWorldGlobalCommand { World = room, Add = true });
        }

        public void RemoveRoomWorld(World room)
        {
            lock (rooms)
            {
                rooms.AddOrRemoveElement(room, false);
            }

            Owner.World.Command(new AddOrRemoveNewWorldGlobalCommand { World = room, Add = false });
        }
    }
}
