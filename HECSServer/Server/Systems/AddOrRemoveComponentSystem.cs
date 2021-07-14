using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Network;
using System;

namespace Systems
{
    public class AddOrRemoveComponentSystem : BaseSystem, 
        IReactGlobalCommand<AddOrRemoveComponentToServerCommand>,
        IReactGlobalCommand<AddNetWorkComponentCommand>,
        IReactGlobalCommand<RemoveNetWorkComponentCommand>
    {
        private ConnectionsHolderComponent connectionsHolderComponent;
        private IDataSenderSystem dataSenderSystem;

        public override void InitSystem()
        {
            Owner.TryGetHecsComponent(out connectionsHolderComponent);
            Owner.TryGetSystem(out dataSenderSystem);
        }

        public void CommandGlobalReact(AddOrRemoveComponentToServerCommand command)
        {
            if (EntityManager.TryGetEntityByID(command.Entity, out var entity))
            {
                var component = EntityManager.ResolversMap.GetComponentFromContainer(command.component);

                if (!TypesMap.GetComponentInfo(component.GetTypeHashCode, out var mask))
                    return;

                if (component == null)
                    return;

                if (command.IsAdded)
                {
                    entity.AddOrReplaceComponent(component);
                }
                else
                    entity.RemoveHecsComponent(mask.ComponentsMask);


                Debug.Log($"получили операцию по компоненту {command.IsAdded} {mask.ComponentName} для ентити {entity.GUID}  {entity.ID}");
            }
        }

        public void CommandGlobalReact(AddNetWorkComponentCommand command)
        {
            var component = command.Component;

            if (component is INetworkComponent networkComponent)
{
                foreach (var connect in connectionsHolderComponent.WorldToPeerClients[component.Owner.WorldId])
                {
                    dataSenderSystem.SendCommand(connect.Value, Guid.Empty, new AddOrRemoveComponentToServerCommand
                    {
                        component = EntityManager.ResolversMap.GetComponentContainer(component),
                        Entity = component.Owner.GUID,
                        IsAdded = true,
                    });
                }
            }
        }

        public void CommandGlobalReact(RemoveNetWorkComponentCommand command)
        {
            var component = command.Component;

            if (component is INetworkComponent networkComponent)
{
                foreach (var connect in connectionsHolderComponent.WorldToPeerClients[component.Owner.WorldId])
                {
                    dataSenderSystem.SendCommand(connect.Value, Guid.Empty, new AddOrRemoveComponentToServerCommand
                    {
                        component = EntityManager.ResolversMap.GetComponentContainer(component),
                        Entity = component.Owner.GUID,
                        IsAdded = true,
                    });
                }
            }
        }
    }
}
