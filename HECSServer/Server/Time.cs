using HECSFramework.Server;

namespace HECSFramework.Core
{
    public static class Time
    {
        public static float deltaTime => Config.Instance.ServerTickMilliseconds / 1000f;
    }
}