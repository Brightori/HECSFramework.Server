using Components;
using HECSFramework.Core;
using System;

namespace Systems
{
    [Documentation(Doc.Server, Doc.Global, "This system provide functionality of setup data to different components on wolrd global start")]
    public sealed class ServerSetOnStartSystem : BaseSystem, IGlobalStart
    {
        public void GlobalStart()
        {
            Owner.GetHECSComponent<TimeComponent>().Start(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Owner.GetHECSComponent<ConfigComponent>().Data.ServerTickMilliseconds);
        }

        public override void InitSystem()
        {
        }
    }
}
