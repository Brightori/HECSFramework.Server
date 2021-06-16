using System;
using System.Collections.Generic;

namespace HECSv2.Core.Generator
{
    public class GenerateCommandMap
    {
        public string GenerateMap(List<Type> commands)
        {
            var tree = new TreeSyntaxNode();
            var resolvers = new TreeSyntaxNode();
            var typeToIdDictionary = new TreeSyntaxNode();
            var dictionaryBody = new TreeSyntaxNode();
            var genericMethod = new TreeSyntaxNode();

            tree.Add(new UsingSyntax("Commands",1));
            tree.Add(new UsingSyntax("HECSServer.HECSNetwork",1));
            tree.Add(new UsingSyntax("System", 1));
            tree.Add(new UsingSyntax("System.Collections.Generic",2));
            tree.Add(new NameSpaceSyntax("HECSServer.ServerShared"));
            tree.Add(new LeftScopeSyntax());
            tree.Add(new TabSimpleSyntax(1, "public class CommandMap"));
            tree.Add(new LeftScopeSyntax(1));
            tree.Add(new TabSimpleSyntax(2, "public Dictionary<int, ICommandResolver> Map = new Dictionary<int, ICommandResolver>"));
            tree.Add(new LeftScopeSyntax(2));
            tree.Add(resolvers);
            tree.Add(new RightScopeSyntax(2, true));
            tree.Add(new ParagraphSyntax());
            tree.Add(typeToIdDictionary);
            tree.Add(genericMethod);
            tree.Add(new RightScopeSyntax(1));
            tree.Add(new ParagraphSyntax());
            tree.Add(GenericPart());
            tree.Add(new ParagraphSyntax());
            tree.Add(new RightScopeSyntax());

            foreach (var t in commands)
                resolvers.Add(GetCommandResolver(t));

            typeToIdDictionary.Add(new TabSimpleSyntax(2, "public Dictionary<Type, int> CommandsIDs = new Dictionary<Type, int>"));
            typeToIdDictionary.Add(new LeftScopeSyntax(2));
            typeToIdDictionary.Add(dictionaryBody);
            typeToIdDictionary.Add(new RightScopeSyntax(2, true));

            for (int i = 0; i < commands.Count; i++)
            {
                Type t = commands[i];
                dictionaryBody.Add(GetCommandMethod(t));

                //if (i < commands.Count - 1)
                //    dictionaryBody.Add(new ParagraphSyntax());
            }

            genericMethod.Add(DefaultCommandMethod());

            return tree.ToString();
        }

        private ISyntax GetCommandMethod(Type command)
        {
            var tree = new TreeSyntaxNode();
            tree.Add(new TabSimpleSyntax(3, $"{{typeof({command.Name}), {IndexGenerator.GetIndexForType(command)}}},"));
            return tree;
        }

        private ISyntax DefaultCommandMethod()
        {
            var tree = new TreeSyntaxNode();

            tree.Add(new TabSimpleSyntax(2, "public HECSNetMessage GetHECSCommandMessage<T>(T networkCommand, Guid guid, bool binaryForce = false) where T : INetworkCommand"));
            tree.Add(new LeftScopeSyntax(2));
            tree.Add(new TabSimpleSyntax(3, "if (CommandsIDs.TryGetValue(typeof(T), out var id))"));
            tree.Add(new TabSimpleSyntax(4, "return new HECSNetMessage"));
            tree.Add(new LeftScopeSyntax(4));
            tree.Add(new TabSimpleSyntax(5, "Type = HECSNetMessage.TypeOfMessage.Command,"));
            tree.Add(new TabSimpleSyntax(5, $"CommandID = id,"));
            tree.Add(new TabSimpleSyntax(5, "Entity = guid,"));
            tree.Add(new TabSimpleSyntax(5, "Data = MessagePack.MessagePackSerializer.Serialize(networkCommand),"));
            tree.Add(new RightScopeSyntax(4, true));
            tree.Add(new TabSimpleSyntax(3, "else"));
            tree.Add(new TabSimpleSyntax(4, @"throw new Exception(""нет кодоген метода под команду"" + networkCommand.GetType().Name);"));
            tree.Add(new RightScopeSyntax(2));
            return tree;
        }

        private ISyntax GetCommandResolver(Type type)
        {
            return new TabSimpleSyntax(3, $"{{{IndexGenerator.GetIndexForType(type)}, new CommandResolver<{type.Name}>()}},");
        }

        #region RawGenericData
        public SimpleSyntax GenericPart()
        {
            return new SimpleSyntax
            {
                Data = @"public class CommandResolver<T> : ICommandResolver where T : INetworkCommand
	{
		public T GetCommand(byte[] data)
		{
			return MessagePack.MessagePackSerializer.Deserialize<T>(data);
		}

		public INetworkCommand ResolveCommand(byte[] data, int worldIndex = 0)
		{
			T networkCommand = GetCommand(data);
			EntityManager.Command(networkCommand, true, worldIndex);
            return networkCommand;
		}
	}

	public interface ICommandResolver
	{
		INetworkCommand ResolveCommand(byte[] data, int worldIndex = 0);
	}"
            };
        }
        #endregion
    }
}