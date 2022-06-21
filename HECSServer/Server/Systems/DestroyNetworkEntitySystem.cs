using Commands;
using Components;
using HECSFramework.Core;
using System.Collections.Generic;

namespace Systems
{
    [Documentation(Doc.GameLogic, "Эта система живет в самом мире, отвечает за то что после всех апдейтов вызовется эта система, и почистит сетевые которые мы просим удалить")]
    public class DestroyNetworkEntityWorldSystem : BaseSystem,   IReactGlobalCommand<DestroyNetworkEntityWorldCommand>, IAfterEntityInit
    {
        private Queue<IEntity> entitiesForDelete = new Queue<IEntity>(8);
        private DataSenderSystem dataSender; 

        private void React()
        {
            while (entitiesForDelete.Count > 0)
            {
                var entity = entitiesForDelete.Dequeue();

                if (entity.IsAlive())
                {
                    dataSender.SendCommandToAllClients(new RemoveEntityFromClientCommand { EntityToRemove = entity.GUID });
                    entity.HecsDestroy();
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            Owner.World.GlobalUpdateSystem.FinishUpdate -= React;
        }

        public void CommandGlobalReact(DestroyNetworkEntityWorldCommand command)
        {
            entitiesForDelete.Enqueue(command.Entity);
        }

        public void AfterEntityInit()
        {
            Owner.World.GlobalUpdateSystem.FinishUpdate += React;
            dataSender = EntityManager.GetSingleSystem<DataSenderSystem>();
        }

        public override void InitSystem()
        {
        }
    }
}

namespace Commands
{
    public struct DestroyNetworkEntityWorldCommand : IGlobalCommand
    {
        public IEntity Entity;
    }
}