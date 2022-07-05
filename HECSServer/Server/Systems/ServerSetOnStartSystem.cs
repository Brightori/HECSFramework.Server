using Components;
using HECSFramework.Core;

namespace Systems
{
    [Documentation(Doc.Server, Doc.Global, "This system provide functionality of setup data to different components on wolrd global start")]
    public sealed class ServerSetOnStartSystem : BaseSystem, IGlobalStart
    {
        public void GlobalStart()
        {
            Owner.GetHECSComponent<TimeComponent>().SetupConfig(Owner.GetHECSComponent<ConfigComponent>().Data);
        }

        public override void InitSystem()
        {
        }
    }
}
