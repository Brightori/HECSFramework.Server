using Components;using System;using MessagePack;namespace HECSFramework.Core{	[MessagePackObject, Serializable]	public struct ConnectionsHolderComponentResolver : IResolver<ConnectionsHolderComponent>, IData	{		public ConnectionsHolderComponentResolver In(ref ConnectionsHolderComponent connectionsholdercomponent)		{			return this;		}		public void Out(ref IEntity entity)		{			var local = entity.GetConnectionsHolderComponent();			Out(ref local);		}		public void Out(ref ConnectionsHolderComponent connectionsholdercomponent)		{		}	}}