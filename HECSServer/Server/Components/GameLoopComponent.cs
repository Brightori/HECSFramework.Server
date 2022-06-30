using HECSFramework.Core;

namespace Components
{
    [Documentation(Doc.NONE, "")]
    public sealed class GameLoopComponent : BaseComponent
    {
        public volatile bool Gameloop = false;
        public volatile bool GameloopInProgress = false;
    }
}