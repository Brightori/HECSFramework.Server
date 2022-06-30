using HECSFramework.Core;

namespace HECSFramework.Server
{
    //todo 
    public static class StartServer
    {
      //  public static uint Tick = 0;
        
        public static void Start(string[] args)
        {
            Config config = Config.Load();
            EntityManager entityManager = new EntityManager(16);
        }
    }
}