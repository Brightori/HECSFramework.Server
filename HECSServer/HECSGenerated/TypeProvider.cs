using System.Collections.Generic;using Components;using System;using Systems;namespace HECSFramework.Core{	public partial class TypesProvider	{		public TypesProvider()		{			Count = 9;			MapIndexes = GetMapIndexes();			TypeToComponentIndex = GetTypeToComponentIndexes();			HashToType = GetHashToTypeDictionary();			TypeToHash = GetTypeToHash();			HECSFactory = new HECSFactory();		}		private Dictionary<Type, int> GetTypeToComponentIndexes()		{			return new Dictionary<Type, int>()			{				{ typeof(ActorContainerID), 0 },				{ typeof(ConnectionsHolderComponent), 1 },				{ typeof(LocationComponent), 2 },				{ typeof(ClientIDHolderComponent), 3 },				{ typeof(NetworkComponentsHolder), 4 },				{ typeof(ServerTagComponent), 5 },				{ typeof(SyncEntitiesHolderComponent), 6 },				{ typeof(WorldSliceIndexComponent), 7 },			};		}		private Dictionary<Type, int> GetTypeToHash()		{			return new Dictionary<Type, int>()			{				{ typeof(ActorContainerID), 107601317 },				{ typeof(ConnectionsHolderComponent), 181847649 },				{ typeof(LocationComponent), 121100508 },				{ typeof(ClientIDHolderComponent), 155175405 },				{ typeof(NetworkComponentsHolder), 163302026 },				{ typeof(ServerTagComponent), 127457553 },				{ typeof(SyncEntitiesHolderComponent), 189120713 },				{ typeof(WorldSliceIndexComponent), 167365335 },			};		}		private Dictionary<int, Type> GetHashToTypeDictionary()		{			return new Dictionary<int, Type>()			{				{ 107601317, typeof(ActorContainerID)},				{ 181847649, typeof(ConnectionsHolderComponent)},				{ 121100508, typeof(LocationComponent)},				{ 155175405, typeof(ClientIDHolderComponent)},				{ 163302026, typeof(NetworkComponentsHolder)},				{ 127457553, typeof(ServerTagComponent)},				{ 189120713, typeof(SyncEntitiesHolderComponent)},				{ 167365335, typeof(WorldSliceIndexComponent)},			};		}		private Dictionary<int, ComponentMaskAndIndex> GetMapIndexes()		{			return new Dictionary<int, ComponentMaskAndIndex>			{				{ -1, new ComponentMaskAndIndex {  ComponentName = "DefaultEmpty", ComponentsMask = HECSMask.Empty }},			{ 107601317, new ComponentMaskAndIndex {ComponentName = "ActorContainerID", ComponentsMask = new HECSMask				{					Index = 0,					Mask01 = 1ul << 2,				}			}},			{ 181847649, new ComponentMaskAndIndex {ComponentName = "ConnectionsHolderComponent", ComponentsMask = new HECSMask				{					Index = 1,					Mask01 = 1ul << 3,				}			}},			{ 121100508, new ComponentMaskAndIndex {ComponentName = "LocationComponent", ComponentsMask = new HECSMask				{					Index = 2,					Mask01 = 1ul << 4,				}			}},			{ 155175405, new ComponentMaskAndIndex {ComponentName = "ClientIDHolderComponent", ComponentsMask = new HECSMask				{					Index = 3,					Mask01 = 1ul << 5,				}			}},			{ 163302026, new ComponentMaskAndIndex {ComponentName = "NetworkComponentsHolder", ComponentsMask = new HECSMask				{					Index = 4,					Mask01 = 1ul << 6,				}			}},			{ 127457553, new ComponentMaskAndIndex {ComponentName = "ServerTagComponent", ComponentsMask = new HECSMask				{					Index = 5,					Mask01 = 1ul << 7,				}			}},			{ 189120713, new ComponentMaskAndIndex {ComponentName = "SyncEntitiesHolderComponent", ComponentsMask = new HECSMask				{					Index = 6,					Mask01 = 1ul << 8,				}			}},			{ 167365335, new ComponentMaskAndIndex {ComponentName = "WorldSliceIndexComponent", ComponentsMask = new HECSMask				{					Index = 7,					Mask01 = 1ul << 9,				}			}},			};		}	}	public partial class HECSFactory	{		public HECSFactory()		{			getComponentFromFactoryByHash = GetComponentFromFactoryFunc;			getSystemFromFactoryByHash = GetSystemFromFactoryFunc;		}		private IComponent GetComponentFromFactoryFunc(int hashCodeType)		{			switch (hashCodeType)			{				case 107601317:					return new ActorContainerID();				case 181847649:					return new ConnectionsHolderComponent();				case 121100508:					return new LocationComponent();				case 155175405:					return new ClientIDHolderComponent();				case 163302026:					return new NetworkComponentsHolder();				case 127457553:					return new ServerTagComponent();				case 189120713:					return new SyncEntitiesHolderComponent();				case 167365335:					return new WorldSliceIndexComponent();			}			return default;		}		private ISystem GetSystemFromFactoryFunc(int hashCodeType)		{			switch (hashCodeType)			{				case 156487476:					return new WaitingCommandsSystems();				case 111402332:					return new DataSenderSystem();				case 36369996:					return new Debug();				case 137941991:					return new ServerNetworkSystem();			}			return default;		}	}}