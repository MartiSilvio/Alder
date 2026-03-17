using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CsEval.Generators.Tests;

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
    public void ReadOnlyProperty_OmittedFromTrySetProperty()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public string Name { get; }

                    public MyModel(string name) { Name = name; }
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryGetSection = ExtractMethod(generated, "TryGetProperty");
        var trySetSection = ExtractMethod(generated, "TrySetProperty");

        Assert.That(tryGetSection, Does.Contain("\"Name\""));
        Assert.That(trySetSection, Does.Not.Contain("\"Name\""));
    }

    [Test]
    public void ReadOnlyField_OmittedFromTrySetField()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public readonly int Id;

                    public MyModel(int id) { Id = id; }
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryGetSection = ExtractMethod(generated, "TryGetField");
        var trySetSection = ExtractMethod(generated, "TrySetField");

        Assert.That(tryGetSection, Does.Contain("\"Id\""));
        Assert.That(trySetSection, Does.Not.Contain("\"Id\""));
    }

    [Test]
    public void StaticMembers_InTryGetStaticPropertyAndField()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public static string Label { get; set; }
                    public static int Count;
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryGetStaticProp = ExtractMethod(generated, "TryGetStaticProperty");
        var tryGetStaticField = ExtractMethod(generated, "TryGetStaticField");

        Assert.That(tryGetStaticProp, Does.Contain("\"Label\""));
        Assert.That(tryGetStaticField, Does.Contain("\"Count\""));
    }

    [Test]
    public void Indexer_GeneratesTryGetIndexAndTrySetIndex()
    {
        var source = """
            using CsEval.Aot;
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

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
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
            using CsEval.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    private readonly int[] _data = { 1, 2, 3 };
                    public int this[int i] => _data[i];
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
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
