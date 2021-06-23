namespace HECSFramework.Core{	public static partial class HMasks	{		private static HECSMask actorcontainerid;		public static ref HECSMask ActorContainerID => ref actorcontainerid;		private static HECSMask connectionsholdercomponent;		public static ref HECSMask ConnectionsHolderComponent => ref connectionsholdercomponent;		private static HECSMask locationcomponent;		public static ref HECSMask LocationComponent => ref locationcomponent;		private static HECSMask clientidholdercomponent;		public static ref HECSMask ClientIDHolderComponent => ref clientidholdercomponent;		private static HECSMask networkcomponentsholder;		public static ref HECSMask NetworkComponentsHolder => ref networkcomponentsholder;		private static HECSMask servertagcomponent;		public static ref HECSMask ServerTagComponent => ref servertagcomponent;		private static HECSMask syncentitiesholdercomponent;		public static ref HECSMask SyncEntitiesHolderComponent => ref syncentitiesholdercomponent;		private static HECSMask worldsliceindexcomponent;		public static ref HECSMask WorldSliceIndexComponent => ref worldsliceindexcomponent;		static HMasks()		{				actorcontainerid = 				new HECSMask				{					Index = 0,					Mask01 = 1ul << 2,				};				connectionsholdercomponent = 				new HECSMask				{					Index = 1,					Mask01 = 1ul << 3,				};				locationcomponent = 				new HECSMask				{					Index = 2,					Mask01 = 1ul << 4,				};				clientidholdercomponent = 				new HECSMask				{					Index = 3,					Mask01 = 1ul << 5,				};				networkcomponentsholder = 				new HECSMask				{					Index = 4,					Mask01 = 1ul << 6,				};				servertagcomponent = 				new HECSMask				{					Index = 5,					Mask01 = 1ul << 7,				};				syncentitiesholdercomponent = 				new HECSMask				{					Index = 6,					Mask01 = 1ul << 8,				};				worldsliceindexcomponent = 				new HECSMask				{					Index = 7,					Mask01 = 1ul << 9,				};		}	}}