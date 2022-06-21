using System;
using HECSFramework.Core;

namespace Commands
{
    public struct NewClientIsReadyCommand : IGlobalCommand
    {
        public Guid Client;
    }
}