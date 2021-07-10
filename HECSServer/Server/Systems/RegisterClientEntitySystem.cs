using Commands;
using Components;
using HECSFramework.Core;
using System;
using System.Collections.Generic;
using System.Text;

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
            Debug.Log($"получили спаун ентити {entity.GUID} от клиента {command.ClientGuid} ");
        }

        public override void InitSystem()
        {
        }
    }

    public interface IEntitySystem
    {
    }
}
