using HECSFramework.Core;
using LiteNetLib;
using System;

namespace Components
{
    public class ClientTagComponent : BaseComponent
    {
        private NetPeer cache;

        public Guid ClientGuid => Owner.GUID;
        
        public NetPeer NetPeer
        {
            get 
            { 
                if (cache == null)
                {
                    EntityManager.GetSingleComponent<ConnectionsHolderComponent>().
                        ClientConnectionsGUID.TryGetValue(ClientGuid, out var connection);
                    
                    cache = connection;
                }
                
                return cache; 
            }
            set => cache = value;
        }
    }
}