using HECSFramework.Core;

namespace Commands
{
    public struct AddNetWorkComponentCommand : IGlobalCommand
    {
        public IComponent Component;
    }
}