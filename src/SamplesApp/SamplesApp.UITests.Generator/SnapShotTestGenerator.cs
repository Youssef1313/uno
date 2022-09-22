﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.Roslyn;

namespace Uno.Samples.UITest.Generator
{
	[Generator]
	public class SnapShotTestGenerator : ISourceGenerator
	{
		private const int GroupCount = 5;

		public void Initialize(GeneratorInitializationContext context)
		{
			UI.SourceGenerators.DependenciesInitializer.Init();
		}

		public void Execute(GeneratorExecutionContext context)
		{
#if DEBUG
			// Debugger.Launch();
#endif
			try
			{
				if (context.Compilation.Assembly.Name == "SamplesApp.UITests")
				{
					GenerateTests(context, "Uno.UI.Samples");
				}
			}
			catch(Exception e)
			{
				if (e is ReflectionTypeLoadException)
				{
					var typeLoadException = e as ReflectionTypeLoadException;
					var loaderExceptions = typeLoadException.LoaderExceptions;

					StringBuilder sb = new();
					foreach (var loaderException in loaderExceptions)
					{
						sb.Append(loaderException.ToString());
					}

					throw new Exception(sb.ToString());
				}

				throw;
			}
		}

		private void GenerateTests(GeneratorExecutionContext context, string assembly)
		{
			var compilation = GetCompilation(context);

			context.AddSource("debug", $"// inner compilation:{compilation.Assembly.Name}");

			var sampleControlInfoSymbol = compilation.GetTypeByMetadataName("Uno.UI.Samples.Controls.SampleControlInfoAttribute");
			var sampleSymbol = compilation.GetTypeByMetadataName("Uno.UI.Samples.Controls.SampleAttribute");

			var query = from typeSymbol in compilation.SourceModule.GlobalNamespace.GetNamespaceTypes()
						where typeSymbol.DeclaredAccessibility == Accessibility.Public
						let info = typeSymbol.FindAttributeFlattened(sampleSymbol) ?? typeSymbol.FindAttributeFlattened(sampleControlInfoSymbol)
						where info != null
						let sampleInfo = GetSampleInfo(typeSymbol, info, sampleControlInfoSymbol)
						orderby sampleInfo.categories.First()
						select (typeSymbol, sampleInfo.categories, sampleInfo.name, sampleInfo.ignoreInSnapshotTests, sampleInfo.isManual);

			query = query.Distinct();

			GenerateTests(assembly, context, query);
		}

