using Commands;
using Components;
using HECSFramework.Core;

namespace Systems
{
    public class RegisterClientEntitySystem : BaseSystem, IEntitySystem, IReactGlobalCommand<RegisterClientEntityOnServerCommand>
    {
        public void CommandGlobalReact(RegisterClientEntityOnServerCommand command)
        {
            var entity = command.Entity.GetEntityFromResolver();
            var clientHolder = entity.GetOrAddComponent<ClientIDHolderComponent>();
            clientHolder.ClientID = command.ClientGuid;
            entity.Init();

            Owner.GetSyncEntitiesHolderComponent().AddEntity(entity);

            if (EntityManager.TryGetEntityByID(command.ClientGuid, out var client))
            {
                client.GetWorldSliceIndexComponent().EntitiesOnClient.Add(entity.GUID);
            }

            HECSDebug.Log($"Spawn entity received {entity.GUID} from client {command.ClientGuid} ");
        }

        public override void InitSystem()
        {
        }
    }

    public interface IEntitySystem
    {
    }
}
