using HECSFramework.Core.Generator;
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
            

            var resolverGenerator = new CodeGenerator();

            
            var resolvers = resolverGenerator.GetSerializationResolvers();
            var resolversMap = resolverGenerator.GetResolverMap();

            foreach (var r in resolvers)
                SaveToFile(dataPath + DefaultPath + ComponentResolversFolder, r.name, r.content);

            SaveToFile(dataPath + DefaultPath + ComponentResolversFolder, ResolversMap, resolversMap);
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
    }
}