		private static (string[] categories, string name, bool ignoreInSnapshotTests, bool isManual) GetSampleInfo(INamedTypeSymbol symbol, AttributeData attr, INamedTypeSymbol sampleControlInfoSymbol)
		{
			if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, sampleControlInfoSymbol))
			{
				return (
					categories: new[] { GetConstructorParameterValue(attr, "category")?.ToString() ?? "Default" },
					name: AlignName(GetConstructorParameterValue(attr, "controlName")?.ToString() ?? symbol.ToDisplayString()),
					ignoreInSnapshotTests: GetConstructorParameterValue(attr, "ignoreInSnapshotTests") is bool b && b,
					isManual: GetConstructorParameterValue(attr, "isManualTest") is bool m && m
				);
			}
			else
			{
				var categories = attr
					.ConstructorArguments
					.Where(arg => arg.Kind == TypedConstantKind.Array)
					.Select(arg => GetCategories(arg.Values))
					.SingleOrDefault()
					?? GetCategories(attr.ConstructorArguments);

				if (categories?.Any(string.IsNullOrWhiteSpace) ?? false)
				{
					throw new InvalidOperationException(
						"Invalid syntax for the SampleAttribute (found an empty category name). "
						+ "Usually this is because you used nameof(Control) to set the categories, which is not supported by the compiler. "
						+ "You should instead use the overload which accepts type (i.e. use typeof() instead of nameof()).");
				}

				return (
					categories: (categories?.Any() ?? false) ? categories : new[] { "Default" },
					name: AlignName(GetAttributePropertyValue(attr, "Name")?.ToString() ?? symbol.ToDisplayString()),
					ignoreInSnapshotTests: GetAttributePropertyValue(attr, "IgnoreInSnapshotTests") is bool b && b,
					isManual: GetAttributePropertyValue(attr, "IsManualTest") is bool m && m
					);

				string[] GetCategories(ImmutableArray<TypedConstant> args) => args
					.Select(v =>
					{
						switch (v.Kind)
						{
							case TypedConstantKind.Primitive: return v.Value.ToString();
							case TypedConstantKind.Type: return ((ITypeSymbol)v.Value).Name;
							default: return null;
						}
					})
					.ToArray();
			}
		}

		private static object GetAttributePropertyValue(AttributeData attr, string name)
			=> attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == name).Value.Value;

		private static object GetConstructorParameterValue(AttributeData info, string name)
			=> info.ConstructorArguments.IsDefaultOrEmpty
				? default
				: info.ConstructorArguments.ElementAt(GetParameterIndex(info, name)).Value;

		private static int GetParameterIndex(AttributeData info, string name)
			=> info
				.AttributeConstructor
				.Parameters
				.Select((p, i) => (p, i))
				.Single(p => p.p.Name == name)
				.i;

		private static string AlignName(string v)
			=> v.Replace('/', '_').Replace(' ', '_').Replace('-', '_').Replace(':', '_');

		private static void GenerateTests(
			string assembly,
			GeneratorExecutionContext context,
			IEnumerable<(INamedTypeSymbol symbol, string[] categories, string name, bool ignoreInSnapshotTests, bool isManual)> symbols)
		{
			var groups =
				from symbol in symbols.Select((v, i) => (index: i, value: v))
				group symbol by symbol.index / 50 into g
				select new
				{
					Index = g.Key,
					Symbols = g.AsEnumerable().Select(v => v.value)
				};

			foreach (var group in groups)
			{
				string sanitizedAssemblyName = assembly.Replace('.', '_');
				var groupName = $"Generated_{sanitizedAssemblyName}_{group.Index:000}";

				var builder = new IndentedStringBuilder();

				builder.AppendLineIndented("using System;");

				using (builder.BlockInvariant($"namespace {context.GetMSBuildPropertyValue("RootNamespace")}.Snap"))
				{
					builder.AppendLineIndented("[global::NUnit.Framework.TestFixture]");

					// Required for https://github.com/unoplatform/uno/issues/1955
					builder.AppendLineIndented("[global::SamplesApp.UITests.TestFramework.TestAppModeAttribute(cleanEnvironment: true, platform: Uno.UITest.Helpers.Queries.Platform.iOS)]");

					using (builder.BlockInvariant($"public partial class {groupName} : SampleControlUITestBase"))
					{
						foreach (var test in group.Symbols)
						{
							builder.AppendLineIndented("[global::NUnit.Framework.Test]");
							builder.AppendLineIndented($"[global::NUnit.Framework.Description(\"runGroup:{group.Index % GroupCount:00}, automated:{test.symbol.ToDisplayString()}\")]");

							var (ignored, ignoreReason) = (test.ignoreInSnapshotTests, test.isManual) switch
							{
								(true, true) => (true, "ignoreInSnapshotTests and isManual are set for attribute"),
								(true, false) => (true, "ignoreInSnapshotTests is are set for attribute"),
								(false, true) => (true, "isManualTest is set for attribute"),
								_ => (false, default),
							};
							if (ignored)
							{
								builder.AppendLineIndented($"[global::NUnit.Framework.Ignore(\"{ignoreReason}\")]");
							}

							builder.AppendLineIndented("[global::SamplesApp.UITests.TestFramework.AutoRetry]");
							// Set to 60 seconds to cover possible restart of the device
							builder.AppendLineIndented("[global::NUnit.Framework.Timeout(60000)]");
							var testName = $"{Sanitize(test.categories.First())}_{Sanitize(test.name)}";
							using (builder.BlockInvariant($"public void {testName}()"))
							{
								builder.AppendLineIndented($"Console.WriteLine(\"Running test [{testName}]\");");
								builder.AppendLineIndented($"Run(\"{test.symbol}\", waitForSampleControl: false);");
								builder.AppendLineIndented($"Console.WriteLine(\"Ran test [{testName}]\");");
							}
						}
					}
				}

				context.AddSource(groupName, builder.ToString());
			}
		}

		private static object Sanitize(string category)
			=> string.Join("", category.Select(c => char.IsLetterOrDigit(c) ? c : '_'));

		private static Compilation GetCompilation(GeneratorExecutionContext context)
		{
			// Used to get the reference assemblies
			var devEnvDir = context.GetMSBuildPropertyValue("MSBuildExtensionsPath");

			if (devEnvDir.StartsWith("*"))
			{
				throw new Exception($"The reference assemblies path is not defined");
			}

			var ws = new AdhocWorkspace();

			var project = ws.CurrentSolution.AddProject("temp", "temp", LanguageNames.CSharp);

			var referenceFiles = new[] {
				typeof(object).Assembly.CodeBase,
				typeof(Attribute).Assembly.CodeBase,
			};

			foreach (var file in referenceFiles.Distinct())
			{
				project = project.AddMetadataReference(MetadataReference.CreateFromFile(new Uri(file).LocalPath));
			}

			project = AddFiles(context, project, "UITests.Shared");
			project = AddFiles(context, project, "SamplesApp.UnitTests.Shared");

			var compilation = project.GetCompilationAsync().Result;

			return compilation;
		}

		private static Project AddFiles(GeneratorExecutionContext context, Project project, string baseName)
		{
			var sourcePath = Path.Combine(context.GetMSBuildPropertyValue("MSBuildProjectDirectory"), "..", baseName);
			foreach (var file in Directory.GetFiles(sourcePath, "*.cs", SearchOption.AllDirectories))
			{
				project = project.AddDocument(Path.GetFileName(file), File.ReadAllText(file)).Project;
			}

			return project;
		}
	}
}
