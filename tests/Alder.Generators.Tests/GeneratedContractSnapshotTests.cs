using NUnit.Framework;

namespace Alder.Generators.Tests;

[TestFixture]
public class GeneratedContractSnapshotTests
{
    private const string RichContractSource = """
        using Alder.Aot;
        using System.Threading.Tasks;
        using System.Collections.Generic;

        namespace TestTypes
        {
            public class MyModel
            {
                private readonly Dictionary<string, int> _data = new();

                public string Name { get; set; } = string.Empty;
                public int Count;
                public static int GlobalCount { get; set; }

                public int this[string key]
                {
                    get => _data[key];
                    set => _data[key] = value;
                }

                public MyModel() { }
                public MyModel(string name) { Name = name; }

                public int Add(int a, int b) => a + b;
                public string Describe(int value) => value.ToString();
                public string Describe(string value) => value;
                public int SumAll(params int[] values) => values.Length;
                public bool TryRead(out string? value) { value = Name; return true; }

                public static int Multiply(int x, int y) => x * y;
                public T Identity<T>(T value) => value;
            }

            [AlderRegistered(typeof(MyModel))]
            [AlderRegistered(typeof(Task<int>))]
            public partial class TestContext : AlderTypeContext { }
        }
        """;

    [Test]
    public void GeneratedContext_ContractSnapshot_RemainsStable()
    {
        var (_, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(RichContractSource);
        AssertNoCompilationErrors(outputCompilation);

        var generated = string.Join("\n", generatedTrees.Select(t => t.GetText().ToString()));
        var snapshot = GeneratedContractSnapshotHelper.BuildContextContractSnapshot(generated, "TestContext");

        GeneratedContractSnapshotHelper.AssertMatchesSnapshot("context-contract.txt", snapshot);
    }

    [Test]
    public void TypedDispatch_ContractSnapshot_RemainsStable()
    {
        var (_, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(RichContractSource);
        AssertNoCompilationErrors(outputCompilation);

        var generated = string.Join("\n", generatedTrees.Select(t => t.GetText().ToString()));
        var snapshot = GeneratedContractSnapshotHelper.BuildTypedDispatchContractSnapshot(generated, "TestTypes_MyModelMetadata");

        GeneratedContractSnapshotHelper.AssertMatchesSnapshot("typed-dispatch-contract.txt", snapshot);
    }

    private static void AssertNoCompilationErrors(Microsoft.CodeAnalysis.Compilation compilation)
    {
        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
    }
}
