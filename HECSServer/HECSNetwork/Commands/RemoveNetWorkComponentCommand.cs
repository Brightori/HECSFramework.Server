using HECSFramework.Core;

namespace Commands
{
    public struct RemoveNetWorkComponentCommand : IGlobalCommand
    {
        public IComponent Component;
    }
}