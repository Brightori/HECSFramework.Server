using HECSFramework.Core;

namespace Components
{
    public partial class TransformComponent : IAfterSerializationComponent
    {
        public void AfterSync()
        {
            InfoUpdated();
        }
    }
}