using HECSFramework.Core;

namespace Commands
{
    [Documentation(Doc.HECS, Doc.Server, "This command for proceed logic after adding new room world to server world")]
    public struct AddOrRemoveNewWorldGlobalCommand : IGlobalCommand
    {
        public World World;
        public bool Add;
    }
}