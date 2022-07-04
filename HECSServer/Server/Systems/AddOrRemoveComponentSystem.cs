using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Network;

namespace Systems
{
    [Documentation(Doc.Network, Doc.Server, "This system add or remove ")]
    public sealed class AddOrRemoveNetworkComponentSystem : BaseSystem,
        IReactGlobalCommand<AddNetWorkComponentCommand>,
        IReactGlobalCommand<RemoveNetWorkComponentCommand>
    {
        private ConnectionsHolderComponent connectionsHolderComponent;
        private DataSenderSystem dataSenderSystem;

        public override void InitSystem()
        {
            dataSenderSystem = EntityManager.GetSingleSystem<DataSenderSystem>();
            connectionsHolderComponent = EntityManager.GetSingleComponent<ConnectionsHolderComponent>();
        }

        public void CommandGlobalReact(AddNetWorkComponentCommand command)
        {
            var component = command.Component;

            if (component is INetworkComponent networkComponent && networkComponent.IsAlive)
            {
                var commandToNetwork = new AddedComponentOnServerCommand
                {
                    component = EntityManager.ResolversMap.GetComponentContainer(component),
                    Entity = component.Owner.GUID,
                };

                dataSenderSystem.SendCommandToAllClients(commandToNetwork);
            }
        }

        public void CommandGlobalReact(RemoveNetWorkComponentCommand command)
        {
            var component = command.Component;

            if (component is INetworkComponent networkComponent)
            {
                var removeCommand = new RemovedComponentOnServerCommand
                {
                    Entity = component.Owner.GUID,
                    TypeIndex = command.Component.GetTypeHashCode,
                };

                dataSenderSystem.SendCommandToAllClients(removeCommand);
            }
        }
    }
}