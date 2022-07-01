using HECSFramework.Core;

namespace Commands
{
    [Documentation(Doc.HECS, Doc.Server, "We send this command to world, for example to server world, for initiating start process")]
    public struct StartWorldCommand : IGlobalCommand
    {
    }
}