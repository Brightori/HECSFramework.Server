using Commands;
using Components;
using HECSFramework.Core;
using HECSFramework.Core.Helpers;
using HECSFramework.Network;
using HECSFramework.Server;
using LiteNetLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Systems
{
    /// <summary>
    /// эта система отвечает за ентити которые синхронизируются и за пометку грязным - нетворк компонент холдера
    /// </summary>
    public class SyncEntitiesSystem : BaseSystem, IUpdatable, IReactEntity, IReactComponent, ILateUpdatable,
        IReactGlobalCommand<SyncClientNetworkCommand>,
        IReactGlobalCommand<AddOrRemoveComponentToServerCommand>,
        IReactGlobalCommand<AddNetWorkComponentCommand>,
        IReactGlobalCommand<SendWorldSliceToClientCommand>,
        IReactGlobalCommand<RemoveNetWorkComponentCommand>,
        IReactGlobalCommand<UpdateWorldSlice>,
        IReactGlobalCommand<RequestEntityFromServerNetworkCommand>,
        IReactGlobalCommand<ConfirmRecieveCommand>
    {
        private const float IntervalMilliseconds = 1000;
        private const float IntervalSliceMilliseconds = 5000;
        private const int IndexDeltaSyncRange = 20;
        private const int LocationsRange = 16;

        private ConcurrentQueue<(Guid, SyncClientNetworkCommand)> syncClients = new ConcurrentQueue<(Guid, SyncClientNetworkCommand)>();
        private ConcurrentDictionary<Guid, SpawnEntityCommand> spawncacheCommands = new ConcurrentDictionary<Guid, SpawnEntityCommand>();
        private ConcurrentDictionary<int, byte[]> worldSlices = new ConcurrentDictionary<int, byte[]>();
        private ConcurrentDictionary<int, List<Guid>> worldSlicesByGuid = new ConcurrentDictionary<int, List<Guid>>();
        private ConcurrentDictionary<int, SpawnCommandToPeerContainer> spawnCommandsWaitForRecieve = new ConcurrentDictionary<int, SpawnCommandToPeerContainer>();
        private List<SpawnEntityCommand>[] commandsToSend = new List<SpawnEntityCommand>[32];
        private Queue<Guid> cleanEntities = new Queue<Guid>(64);

        private ConnectionsHolderComponent connectionsHolderComponent;
        private SyncEntitiesHolderComponent syncEntitiesHolderComponent;
        private IDataSenderSystem dataSenderSystem;

        private float timer;
        private float sliceTimer;
        private int sliceIndex;
        private bool needUpdateIndex;
        private int commandIndex;

        private float step => Config.Instance.ServerTickMilliseconds;
        public Guid ListenerGuid => SystemGuid;

        public override void InitSystem()
        {
            Owner.TryGetSystem(out dataSenderSystem);
            Owner.TryGetHecsComponent(out connectionsHolderComponent);
            Owner.TryGetHecsComponent(out syncEntitiesHolderComponent);

            for (int i = 0; i < commandsToSend.Length; i++)
                commandsToSend[i] = new List<SpawnEntityCommand>(512);

            for (int i = 0; i < LocationsRange; i++)
                worldSlicesByGuid.TryAdd(i, new List<Guid>(256));

            sliceTimer = IntervalSliceMilliseconds + step;
            AddEmptySlice();
        }

        private void AddEmptySlice()
        {
            var command = new SpawnCompleteCommand { SyncIndex = sliceIndex, SpawnEntities = new List<SpawnEntityCommand>(0) };
            var packet = EntityManager.ResolversMap.GetCommandContainer(command, SystemGuid);
            var byteData = MessagePack.MessagePackSerializer.Serialize(packet);
            worldSlices.TryAdd(-1, byteData);
        }

        public void UpdateGuidSlice(int location, Guid entity, bool add)
        {
            if (!worldSlicesByGuid.TryGetValue(location, out var neededList))
                return;

            lock (neededList)
            {
                neededList.AddOrRemoveElement(entity, add);
            }
        }

        public void CommandGlobalReact(SyncClientNetworkCommand command)
        {
            lock (syncClients)
            {
                if (syncClients.Any(x => x.Item1 == command.ClientGuid))
                    return;
            }

            syncClients.Enqueue((command.ClientGuid, command));
        }

        private async void SyncClient(SyncClientNetworkCommand command)
        {
            await Task.Factory.StartNew(() => ProcessClientSyncRequest(command)).ConfigureAwait(false);
        }

        private void ProcessClientSyncRequest(SyncClientNetworkCommand command)
        {
            if (command.World == -1)
                return;

            if (!connectionsHolderComponent.ClientConnectionsGUID.TryGetValue(command.ClientGuid, out var peer))
                return;

            if (syncEntitiesHolderComponent.EntityToSlice.TryGetValue(command.ClientGuid, out var sliceIndex))
            {
                if (sliceIndex == syncEntitiesHolderComponent.CurrentIndex)
                    return;

                if (Math.Abs(syncEntitiesHolderComponent.CurrentIndex - sliceIndex) < IndexDeltaSyncRange)
                {
                    var location = command.World;
                    var currentIndex = syncEntitiesHolderComponent.CurrentIndex;
                    var peerDelta = peer;

                    for (int i = sliceIndex; i <= currentIndex; i++)
                    {
                        if (!syncEntitiesHolderComponent.DeltaSlice.TryGetValue(i, out var minislice))
                            continue;

                        if (minislice.Location != location)
                            continue;

                        if (minislice.AddEntity != Guid.Empty)
                        {
                            if (!syncEntitiesHolderComponent.SyncEnities.TryGetValue(minislice.AddEntity, out var entity))
                                continue;

                            var spawn = GetSpawnEntityCommand(entity);
                            spawn.IsNeedRecieveConfirm = true;
                            spawn.Index = commandIndex;
                            commandIndex++;

                            spawnCommandsWaitForRecieve.TryAdd(commandIndex, new SpawnCommandToPeerContainer { Peer = peer, SpawnEntityCommand = spawn });

                            try
                            {
                                dataSenderSystem.SendCommand(peerDelta, Guid.Empty, spawn, LiteNetLib.DeliveryMethod.Unreliable);
                            }
                            catch
                            {
                                dataSenderSystem.SendCommand(peerDelta, Guid.Empty, spawn, LiteNetLib.DeliveryMethod.ReliableUnordered);
                            }
                        }

                        if (minislice.RemoveEntity != Guid.Empty)
                            dataSenderSystem.SendCommand(peerDelta, Guid.Empty, new RemoveEntityFromClientCommand { EntityToRemove = minislice.RemoveEntity });
                    }

                    syncEntitiesHolderComponent.EntityToSlice[command.ClientGuid] = syncEntitiesHolderComponent.CurrentIndex;

                    dataSenderSystem.SendCommand(peerDelta, Guid.Empty, new DeltaSliceNetworkCommand { CurrentSliceIndex = syncEntitiesHolderComponent.CurrentIndex, CurrentEntities = worldSlicesByGuid[command.World] });
                    return;
                }
            }

            if (worldSlices.TryGetValue(command.World, out var data))
            {
                peer.Send(data, LiteNetLib.DeliveryMethod.ReliableUnordered);
                if (syncEntitiesHolderComponent.EntityToSlice.ContainsKey(command.ClientGuid))
                    syncEntitiesHolderComponent.EntityToSlice[command.ClientGuid] = syncEntitiesHolderComponent.CurrentIndex;
                else
                    syncEntitiesHolderComponent.EntityToSlice.TryAdd(command.ClientGuid, syncEntitiesHolderComponent.CurrentIndex);
            }

            else
                Debug.LogWarning("Срез еще не готов");
        }

        public void SendSpawnConfirmedCommand(NetPeer peer, IEntity entity)
        {
            var spawn = GetSpawnEntityCommand(entity);
            spawn.IsNeedRecieveConfirm = true;
            spawn.Index = commandIndex;
            commandIndex++;

            spawnCommandsWaitForRecieve.TryAdd(commandIndex, new SpawnCommandToPeerContainer { Peer = peer, SpawnEntityCommand = spawn });

            try
            {
                dataSenderSystem.SendCommand(peer, Guid.Empty, spawn, LiteNetLib.DeliveryMethod.Unreliable);
            }
            catch
            {
                dataSenderSystem.SendCommand(peer, Guid.Empty, spawn, LiteNetLib.DeliveryMethod.ReliableUnordered);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpawnEntityCommand GetSpawnEntityCommand(IEntity e)
        {
            var spawnEntityCommand = new SpawnEntityCommand();

            spawnEntityCommand.ClientGuid = e.GetClientIDHolderComponent().ClientID;
            spawnEntityCommand.CharacterGuid = e.GUID;
            spawnEntityCommand.Entity = new EntityResolver().GetEntityResolver(e);
            return spawnEntityCommand;
        }

        public void UpdateLocal()
        {
            if (timer > IntervalMilliseconds)
                timer = 0;
            else
            {
                timer += step;
                return;
            }

            // TODO: отдавать приоритет клиентам со старым индексом
            int counter = 0;

            while (counter < 50)
            {
                if (syncClients.TryDequeue(out var syncClient))
                    SyncClient(syncClient.Item2);
                else
                    break;

                counter++;
            }

            dataSenderSystem.SendCommandToAll(new RequestSyncEntitiesNetworkCommand { Index = syncEntitiesHolderComponent.CurrentIndex }, Guid.Empty, LiteNetLib.DeliveryMethod.Unreliable);
        }

        public void ResendDontConfirmedSpawnCommands()
        {
            foreach (var commandContainer in spawnCommandsWaitForRecieve)
            {
                var peer = commandContainer.Value.Peer;
                if (peer != null && peer.ConnectionState == ConnectionState.Connected)
                {
                    dataSenderSystem.SendCommand(peer, Guid.Empty, commandContainer.Value.SpawnEntityCommand, DeliveryMethod.Unreliable);
                }
                else
                    spawnCommandsWaitForRecieve.TryRemove(commandContainer.Key, out _);
            }
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

        public void EntityReact(IEntity entity, bool add)
        {
            if (!add)
                syncEntitiesHolderComponent.RemoveEntity(entity);
        }

        public void ComponentReact(IComponent component, bool isAdded)
        {
            if (component.Owner.IsAlive && component is INetworkComponent && component.Owner.ContainsMask(ref HMasks.NetworkComponentsHolder))
                component.Owner.GetNetworkComponentsHolder().IsDirty = true;
        }

        public async void UpdateLateLocal()
        {
            if (sliceTimer > IntervalSliceMilliseconds)
                sliceTimer = 0;
            else
            {
                sliceTimer += step;
                return;
            }

            if (sliceIndex != syncEntitiesHolderComponent.CurrentIndex)
                needUpdateIndex = true;

            await Task.Factory.StartNew(CreateWorldSlice, TaskCreationOptions.LongRunning);
        }

        private void CreateWorldSlice()
        {
            Slice();
        }

        private async void Slice()
        {
            foreach (var cashed in commandsToSend)
                cashed.Clear();

            foreach (var sliceGuid in worldSlicesByGuid)
                sliceGuid.Value.Clear();

            foreach (var e in syncEntitiesHolderComponent.SyncEnities)
            {
                if (!spawncacheCommands.TryGetValue(e.Value.GUID, out var command))
                {
                    spawncacheCommands.TryAdd(e.Value.GUID, GetSpawnEntityCommand(e.Value));
                    command = spawncacheCommands[e.Value.GUID];
                }

                UpdateSpawnCommand(ref command);
                spawncacheCommands[e.Value.GUID] = command;

                if (e.Value.WorldId < 0)
                    continue;


                commandsToSend[e.Value.WorldId].Add(command);
                worldSlicesByGuid[e.Value.WorldId].Add(e.Key);
            }

            if (needUpdateIndex)
            {
                sliceIndex = syncEntitiesHolderComponent.UpdateIndex();
                needUpdateIndex = false;
                Debug.LogDebugFormat("Создаём срез мира", sliceIndex);
            }

            for (int i = 0; i < commandsToSend.Length; i++)
            {
                List<SpawnEntityCommand> data = commandsToSend[i];
                if (data.Count == 0)
                    continue;

                var index = i;
                var command = new SpawnCompleteCommand { SyncIndex = sliceIndex, SpawnEntities = data,  WorldIndex = index };
                ResolverDataContainer packet;

                try
                {
                    packet = EntityManager.ResolversMap.GetCommandContainer(command, Guid.Empty);
                }
                catch
                {
                    sliceIndex = syncEntitiesHolderComponent.UpdateIndex();
                    needUpdateIndex = true;
                    continue;
                }

                var byteData = MessagePack.MessagePackSerializer.Serialize(packet);

                if (worldSlices.ContainsKey(index))
                    worldSlices[index] = byteData;
                else
                    worldSlices.TryAdd(index, byteData);
            }

            await Task.Run(CleanEntities);
        }

        private void CleanEntities()
        {
            foreach (var e in syncEntitiesHolderComponent.SyncEnities)
            {
                if (e.Value == null || !e.Value.IsAlive)
                    cleanEntities.Enqueue(e.Key);
            }

            while (cleanEntities.TryDequeue(out var entity))
            {
                syncEntitiesHolderComponent.RemoveEntity(entity);
                var needed = EntityManager.Worlds[0].Entities.FirstOrDefault(x => x.GUID == entity);

                if (needed != null)
                {
                    EntityManager.Worlds[0].Entities.Remove(needed);
                }
            }
        }

        private void UpdateSpawnCommand(ref SpawnEntityCommand spawnBotsCommand)
        {
            if (EntityManager.TryGetEntityByID(spawnBotsCommand.CharacterGuid, out var entity))
            {
                spawnBotsCommand.Entity = new EntityResolver().GetEntityResolver(entity);
            }
        }

        public void CommandGlobalReact(SendWorldSliceToClientCommand command)
        {
            if (worldSlices.TryGetValue(command.LocationZone, out var slice))
                command.Peer.Send(slice, LiteNetLib.DeliveryMethod.ReliableUnordered);
        }


        public async void CommandGlobalReact(UpdateWorldSlice command)
        {
            await Task.Factory.StartNew(CreateWorldSlice, TaskCreationOptions.LongRunning);
        }

        public void CommandGlobalReact(ConfirmRecieveCommand command)
        {
            if (spawnCommandsWaitForRecieve.TryGetValue(command.Index, out _))
                spawnCommandsWaitForRecieve.Remove(command.Index, out _);
        }

        public void CommandGlobalReact(RequestEntityFromServerNetworkCommand command)
        {
            if (!connectionsHolderComponent.ClientConnectionsGUID.TryGetValue(command.ClientID, out var peer))
                return;

            if (!syncEntitiesHolderComponent.SyncEnities.TryGetValue(command.NeededEntity, out var entity))
                return;

            var spawn = GetSpawnEntityCommand(entity);
            spawn.IsNeedRecieveConfirm = true;
            spawn.Index = commandIndex;
            commandIndex++;

            spawnCommandsWaitForRecieve.TryAdd(commandIndex, new SpawnCommandToPeerContainer { Peer = peer, SpawnEntityCommand = spawn });
            
            try
            {
                dataSenderSystem.SendCommand(peer, Guid.Empty, spawn, DeliveryMethod.Unreliable);
            }
            catch
            {
                dataSenderSystem.SendCommand(peer, Guid.Empty, spawn, DeliveryMethod.ReliableUnordered);
            }
        }
    }

    public struct SpawnCommandToPeerContainer
    {
        public NetPeer Peer;
        public SpawnEntityCommand SpawnEntityCommand;
    }
}