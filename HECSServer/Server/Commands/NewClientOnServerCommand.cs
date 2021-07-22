using HECSFramework.Core;
using System;

namespace Commands
{
    [Documentation("Client", "Эту команду мы отправляем после того как зарегестрировали клиента")]
    public struct NewClientOnServerCommand : IGlobalCommand
    {
        public Guid Client;
    }
}