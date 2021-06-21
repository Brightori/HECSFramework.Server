using System;
using System.Collections.Generic;
using System.Text;

namespace HECSFramework.Core
{
    public partial class ResolversMap
    {
        private Dictionary<int, ICommandResolver> commandsMap = new Dictionary<int, ICommandResolver>(64);
		partial void InitPartialCommandResolvers();

		public void InitCommandResolvers()
        {
			InitPartialCommandResolvers();
        }

		public void ProcessCommand(ResolverDataContainer resolverDataContainer)
        {
			if (commandsMap.TryGetValue(resolverDataContainer.TypeHashCode, out var resolver))
			{
				resolver.ResolveCommand(resolverDataContainer);
            }
        }
    }

	public class CommandResolver<T> : ICommandResolver where T : INetworkCommand
	{
		public void ResolveCommand(ResolverDataContainer resolverDataContainer, int worldIndex = 0)
		{
			EntityManager.Command((T)resolverDataContainer.Data, worldIndex);
		}
	}

	public interface ICommandResolver
	{
		void ResolveCommand(ResolverDataContainer resolverDataContainer, int worldIndex = 0);
	}

	
	public interface INetworkCommand : ICommand, IGlobalCommand, IData
	{
	}
}