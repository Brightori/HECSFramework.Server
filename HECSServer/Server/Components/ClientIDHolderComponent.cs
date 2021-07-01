using HECSFramework.Core;
using LiteNetLib;
using System;

namespace Components
{
    public class ClientIDHolderComponent : BaseComponent
    {
        private NetPeer peerConnection = null;
        private IEntity client;
        public Guid ClientID { get; set; }
        public IEntity Client
        {
            get
            {
                if (client != null)
                    return client;

                else
                    EntityManager.TryGetEntityByID(ClientID, out client);

                return client;
            }
        }

        public NetPeer GetPeer()
        {
            if (peerConnection != null)
                return peerConnection;
            
            //todo заменить на актуальную маску
            EntityManager.TryGetEntityByComponents(out var serverLogic, ref HMasks.Dummy);
            peerConnection = serverLogic.GetHECSComponent<ConnectionsHolderComponent>().ClientConnectionsGUID[ClientID];
            return peerConnection;
        }

        public bool TryGetClientEntity(out IEntity entity)
        {
            if (client != null)
            {
                entity = client;
                return true;
            }

            var result = EntityManager.TryGetEntityByID(ClientID, out var clientFind);


            entity = clientFind;
            client = clientFind;

            return result;
        }
    }
}