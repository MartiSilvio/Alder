using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CsEval.Generators.Tests;

[TestFixture]
public class EdgeCaseGenerationTests
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
    public void TypeWithNoPublicMembers_GeneratesEmptyMetadata()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class EmptyModel
                {
                    private int _secret;
                    private string Hidden { get; set; }
                }

                [CsEvalRegistered(typeof(EmptyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("TryGetProperty"));
        Assert.That(generated, Does.Contain("return false;"));
    }

    [Test]
    public void NestedType_GeneratesCorrectTypeof()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class Outer
                {
                    public class Inner
                    {
                        public int X { get; set; }
                    }
                }

                [CsEvalRegistered(typeof(Outer.Inner))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("typeof(global::TestTypes.Outer.Inner)"));
    }

    [Test]
    public void ClosedGenericType_EmitsPreservationReference()
    {
        var source = """
            using CsEval.Aot;
            using System.Collections.Generic;

            namespace TestTypes
            {
                [CsEvalRegistered(typeof(List<int>))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("GenericInstantiations"));
        Assert.That(generated, Does.Contain("typeof(global::System.Collections.Generic.List<int>)"));
    }

    [Test]
    public void NonGenericType_NoGenericInstantiationsEmitted()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class SimpleModel
                {
                    public int Id { get; set; }
                }

                [CsEvalRegistered(typeof(SimpleModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Not.Contain("GenericInstantiations"));
    }

    [Test]
    public void TypeWithInheritedMembers_OnlyGeneratesDeclaredMembers()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class BaseModel
                {
                    public string BaseProp { get; set; }
                }

                public class DerivedModel : BaseModel
                {
                    public int DerivedProp { get; set; }
                }

                [CsEvalRegistered(typeof(DerivedModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("\"DerivedProp\""));
        Assert.That(generated, Does.Not.Contain("\"BaseProp\""));
    }

    [Test]
    public void MultipleContextClasses_GeneratesForBoth()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class ModelA
                {
                    public int X { get; set; }
                }

                public class ModelB
                {
                    public int Y { get; set; }
                }

                [CsEvalRegistered(typeof(ModelA))]
                public partial class ContextA : CsEvalTypeContext { }

                [CsEvalRegistered(typeof(ModelB))]
                public partial class ContextB : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generatedTrees.Length, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void ContextWithoutCsEvalRegistered_NoOutput()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public partial class EmptyContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);

        Assert.That(generatedTrees.Length, Is.EqualTo(0));
    }

    [Test]
    public void PropertyWithInitOnlySetter_OmittedFromTrySetProperty()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public string Name { get; init; }
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

    private static string ExtractMethod(string source, string methodName)
    {
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
