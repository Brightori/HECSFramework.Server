using MessagePack;
using System;

namespace HECSServer.HECSNetwork
{
    [MessagePackObject]
    public struct HECSNetMessage
    {
        public enum TypeOfMessage 
        {
            Command,
            Component,
            String,
            Connect,
        }

        [Key(0)]
        public Guid ClientGuid;
        
        [Key(1)]
        public Guid Entity;

        [Key(2)]
        public TypeOfMessage Type;
        
        [Key(3)]
        public int ComponentID;
        
        [Key(4)]
        public int CommandID;

        [Key(5)]
        public byte[] Data;
    }
}