using HECSFramework.Core;
using LiteNetLib;
using System;

namespace Components
{
    public partial class ClientIDHolderComponent : BaseComponent
    {
        private NetPeer peerConnection = null;
        private IEntity client;
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

            peerConnection = EntityManager.GetSingleComponent<ConnectionsHolderComponent>().ClientConnectionsGUID[ClientID];
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