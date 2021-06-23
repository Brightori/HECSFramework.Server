using HECSFramework.Core;
using System;

namespace Commands
{
    public struct RemoveClientCommand : ICommand
    {
        internal Guid ClientGuidToRemove;
    }
}