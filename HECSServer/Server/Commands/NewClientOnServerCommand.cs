using HECSFramework.Core;
using System;

namespace Commands
{
    [Documentation("Client", "Эту команду мы отправляем после того как получили нового клиента")]
    public struct NewConnectionCommand : ICommand
    {
        public Guid Client;
    }
}