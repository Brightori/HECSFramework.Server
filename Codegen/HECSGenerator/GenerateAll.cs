using GlobalCommander;
using HECSFramework.Core.Generator;
using HECSFrameWork;
using HECSFrameWork.Components;
using HECSServer.HECSNetwork;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HECSv2.Core.Generator
{
    public class GenerateAll
    {
        private readonly string DefaultPath = "/HECSServer/HECSGenerated/";
        private readonly string DefaultNameSpace = "HECSFrameWork";
        private readonly string TypesMap = "TypesMap.cs";
        private readonly string ComponentID = "ComponentID.cs";
        private readonly string ComponentsMask = "ComponentsMask.cs";
        private readonly string ComponentsContext = "ComponentContext.cs";
        private readonly string PartialWorld = "WorldPart.cs";
        private readonly string EntityGenericExtentions = "EntityGenericExtentions.cs";
        private readonly string CommandMap = "CommandMap.cs";
        private readonly string ResolversMap = "ResolversMap.cs";
        private readonly string ComponentResolversFolder = "/ComponentResolversFolder/";

        private string dataPath => Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName);

        private const string AggressiveInline = "[MethodImpl(MethodImplOptions.AggressiveInlining)]";

        private List<Type> componentTypes;
        private List<Type> componentTypesByClass;
        private List<Type> commands;

        public void StartGeneration()
        {
            GatherAssembly();
            GenerateComponentMask();
            GenerateTypesMap();
            GenerateComponentContext();

            var resolverGenerator = new CodeGenerator();
            var resolvers = resolverGenerator.GetSerializationResolvers();
            var resolversMap = resolverGenerator.GetResolverMap();

            foreach (var r in resolvers)
                SaveToFile(dataPath + DefaultPath + ComponentResolversFolder, r.name, r.content);

            SaveToFile(dataPath + DefaultPath + ComponentResolversFolder, ResolversMap, resolversMap);
            SaveToExistFile(CommandMap, new GenerateCommandMap().GenerateMap(commands));
            //SaveToFile(dataPath + "/helm/", "Chart.yaml", GenerateServerVersion(), Encoding.UTF8);
            GenerateServerVersionRegex();
        }

        private void GenerateServerVersionRegex()
        {
            var filePath = dataPath + "/helm/" + "Chart.yaml";
            var reg = new Regex(@"(?<x>^\b version:)(?<y>[^\r\n]*)(?<z>\d)");

            var t = File.ReadAllText(filePath);
            var pattern = @"version: ""[^\r\n]*(?<ver>\d)";
            var match = Regex.Match(t, pattern);

            //if (int.TryParse(match.Groups["ver"].Value, out var number))
            //{
            //    var test = ++number;
            //    t = Regex.Replace(t, pattern, test.ToString());
            //}
            
            var lines = t.Split(CParse.Paragraph);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("version:"))
                {
                    var operation = lines[i];
                    var toList = operation.ToArray().ToList();
                    var firstIndex = operation.LastIndexOf('.');
                    var lastIndex = operation.LastIndexOf(CParse.Quote);
                    var result = operation.Substring(firstIndex+1, (lastIndex-1) - firstIndex);
                    int.TryParse(result, out var testResult);
                    var value = testResult+1;

                    var valueTostring = value.ToString();
                    toList.RemoveRange(firstIndex + 1, (lastIndex - 1) - firstIndex);

                    toList.InsertRange(firstIndex + 1, valueTostring);

                    lines[i] = string.Join("", toList.ToArray());
                    break;
                }
            }

            var finalResult = string.Join(CParse.Paragraph, lines);

            SaveToFile(filePath, finalResult);
        }

        private string GenerateServerVersion()
        {
            var version = Codegen.Properties.CodogenSettings.Default.ServerVersion;
            var versionSub = Codegen.Properties.CodogenSettings.Default.SubVersion;
            var versionSubSub = Codegen.Properties.CodogenSettings.Default.SubSubVersion;
            versionSubSub++;

            Codegen.Properties.CodogenSettings.Default.SubSubVersion = versionSubSub;
            Codegen.Properties.CodogenSettings.Default.Save();

            var tree = new TreeSyntaxNode();
            tree.Add(new SimpleSyntax($"name: conference-server"+ CParse.Paragraph));
            tree.Add(new SimpleSyntax("description: Helm Chart for conference-server"+ CParse.Paragraph));
            tree.Add(new SimpleSyntax("type: application"+CParse.Paragraph));
            tree.Add(new SimpleSyntax($"version: {CParse.Quote}{version}.{versionSub}.{versionSubSub}{CParse.Quote}"+ CParse.Paragraph));
            tree.Add(new SimpleSyntax($"appVersion: {CParse.Quote}{version}.{versionSub}{CParse.Quote}"+CParse.Paragraph));
            tree.Add(new SimpleSyntax($"apiVersion: v2" + CParse.Paragraph));
            tree.Add(new SimpleSyntax($"dependencies:" + CParse.Paragraph));
            tree.Add(new SimpleSyntax($"  - name: common" + CParse.Paragraph));
            tree.Add(new SimpleSyntax($"    version: '^1'" + CParse.Paragraph));
            tree.Add(new SimpleSyntax($"    repository: {CParse.Quote}https://charts.bitnami.com/bitnami{CParse.Quote}" + CParse.Paragraph));
            tree.Add(new SimpleSyntax($"keywords:" + CParse.Paragraph));
            tree.Add(new SimpleSyntax($"  - kefir" + CParse.Paragraph));
            tree.Add(new SimpleSyntax($"  - prototypes" + CParse.Paragraph));
            tree.Add(new SimpleSyntax($"  - conference-server" + CParse.Paragraph));
            tree.Add(new SimpleSyntax($"sources:" + CParse.Paragraph));
            tree.Add(new SimpleSyntax($"  - https://gitlab.kefirgames.ru/prototypes/conference-server" + CParse.Paragraph));
            tree.Add(new SimpleSyntax($"home: https://kefirgames.ru/" + CParse.Paragraph));

            return  tree.ToString();
        }


        #region ComponentContext
        private void GenerateComponentContext()
        {
            var overTree = new TreeSyntaxNode();
            var entityExtention = new TreeSyntaxNode();

            var usings = new TreeSyntaxNode();
            var nameSpaces = new List<string>();

            var tree = new TreeSyntaxNode();
            var properties = new TreeSyntaxNode();
            
            var disposable = new TreeSyntaxNode();
            var disposableBody = new TreeSyntaxNode();

            
            var switchAdd = new TreeSyntaxNode();
            var switchBody = new TreeSyntaxNode();

            var switchRemove = new TreeSyntaxNode();
            var switchRemoveBody = new TreeSyntaxNode();

            overTree.Add(tree);
            overTree.Add(entityExtention);

            tree.Add(usings);
            tree.Add(new ParagraphSyntax());

            tree.Add(new NameSpaceSyntax(DefaultNameSpace));
            tree.Add(new LeftScopeSyntax());

            tree.Add(new CompositeSyntax(new TabSpaceSyntax(1), new SimpleSyntax("public partial class ComponentContext : IDisposable"), new ParagraphSyntax()));
            tree.Add(new LeftScopeSyntax(1));
            tree.Add(properties);
            tree.Add(switchAdd);
            tree.Add(switchRemove);
            tree.Add(disposable);
            tree.Add(new RightScopeSyntax(1));
            tree.Add(new RightScopeSyntax());

            switchAdd.Add(new ParagraphSyntax());
            switchAdd.Add(new CompositeSyntax(new TabSpaceSyntax(2), 
                new SimpleSyntax("public void AddComponent<T>(T component) where T: IComponent"), new ParagraphSyntax()));
            switchAdd.Add(new LeftScopeSyntax(2));
            
            switchAdd.Add(new CompositeSyntax(new TabSpaceSyntax(3), new SimpleSyntax("switch (component)"), new ParagraphSyntax()));
            switchAdd.Add(new LeftScopeSyntax(3));
            switchAdd.Add(switchBody);
            switchAdd.Add(new RightScopeSyntax(3));
            switchAdd.Add(new RightScopeSyntax(2));  
            
            switchAdd.Add(new ParagraphSyntax());
            switchAdd.Add(new CompositeSyntax(new TabSpaceSyntax(2), 
                new SimpleSyntax("public void RemoveComponent<T>(T component) where T: IComponent"), new ParagraphSyntax()));
            switchAdd.Add(new LeftScopeSyntax(2));
            
            switchAdd.Add(new CompositeSyntax(new TabSpaceSyntax(3), new SimpleSyntax("switch (component)"), new ParagraphSyntax()));
            switchAdd.Add(new LeftScopeSyntax(3));
            switchAdd.Add(switchRemoveBody);
            switchAdd.Add(new RightScopeSyntax(3));
            switchAdd.Add(new RightScopeSyntax(2));

            nameSpaces.Add("HECSFrameWork.Components");
            
            foreach(var c in componentTypes)
            {
                properties.Add(new CompositeSyntax(new TabSpaceSyntax(2), 
                    new SimpleSyntax($"public {c.Name} Get{c.Name.Remove(0,1)} {{ get; private set; }}"), new ParagraphSyntax()));

                var cArgument = c.Name.Remove(0, 1);
                var fixedArg = Char.ToLower(cArgument[0]) + cArgument.Substring(1);

                switchBody.Add(new CompositeSyntax(new TabSpaceSyntax(4), new SimpleSyntax($"case {c.Name} {fixedArg}:"), new ParagraphSyntax()));
                switchBody.Add(new LeftScopeSyntax(5));
                switchBody.Add(new CompositeSyntax(new TabSpaceSyntax(6), new SimpleSyntax($"Get{c.Name.Remove(0, 1)} = {fixedArg};"), new ParagraphSyntax()));
                switchBody.Add(new CompositeSyntax(new TabSpaceSyntax(6), new ReturnSyntax()));
                switchBody.Add(new RightScopeSyntax(5));
                 
                switchRemoveBody.Add(new CompositeSyntax(new TabSpaceSyntax(4), new SimpleSyntax($"case {c.Name} {fixedArg}:"), new ParagraphSyntax()));
                switchRemoveBody.Add(new LeftScopeSyntax(5));
                switchRemoveBody.Add(new CompositeSyntax(new TabSpaceSyntax(6), new SimpleSyntax($"Get{c.Name.Remove(0, 1)} = null;"), new ParagraphSyntax()));
                switchRemoveBody.Add(new CompositeSyntax(new TabSpaceSyntax(6), new ReturnSyntax()));
                switchRemoveBody.Add(new RightScopeSyntax(5));

                disposableBody.Add(new CompositeSyntax(new TabSpaceSyntax(3), new SimpleSyntax($"Get{c.Name.Remove(0, 1)} = null;"), new ParagraphSyntax()));
                //if (c != componentTypes.Last())
                //    switchBody.Add(new ParagraphSyntax());

                nameSpaces.AddOrRemoveElement(c.Namespace, true);
            }

            foreach(var n in nameSpaces)
            {
                usings.Add(new UsingSyntax(n));
                usings.Add(new ParagraphSyntax());
            }

            AddEntityExtention(entityExtention);

            usings.Add(new UsingSyntax("System", 1));
            usings.Add(new UsingSyntax("System.Runtime.CompilerServices", 1));


            disposable.Add(new ParagraphSyntax());
            disposable.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax("public void Dispose()"), new ParagraphSyntax()));
            disposable.Add(new LeftScopeSyntax(2));
            disposable.Add(disposableBody);
            disposable.Add(new RightScopeSyntax(2));


            SaveComponentContext(overTree.ToString());
        }

        private void AddEntityExtention(TreeSyntaxNode tree)
        {
            var body = new TreeSyntaxNode();

            tree.Add(new ParagraphSyntax());

            tree.Add(new NameSpaceSyntax(DefaultNameSpace));
            tree.Add(new LeftScopeSyntax());

            tree.Add(new CompositeSyntax(new TabSpaceSyntax(1), new SimpleSyntax("public static class EntityComponentExtentions"), new ParagraphSyntax()));
            tree.Add(new LeftScopeSyntax(1));
            tree.Add(body);
            tree.Add(new RightScopeSyntax(1));
            tree.Add(new RightScopeSyntax());

            foreach(var c in componentTypes)
            {
                body.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax(AggressiveInline), new ParagraphSyntax()));
                body.Add(new CompositeSyntax(new TabSpaceSyntax(2), 
                    new SimpleSyntax($"public static {c.Name} Get{c.Name.Remove(0,1)}(this IEntity entity)"), new ParagraphSyntax()));
                body.Add(new LeftScopeSyntax(2));
                body.Add(new CompositeSyntax(new TabSpaceSyntax(3), 
                    new SimpleSyntax($"return entity.ComponentContext.Get{c.Name.Remove(0, 1)};"), new ParagraphSyntax()));
                body.Add(new RightScopeSyntax(2));

                if (c != componentTypes.Last())
                    body.Add(new ParagraphSyntax());
            }
        }

        #endregion

        #region EntityExtentions
        private void GenerateEntityExtentions()
        {
            var tree = new TreeSyntaxNode();
            var methods = new TreeSyntaxNode();

            tree.Add(new UsingSyntax("Components"));
            tree.Add(new ParagraphSyntax());
            
            tree.Add(new UsingSyntax("System.Runtime.CompilerServices"));
            tree.Add(new ParagraphSyntax());
            tree.Add(new ParagraphSyntax());

            tree.Add(new NameSpaceSyntax(DefaultNameSpace));
            tree.Add(new LeftScopeSyntax());

            tree.Add(new CompositeSyntax(new TabSpaceSyntax(1), new SimpleSyntax("public static class EntityGenericExtentions"), new ParagraphSyntax()));
            
            tree.Add(new LeftScopeSyntax(1, true));
            tree.Add(methods);
            tree.Add(new RightScopeSyntax(1));
            tree.Add(new RightScopeSyntax(0));

            foreach (var c in componentTypes)
            {
                EntityAddComponent(c, methods);
                EntityGetComponent(c, methods);
                EntityRemoveComponent(c, methods);
            }

            SaveEntityExtention(tree.ToString());
        }

        private void EntityAddComponent(Type component, TreeSyntaxNode tree)
        {
            tree.Add(new ParagraphSyntax());
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax(AggressiveInline)));
            tree.Add(new ParagraphSyntax());

            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2),
                new SimpleSyntax($"public static void Add{component.Name}(this Entity entity, {component.Name} {component.Name.ToLower()}Component)")));

            tree.Add(new ParagraphSyntax());
            tree.Add(new LeftScopeSyntax(2));
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(3),
                new SimpleSyntax($"EntityManager.World(entity.WorldIndex).Add{component.Name}Component({component.Name.ToLower()}Component, ref entity);"),
                new ParagraphSyntax()));

            tree.Add(new RightScopeSyntax(2));
        }
        
        private void EntityGetComponent(Type component, TreeSyntaxNode tree)
        {
            tree.Add(new ParagraphSyntax());
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax(AggressiveInline)));
            tree.Add(new ParagraphSyntax());

            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2),
                new SimpleSyntax($"public static ref {component.Name} Get{component.Name}(this ref Entity entity)")));

            tree.Add(new ParagraphSyntax());
            tree.Add(new LeftScopeSyntax(2));
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(3),
                new SimpleSyntax($"return ref EntityManager.World(entity.WorldIndex).Get{component.Name}Component(ref entity);"),
                new ParagraphSyntax()));

            tree.Add(new RightScopeSyntax(2));
        } 
        
        private void EntityRemoveComponent(Type component, TreeSyntaxNode tree)
        {
            tree.Add(new ParagraphSyntax());
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax(AggressiveInline)));
            tree.Add(new ParagraphSyntax());

            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2),
                new SimpleSyntax($"public static void Remove{component.Name}(this ref Entity entity)")));

            tree.Add(new ParagraphSyntax());
            tree.Add(new LeftScopeSyntax(2));
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(3),
                new SimpleSyntax($"EntityManager.World(entity.WorldIndex).Remove{component.Name}Component(ref entity);"),
                new ParagraphSyntax()));

            tree.Add(new RightScopeSyntax(2));
        }


        #endregion

        #region SaveToFiles

        private void SaveTypeMap(string data)
        {
            var neededFiles = Directory.GetFiles(dataPath, TypesMap, SearchOption.AllDirectories);

            if (neededFiles.Length > 0)
            {
                File.WriteAllText(neededFiles[0], data);
                Console.WriteLine("записали карту типов");
                return;
            }

            Directory.CreateDirectory(dataPath + DefaultPath);
            File.WriteAllText(dataPath + DefaultPath + TypesMap, data);
        }

        private void SaveEntityExtention(string data)
        {
            var path = dataPath + DefaultPath + EntityGenericExtentions;

            if (!Directory.Exists(dataPath + DefaultPath))
                Directory.CreateDirectory(dataPath + DefaultPath);

            File.WriteAllText(path, data);
        }        
        
        private void SaveComponentContext(string data)
        {
            var path = dataPath + DefaultPath + ComponentsContext;

            if (!Directory.Exists(dataPath + DefaultPath))
                Directory.CreateDirectory(dataPath + DefaultPath);

            File.WriteAllText(path, data);
        }

        private void SaveToFile(string path, string fileName, string data)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            File.WriteAllText(path+fileName, data, Encoding.UTF8);
        }     
        
        private void SaveToFile(string fullPath, string data)
        {
            File.WriteAllText(fullPath, data, Encoding.UTF8);
        }    
        
        private void SaveToFile(string path, string fileName, string data, Encoding encoding)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            File.WriteAllText(path+fileName, data, encoding);
        }

        private void SavePartialWorld(string data)
        {
            var path = dataPath + DefaultPath + PartialWorld;

            if (!Directory.Exists(dataPath + DefaultPath))
                Directory.CreateDirectory(dataPath + DefaultPath);

            File.WriteAllText(path, data);
        }

        private void SaveToExistFile(string fileName, string data)
        {
            var neededFiles = Directory.GetFiles(dataPath, fileName, SearchOption.AllDirectories);

            if (neededFiles.Length > 0)
            {
                File.WriteAllText(neededFiles[0], data);

                Console.WriteLine($"записали {fileName}");
                return;
            }

            Directory.CreateDirectory(dataPath + DefaultPath);

            var path = dataPath + DefaultPath + fileName;

            File.WriteAllText(path, data);
        }

        private void SaveComponentMask(string data)
        {
            var neededFiles = Directory.GetFiles(dataPath, ComponentsMask, SearchOption.AllDirectories);

            if (neededFiles.Length > 0)
            {
                File.WriteAllText(neededFiles[0], data);

                Console.WriteLine("записали маску компонент");
                return;
            }

            Directory.CreateDirectory(dataPath + DefaultPath);

            var path = dataPath + DefaultPath + ComponentsMask;

            File.WriteAllText(path, data);
        }
        
        private void SaveComponentEnumIDS(string data)
        {
            var dir = new DirectoryInfo(dataPath);
            var neededFiles = dir.GetFiles(ComponentID, SearchOption.AllDirectories);

            if (neededFiles.Length > 0)
            {
                File.WriteAllText(neededFiles[0].FullName, data);

                //var sourceFile = neededFiles[0].FullName.Replace(Application.dataPath, "Assets");
                //AssetDatabase.ImportAsset(sourceFile);
                Console.WriteLine("записали инамы компонент");
                return;
            }

            Directory.CreateDirectory(dataPath + DefaultPath);

            var path = dataPath + DefaultPath + ComponentID;

            File.WriteAllText(path, data);
        }

        #endregion

        #region GenerateTypesMap
        public void GenerateTypesMap()
        {
            UpdateComponentID();

            var className = "TypesMap";

            var tree = new TreeSyntaxNode();
            var componentsSegment = new TreeSyntaxNode();

            tree.Add(new CompositeSyntax(new UsingSyntax("System.Collections.Generic"), new ParagraphSyntax()));
            tree.Add(new CompositeSyntax(new UsingSyntax("HECSFrameWork.Components"), new ParagraphSyntax()));
            tree.Add(new ParagraphSyntax());
            tree.Add(new NameSpaceSyntax(DefaultNameSpace));
            tree.Add(new LeftScopeSyntax());
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(1), new SimpleSyntax($"public static class {className}"), new ParagraphSyntax()));
            tree.Add(new LeftScopeSyntax(1));

            //size for componentsArray
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax($"public static int SizeOfComponents = {componentTypes.Count};")));
            tree.Add(new ParagraphSyntax());
            tree.Add(new ParagraphSyntax());

            //dictionaty
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax("public static readonly Dictionary<int, ComponentMaskAndIndex> MapIndexes = new Dictionary<int, ComponentMaskAndIndex>")));
            tree.Add(new ParagraphSyntax());
            tree.Add(new LeftScopeSyntax(2));
            tree.Add(componentsSegment);
            tree.Add(new RightScopeSyntax(2, true));
            tree.Add(new RightScopeSyntax(1));
            tree.Add(new RightScopeSyntax());

            //default stroke in dictionary
            componentsSegment.Add(new CompositeSyntax(new TabSpaceSyntax(3), new SimpleSyntax("{ -1, new ComponentMaskAndIndex { ComponentsMask = ComponentsMask.Empty, ComponentID = Components.ComponentID.Default }},")));

            //here we know how much mask field we have
            var m = ComponentsCount();

            for (int i = 0; i < componentTypesByClass.Count; i++)
            {
                Type c = componentTypesByClass[i];
                var index = i;
                componentsSegment.Add(GetComponentForTypeMap(index, m, c));
            }

            var result = tree.ToString();
            SaveTypeMap(result);
        }

        private void UpdateComponentID()
        {
            var tree = new TreeSyntaxNode();
            var componentdIDS = new TreeSyntaxNode();

            tree.Add(new NameSpaceSyntax("HECSFrameWork.Components"));
            tree.Add(new LeftScopeSyntax());
            tree.Add(new TabSimpleSyntax(1, "public enum ComponentID"));
            tree.Add(new LeftScopeSyntax(1));
            tree.Add(componentdIDS);
            tree.Add(new RightScopeSyntax(1));
            tree.Add(new RightScopeSyntax());

            componentdIDS.Add(new TabSimpleSyntax(2, "Default"+ CParse.Comma));

            foreach (var c in componentTypesByClass)
                componentdIDS.Add(new TabSimpleSyntax(2, GetComponentID(c) + CParse.Comma));

            SaveComponentEnumIDS(tree.ToString());
        }

        private string GetComponentID(Type component)
        {
            //исключение, так как мы уже это везде используем, но не хочется получать IDID на конце
            if (component.Name.Contains("ActorContainerID"))
                return "ActorContainerID";

            return $"{component.Name}ID";
        }

        private ISyntax GetComponentForTypeMap(int index, int fieldCount, Type c)
        {
            var composite = new TreeSyntaxNode();
            var MaskPart = new TreeSyntaxNode();
            var maskBody = new TreeSyntaxNode();

            composite.Add(new ParagraphSyntax());
            composite.Add(new TabSpaceSyntax(3));
            composite.Add(new SimpleSyntax(CParse.LeftScope));
            composite.Add(new CompositeSyntax(new SimpleSyntax(IndexGenerator.GetIndexForType(c).ToString() + CParse.Comma)));
            composite.Add(new SimpleSyntax($"new ComponentMaskAndIndex {{ComponentID = {typeof(ComponentID).Name}.{GetComponentID(c)}, ComponentsMask = new ComponentsMask"));
            composite.Add(new ParagraphSyntax());
            composite.Add(MaskPart);
            composite.Add(new CompositeSyntax(new TabSpaceSyntax(3), new SimpleSyntax("}},")));
            composite.Add(new ParagraphSyntax());

            MaskPart.Add(new LeftScopeSyntax(4));
            MaskPart.Add(maskBody);
            MaskPart.Add(new RightScopeSyntax(4));

            var maskSplitToArray = CalculateIndexesForMask(index, fieldCount);

            for (int i = 0; i < fieldCount; i++)
            {
                maskBody.Add(new CompositeSyntax(new TabSpaceSyntax(5), new SimpleSyntax($"Mask0{i + 1} = 1ul << {maskSplitToArray[i]},")));

                if (i > fieldCount - 1)
                    continue;

                maskBody.Add(new ParagraphSyntax());
            }

            return composite;
        }

        public int[] CalculateIndexesForMask(int index, int fieldCounts)
        {
            var t = new List<int>(new int[fieldCounts + 1]);

            var calculate = index;

            for (int i = 0; i < fieldCounts; i++)
            {
                if (calculate + 2 > 63)
                {
                    t[i] = 63;
                    calculate -= 61;
                    continue;
                }

                if (calculate < 63 && calculate >= 0)
                {
                    t[i] = calculate + 2;
                    calculate -= 100;
                    continue;
                }

                else if (calculate < 0)
                {
                    t[i] = 1;
                }
            }

            return t.ToArray();
        }
        #endregion

        #region GenerateComponentMask
        private void GenerateComponentMask()
        {
            var componentMaskName = "ComponentsMask";
            var componentsPeriodCount = ComponentsCount();


            //overallType
            var tree = new TreeSyntaxNode();

            //defaultMask
            var maskDefault = new TreeSyntaxNode();

            var fields = new TreeSyntaxNode();
            var operatorPlus = new TreeSyntaxNode();
            var operatorMinus = new TreeSyntaxNode();
            var isHaveBody = new TreeSyntaxNode();

            tree.Add(new NameSpaceSyntax(DefaultNameSpace));
            tree.Add(new LeftScopeSyntax());

            //className
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(),
                new ModificatorSyntax(CParse.Public), new SimpleSyntax(CParse.Struct+CParse.Space), new SimpleSyntax(componentMaskName), new ParagraphSyntax()));
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(), new LeftScopeSyntax()));
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax("public static ComponentsMask Empty => new ComponentsMask"), new ParagraphSyntax()));
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2), new LeftScopeSyntax()));

            //here we insert mask later
            tree.Add(new CompositeSyntax(maskDefault));
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2), new RightScopeSyntax { StringValue = CParse.RightScope + CParse.Semicolon }));

            //here we insertFields
            tree.Add(new ParagraphSyntax());
            tree.Add(new CompositeSyntax(fields));
            tree.Add(new ParagraphSyntax());

            //plus operator
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax("public static ComponentsMask operator +(ComponentsMask l, ComponentsMask r)"), new ParagraphSyntax()));
            tree.Add(new LeftScopeSyntax(2));
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(3), new SimpleSyntax("return new ComponentsMask"), new ParagraphSyntax()));
            tree.Add(new LeftScopeSyntax(3));
            tree.Add(operatorPlus);
            tree.Add(new RightScopeSyntax(3, true));
            tree.Add(new RightScopeSyntax(2));

            tree.Add(new ParagraphSyntax());
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax("public static ComponentsMask operator -(ComponentsMask l, ComponentsMask r)"), new ParagraphSyntax()));
            tree.Add(new LeftScopeSyntax(2));
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(3), new SimpleSyntax("return new ComponentsMask"), new ParagraphSyntax()));
            tree.Add(new LeftScopeSyntax(3));
            tree.Add(operatorMinus);
            tree.Add(new RightScopeSyntax(3, true));
            tree.Add(new RightScopeSyntax(2));

            //bool IsHave
            tree.Add(new ParagraphSyntax());
            tree.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax("public bool IsHave(ref ComponentsMask mask)"), new ParagraphSyntax()));
            tree.Add(new LeftScopeSyntax(2));
            tree.Add(isHaveBody);
            isHaveBody.Add(new CompositeSyntax(new TabSpaceSyntax(3), new SimpleSyntax(CParse.Return), new SpaceSyntax()));
            tree.Add(new ParagraphSyntax());
            tree.Add(new RightScopeSyntax(2));
            tree.Add(new RightScopeSyntax(1));
            tree.Add(new RightScopeSyntax());

            //fill trees
            for (int i = 0; i < componentsPeriodCount; i++)
            {
                maskDefault.Add(new CompositeSyntax(new TabSpaceSyntax(3), new SimpleSyntax($"Mask0{i + 1} = 1 << 1,"), new ParagraphSyntax()));
                fields.Add(new CompositeSyntax(new TabSpaceSyntax(2), new SimpleSyntax($"public ulong Mask0{i + 1};"), new ParagraphSyntax()));
                operatorPlus.Add(new CompositeSyntax(new TabSpaceSyntax(4), new SimpleSyntax($"Mask0{i + 1} = l.Mask0{i + 1} | r.Mask0{i + 1},"), new ParagraphSyntax()));
                operatorMinus.Add(new CompositeSyntax(new TabSpaceSyntax(4), new SimpleSyntax($"Mask0{i + 1} = l.Mask0{i + 1} ^ r.Mask0{i + 1},"), new ParagraphSyntax()));

                if (i == 0)
                    isHaveBody.Add(new SimpleSyntax($"(Mask0{i + 1} & mask.Mask0{i + 1}) == mask.Mask0{i + 1}"));
                else
                    isHaveBody.Add(new CompositeSyntax(new ParagraphSyntax(), new TabSpaceSyntax(6), new SimpleSyntax("&&"), new SimpleSyntax($"(Mask0{i + 1} & mask.Mask0{i + 1}) == mask.Mask0{i + 1}")));
            }

            //add final semicolon
            isHaveBody.Add(new SimpleSyntax(CParse.Semicolon));

            var result = tree.ToString();
            SaveComponentMask(result);
        }
        #endregion

        #region Helpers
        private int ComponentsCount()
        {
            double count = (double)componentTypes.Count;

            if (count == 0)
                ++count;

            var componentsPeriodCount = (Math.Ceiling(count / 61));

            return (int)componentsPeriodCount;
        }

        public void GatherAssembly()
        {
            var componentType = typeof(IComponent);
            var networkType = typeof(INetworkComponent);
            var netWorkCommand = typeof(INetworkCommand);

            var asses = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes());
            componentTypes = asses.Where(p => componentType.IsAssignableFrom(p) && !p.IsGenericType && p.IsInterface 
                && p.Name != "IComponent" 
                && p.Name != "INetworkComponentUpdatable"
                && p.Name != "ISyncComponent"
                && p.Name != "IInteractiveComponent"
                && p.Name != "IAfterSyncComponent"
                && p.Name != "ISyncToSelf"
                && p.Name != "INetworkComponent")
                    .ToList();

            componentTypesByClass = asses.Where(p => p.Name != "BaseComponent" 
            && componentType.IsAssignableFrom(p) && !p.IsAbstract && !p.IsGenericType && !p.IsInterface).ToList();

            commands = asses.Where(x => netWorkCommand.IsAssignableFrom(x) && x.IsValueType).ToList();
        }
        #endregion
    }
}