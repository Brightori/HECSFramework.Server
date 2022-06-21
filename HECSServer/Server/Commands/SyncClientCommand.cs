using System;
using Components;
using HECSFramework.Core;

namespace Commands
{
    [Documentation(Doc.Server, "Триггерит синхронизацию клиента вне очереди")]
    public struct SyncClientCommand : IGlobalCommand
    {
        public Guid Client { get; set; }
    }
}