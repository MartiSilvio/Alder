using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Alder.Generators.Tests;

[TestFixture]
public class MemberAccessGenerationTests
{
    private static string[] GetCompilationErrors(Microsoft.CodeAnalysis.Compilation compilation)
        => compilation.GetDiagnostics()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();

    private static string GetAllGeneratedSource(
        System.Collections.Immutable.ImmutableArray<SyntaxTree> trees)
        => string.Join("\n", trees.Select(t => t.GetText().ToString()));

    [Test]
    public void ReadOnlyProperty_OmittedFromTrySet()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public string Name { get; }

                    public MyModel(string name) { Name = name; }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryGetSection = ExtractMethod(generated, "TryGet");
        var trySetSection = ExtractMethod(generated, "TrySet");

        Assert.That(tryGetSection, Does.Contain("\"Name\""));
        Assert.That(trySetSection, Does.Not.Contain("\"Name\""));
    }

    [Test]
    public void ReadOnlyField_OmittedFromTrySet()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public readonly int Id;

                    public MyModel(int id) { Id = id; }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryGetSection = ExtractMethod(generated, "TryGet");
        var trySetSection = ExtractMethod(generated, "TrySet");

        Assert.That(tryGetSection, Does.Contain("\"Id\""));
        Assert.That(trySetSection, Does.Not.Contain("\"Id\""));
    }

    [Test]
    public void StaticMembers_InTryGetStatic()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public static string Label { get; set; }
                    public static int Count;
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryGetStatic = ExtractMethod(generated, "TryGetStatic");

        Assert.That(tryGetStatic, Does.Contain("\"Label\""));
        Assert.That(tryGetStatic, Does.Contain("\"Count\""));
    }

    [Test]
    public void Indexer_GeneratesTryGetIndexAndTrySetIndex()
    {
        var source = """
            using Alder.Aot;
            using System.Collections.Generic;

            namespace TestTypes
            {
                public class MyModel
                {
                    private readonly Dictionary<string, int> _data = new();
                    public int this[string key]
                    {
                        get => _data[key];
                        set => _data[key] = value;
                    }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryGetIndex = ExtractMethod(generated, "TryGetIndex");
        var trySetIndex = ExtractMethod(generated, "TrySetIndex");

        Assert.That(tryGetIndex, Does.Contain("return true;"));
        Assert.That(trySetIndex, Does.Contain("return true;"));
    }

    [Test]
    public void ReadOnlyIndexer_OnlyGeneratesTryGetIndex()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    private readonly int[] _data = { 1, 2, 3 };
                    public int this[int i] => _data[i];
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryGetIndex = ExtractMethod(generated, "TryGetIndex");
        var trySetIndex = ExtractMethod(generated, "TrySetIndex");

        Assert.That(tryGetIndex, Does.Contain("return true;"));
        Assert.That(trySetIndex, Does.Not.Contain("return true;"));
    }

    private static string ExtractMethod(string source, string methodName)
    {
        // Find the method signature and extract until matching closing brace
        var searchStr = $"public bool {methodName}(";
        var idx = source.IndexOf(searchStr);
        if (idx < 0) return string.Empty;

        int braceCount = 0;
        bool started = false;
        int start = idx;
        for (int i = idx; i < source.Length; i++)
        {
            if (source[i] == '{') { braceCount++; started = true; }
            if (source[i] == '}') { braceCount--; }
            if (started && braceCount == 0)
                return source.Substring(start, i - start + 1);
        }
        return source.Substring(start);
    }
}
