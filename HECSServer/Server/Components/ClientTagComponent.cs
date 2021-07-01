using HECSFramework.Core;
using System;

namespace Components
{
    public class ClientTagComponent : BaseComponent
    {
        public Guid ClientGuid => Owner.GUID;
    }
}