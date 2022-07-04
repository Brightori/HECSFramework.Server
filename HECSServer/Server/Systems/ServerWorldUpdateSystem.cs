using System;
using System.Collections.Concurrent;
using System.Threading;
using Commands;
using Components;
using HECSFramework.Core;

namespace Systems
{
    public sealed class ServerWorldUpdateSystem : BaseSystem, IReactGlobalCommand<AddOrRemoveNewWorldGlobalCommand>, ILateStart
    {
        [Required]
        public GameLoopComponent LoopComponent;

        [Required]
        public TimeComponent Time;

        [Required]
        public ServerTickComponent TickComponent;

        private ConcurrencyList<GlobalUpdateSystem> globalUpdateSystems = new ConcurrencyList<GlobalUpdateSystem>(8);
        private ConcurrentQueue<AddOrRemoveNewWorldGlobalCommand> processWorldsInQueue = new ConcurrentQueue<AddOrRemoveNewWorldGlobalCommand>();

        public override void InitSystem()
        {
            globalUpdateSystems.Add(Owner.World.GlobalUpdateSystem);

            if (LoopComponent.Gameloop || LoopComponent.GameloopInProgress)
            {
                HECSDebug.LogError("Game loop already started");
                return;
            }

            LoopComponent.Gameloop = true;
            LoopComponent.GameloopInProgress = true;
        }

        private void GameLoop()
        {
            while (LoopComponent.Gameloop)
            {
                try
                {
                    while (processWorldsInQueue.TryDequeue(out var result))
                    {
                        if (result.Add)
                            globalUpdateSystems.Add(result.World.GlobalUpdateSystem);
                        else
                            globalUpdateSystems.Remove(result.World.GlobalUpdateSystem);
                    }

                    for (int i = 0; i < globalUpdateSystems.Count; i++)
                    {
                        globalUpdateSystems.Data[i].Update();
                    }

                    for (int i = 0; i < globalUpdateSystems.Count; i++)
                    {
                        globalUpdateSystems.Data[i].LateUpdate();
                    }

                    for (int i = 0; i < globalUpdateSystems.Count; i++)
                    {
                        globalUpdateSystems.Data[i].FinishUpdate?.Invoke();
                    }

                    int sleepTime = (int)Time.TimeUntilTick;

                    //todo rework to spin
                    if (sleepTime > 0) { Thread.Sleep(sleepTime); }

                    Time.NextTick();
                    TickComponent.Tick++;
                }
                catch (Exception e)
                {
                    HECSDebug.LogError(e.ToString());
                }
            }

            LoopComponent.GameloopInProgress = false;
        }

        public void StopGameLoop()
        {
            LoopComponent.Gameloop = false;
        }

        public void CommandGlobalReact(AddOrRemoveNewWorldGlobalCommand command)
        {
            processWorldsInQueue.Enqueue(command);
        }

        public void LateStart()
        {
            _ = new Thread(GameLoop);
        }
    }
}