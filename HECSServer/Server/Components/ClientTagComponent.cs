using HECSFramework.Core;

namespace Components
{
    public class ClientTagComponent : BaseComponent
    {
        public bool IsReadyToSync { get; set; }
    }
}