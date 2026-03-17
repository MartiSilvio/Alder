using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CsEval.Generators.Tests;

[TestFixture]
public class MethodDispatchGenerationTests
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
    public void InstanceMethod_GeneratesTryInvokeMethod()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public int Add(int a, int b) => a + b;
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvokeMethod");
        Assert.That(tryInvoke, Does.Contain("case \"Add\":"));
        Assert.That(tryInvoke, Does.Contain("case 2:"));
    }

    [Test]
    public void StaticMethod_GeneratesTryInvokeStaticMethod()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public static int Multiply(int x, int y) => x * y;
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvokeStatic = ExtractMethod(generated, "TryInvokeStaticMethod");
        Assert.That(tryInvokeStatic, Does.Contain("case \"Multiply\":"));
        Assert.That(tryInvokeStatic, Does.Contain("case 2:"));
    }

    [Test]
    public void VoidMethod_EmitsNullResultPattern()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public void Reset() { }
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvokeMethod");
        Assert.That(tryInvoke, Does.Contain("case \"Reset\":"));
        Assert.That(tryInvoke, Does.Contain("result = null;"));
        Assert.That(tryInvoke, Does.Not.Contain("result = typed.Reset()"));
    }

    [Test]
    public void OverloadedMethods_GeneratesArgLengthSwitch()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public string Format() => "default";
                    public string Format(string template) => template;
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvokeMethod");
        Assert.That(tryInvoke, Does.Contain("case \"Format\":"));
        Assert.That(tryInvoke, Does.Contain("case 0:"));
        Assert.That(tryInvoke, Does.Contain("case 1:"));
    }

    [Test]
    public void GenericMethod_Skipped()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public T Identity<T>(T value) => value;
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvokeMethod");
        Assert.That(tryInvoke, Does.Not.Contain("\"Identity\""));
    }

    [Test]
    public void RefOutParameter_Skipped()
    {
        var source = """
            using CsEval.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public bool TryParse(string s, out int result) { result = 0; return false; }
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvokeMethod");
        Assert.That(tryInvoke, Does.Not.Contain("\"TryParse\""));
    }

    [Test]
    public void ParamsMethod_Skipped()
    {
        var source = """
            using CsEval.Aot;
            using System.Linq;

            namespace TestTypes
            {
                public class MyModel
                {
                    public int Sum(params int[] values) => values.Sum();
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvokeMethod");
        Assert.That(tryInvoke, Does.Not.Contain("\"Sum\""));
    }

    [Test]
    public void AllTestCases_NoCompilationErrors()
    {
        var source = """
            using CsEval.Aot;
            using System.Linq;

            namespace TestTypes
            {
                public class MyModel
                {
                    public int Add(int a, int b) => a + b;
                    public static int Multiply(int x, int y) => x * y;
                    public void Reset() { }
                    public string Format() => "default";
                    public string Format(string template) => template;
                    public T Identity<T>(T value) => value;
                    public bool TryParse(string s, out int result) { result = 0; return false; }
                    public int Sum(params int[] values) => values.Sum();
                }

                [CsEvalRegistered(typeof(MyModel))]
                public partial class TestContext : CsEvalTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
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
