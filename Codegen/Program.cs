using HECSFramework.Core.Generator;
using HECSFrameWork;
using HECSFrameWork.Systems;
using HECSv2.Core.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Codegen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            TestRoslyn();
            GenerateBindings();

            var generateAll = new GenerateAll();

            generateAll.StartGeneration();
        }

        private static void TestRoslyn()
        {
            string path = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName);
            var files = Directory.GetFiles(path, "CommandMap.cs", SearchOption.AllDirectories);
            var check = File.ReadAllText(files[0]);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(check);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
        }

        public static void GenerateBindings()
        {
            string path = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName);
            var files = Directory.GetDirectories(path, "CurrentGame", SearchOption.AllDirectories);
            var check = files[0];

            var sourceFile = check + "\\" + @"RegisterService.cs";
            var asses = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes());
            var localreact = typeof(ICommandReact<>);
            var globalReact = typeof(IGlobalCommandReact<>);

            var localReactTypes = asses.SelectMany(p => p.GetInterfaces()).Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == localreact);
            var globalReactTypes = asses.SelectMany(p => p.GetInterfaces()).Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == globalReact);

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace("HECSFrameWork");
            codeNamespace.Imports.Add(new CodeNamespaceImport("Components"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("Commands"));

            CodeTypeDeclaration neededClass = new CodeTypeDeclaration()
            {
                Name = "RegisterService",
                IsClass = true,
                IsPartial = true,
            };

            codeNamespace.Types.Add(neededClass);
            compileUnit.Namespaces.Add(codeNamespace);

            CodeMemberMethod bindingMethod = new CodeMemberMethod();
            bindingMethod.Attributes = MemberAttributes.Private | MemberAttributes.Final;

            bindingMethod.Name = "BindSystem";
            var sys = new CodeParameterDeclarationExpression("T", "system");
            CodeTypeParameter tType = new CodeTypeParameter("T");
            bindingMethod.TypeParameters.Add(tType);

            tType.Constraints.Add(typeof(ISystem));
            bindingMethod.Parameters.Add(sys);

            HashSet<Type> genTypes = new HashSet<Type>();
            HashSet<Type> genTypesGlobal = new HashSet<Type>();

            foreach (var c in localReactTypes)
            {
                foreach (var type in c.GetGenericArguments())
                    genTypes.Add(type);
            }

            foreach (var c in globalReactTypes)
            {
                foreach (var type in c.GetGenericArguments())
                    genTypesGlobal.Add(type);
            }

            foreach (var neededType in genTypes)
            {
                var func = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("SystemsExtensions"),
                    $"ListenCommand<{neededType.Name}>", new CodeVariableReferenceExpression($"system, {neededType.Name}Listener.React"));

                CodeConditionStatement conditionalStatement = new CodeConditionStatement
                    (
                        new CodeVariableReferenceExpression($"system is ICommandReact<{neededType.Name}> {neededType.Name}Listener"),
                        new CodeStatement[] { new CodeExpressionStatement(func) }
                    );

                bool have = false;

                foreach (var ns in codeNamespace.Imports)
                {
                    if ((ns as CodeNamespaceImport).Namespace == neededType.Namespace)
                    {
                        have = true;
                        break;
                    }

                }

                if (!have)
                    codeNamespace.Imports.Add(new CodeNamespaceImport(neededType.Namespace));

                bindingMethod.Statements.Add(conditionalStatement);
            }

            foreach (var neededType in genTypesGlobal)
            {
                var func = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("SystemsExtensions"),
                    $"ListenCommandGlobal<{neededType.Name}>", new CodeVariableReferenceExpression($"system, {neededType.Name}GListener.GlobalReact"));

                CodeConditionStatement conditionalStatement = new CodeConditionStatement
                    (
                        new CodeVariableReferenceExpression($"system is IGlobalCommandReact<{neededType.Name}> {neededType.Name}GListener"),
                        new CodeStatement[] { new CodeExpressionStatement(func) }
                    );

                bool have = false;

                foreach (var ns in codeNamespace.Imports)
                {
                    if ((ns as CodeNamespaceImport).Namespace == neededType.Namespace)
                    {
                        have = true;
                        break;
                    }

                }

                if (!have)
                    codeNamespace.Imports.Add(new CodeNamespaceImport(neededType.Namespace));

                bindingMethod.Statements.Add(conditionalStatement);
            }

            neededClass.Members.Add(bindingMethod);

            using (System.IO.StreamWriter sw = System.IO.File.CreateText(sourceFile))
            {
                provider.GenerateCodeFromCompileUnit(compileUnit, sw,
                    new CodeGeneratorOptions());
            }
        }
    }
}
