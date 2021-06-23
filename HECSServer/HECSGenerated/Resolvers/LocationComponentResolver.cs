using Components;using System;using MessagePack;namespace HECSFramework.Core{	[MessagePackObject]	public struct LocationComponentResolver : IResolver<LocationComponent>, IData	{		[Key(0)]		public int LocationZone;		[Key(1)]		public int Version;		public LocationComponentResolver In(ref LocationComponent locationcomponent)		{			this.LocationZone = locationcomponent.LocationZone;			this.Version = locationcomponent.Version;			return this;		}		public void Out(ref IEntity entity)		{			var local = entity.GetLocationComponent();			Out(ref local);		}		public void Out(ref LocationComponent locationcomponent)		{			locationcomponent.LocationZone = this.LocationZone;			locationcomponent.Version = this.Version;			locationcomponent.AfterSync();		}	}}