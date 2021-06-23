using HECSFramework.Core;

namespace Commands
{
    public struct InitNetworkSystemCommand : ICommand, IGlobalCommand
    {
        public int Port;
        public string Key;
    }
}