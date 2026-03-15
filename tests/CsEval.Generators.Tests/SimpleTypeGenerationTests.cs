using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CsEval.Generators.Tests;

[TestFixture]
public class SimpleTypeGenerationTests
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
    public void SimpleType_GeneratesPropertyAccessors()
    {
        var source = """
            using CsEval;

            namespace TestTypes
            {
                public class MyModel
                {
                    public string Name { get; set; }
                    public int Value { get; set; }
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("TryGetProperty"));
        Assert.That(generated, Does.Contain("typed.Name"));
        Assert.That(generated, Does.Contain("typed.Value"));
        Assert.That(generated, Does.Contain("(global::TestTypes.MyModel)instance"));
    }

    [Test]
    public void MultipleTypes_GeneratesMetadataForEach()
    {
        var source = """
            using CsEval;

            namespace TestTypes
            {
                public class TypeA
                {
                    public string Foo { get; set; }
                }

                public class TypeB
                {
                    public int Bar { get; set; }
                }

                [CsEvalRegistered(typeof(TypeA))]
                [CsEvalRegistered(typeof(TypeB))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("global::TestTypes.TypeA"));
        Assert.That(generated, Does.Contain("global::TestTypes.TypeB"));
        Assert.That(generated, Does.Contain("s_metadata"));
    }

    [Test]
    public void Context_GeneratesDefaultSingleton()
    {
        var source = """
            using CsEval;

            namespace TestTypes
            {
                public class MyModel
                {
                    public int Id { get; set; }
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("public static TestContext Default { get; } = new();"));
    }

    [Test]
    public void Context_OverridesGetTypeMetadata()
    {
        var source = """
            using CsEval;

            namespace TestTypes
            {
                public class MyModel
                {
                    public int Id { get; set; }
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("override"));
        Assert.That(generated, Does.Contain("GetTypeMetadata()"));
    }

    [Test]
    public void MultipleConstructors_GeneratesSwitchOnArgLength()
    {
        var source = """
            using CsEval;

            namespace TestTypes
            {
                public class MyModel
                {
                    public string Name { get; set; }
                    public int Value { get; set; }

                    public MyModel() { }
                    public MyModel(string name, int value)
                    {
                        Name = name;
                        Value = value;
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
        Assert.That(generated, Does.Contain("args.Length"));
        Assert.That(generated, Does.Contain("case 0:"));
        Assert.That(generated, Does.Contain("case 2:"));
    }
}
