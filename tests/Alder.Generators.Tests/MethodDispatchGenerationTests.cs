using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Alder.Generators.Tests;

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
    public void InstanceMethod_GeneratesTryInvoke()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public int Add(int a, int b) => a + b;
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvoke");
        Assert.That(tryInvoke, Does.Contain("case \"Add\":"));
        Assert.That(tryInvoke, Does.Contain("case 2:"));
    }

    [Test]
    public void StaticMethod_GeneratesTryInvokeStatic()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public static int Multiply(int x, int y) => x * y;
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvokeStatic = ExtractMethod(generated, "TryInvokeStatic");
        Assert.That(tryInvokeStatic, Does.Contain("case \"Multiply\":"));
        Assert.That(tryInvokeStatic, Does.Contain("case 2:"));
    }

    [Test]
    public void VoidMethod_EmitsNullResultPattern()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public void Reset() { }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvoke");
        Assert.That(tryInvoke, Does.Contain("case \"Reset\":"));
        Assert.That(tryInvoke, Does.Contain("result = null;"));
        Assert.That(tryInvoke, Does.Not.Contain("result = typed.Reset()"));
    }

    [Test]
    public void OverloadedMethods_GeneratesArgLengthSwitch()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public string Format() => "default";
                    public string Format(string template) => template;
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvoke");
        Assert.That(tryInvoke, Does.Contain("case \"Format\":"));
        Assert.That(tryInvoke, Does.Contain("case 0:"));
        Assert.That(tryInvoke, Does.Contain("case 1:"));
    }

    [Test]
    public void SameArityOverloads_GeneratesTypeCheckedDispatch()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public string Describe(int value) => value.ToString();
                    public string Describe(string value) => value;
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvoke");
        Assert.That(tryInvoke, Does.Contain("case \"Describe\":"));
        Assert.That(tryInvoke, Does.Contain("case 1:"));
        Assert.That(tryInvoke, Does.Contain("args[0] is int"));
        Assert.That(tryInvoke, Does.Contain("args[0] is string"));
    }

    [Test]
    public void GenericMethod_IsNotExpandedIntoAotDispatch()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public T Identity<T>(T value) => value;
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvoke");
        Assert.That(tryInvoke, Does.Not.Contain("\"Identity\""),
            "Generic methods are outside the bounded AOT dispatch surface.");
    }

    [Test]
    public void OutParameter_GeneratesDispatchAndCopiesBack()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public bool TryParse(string s, out int result) { result = 0; return false; }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvoke");
        Assert.That(tryInvoke, Does.Contain("\"TryParse\""));
        Assert.That(tryInvoke, Does.Contain("int __out1 = default;"));
        Assert.That(tryInvoke, Does.Contain("out __out1"));
        Assert.That(tryInvoke, Does.Contain("args[1] = __out1;"));
    }

    [Test]
    public void NullableReferenceOutParameter_UsesNullableLocal()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public bool TryRead(out string? value) { value = null; return false; }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvoke");
        Assert.That(tryInvoke, Does.Contain("string? __out0 = default;"));
        Assert.That(tryInvoke, Does.Contain("result = typed.TryRead(out __out0);"));
    }

    [Test]
    public void MaybeNullOutParameter_UsesNullableLocal()
    {
        var source = """
            using Alder.Aot;
            using System.Diagnostics.CodeAnalysis;

            namespace TestTypes
            {
                public class MyModel
                {
                    public bool TryRead([MaybeNullWhen(false)] out string value) { value = null!; return false; }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvoke");
        Assert.That(tryInvoke, Does.Contain("string? __out0 = default;"));
    }

    [Test]
    public void RefParameter_RemainsOutsideBoundedAotSurface()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public void Bump(ref int value) { value++; }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvoke");
        Assert.That(tryInvoke, Does.Not.Contain("\"Bump\""));
    }

    [Test]
    public void RootedTaskOfT_GeneratesExplicitGenericStaticDispatch()
    {
        var source = """
            using Alder.Aot;
            using System.Threading.Tasks;

            namespace TestTypes
            {
                [AlderRegistered(typeof(Task<int>))]
                [AlderRegistered(typeof(Task<string>))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("GetGenericStaticDispatch()"));
        Assert.That(generated, Does.Contain("TaskFromResultDispatch"));
        Assert.That(generated, Does.Contain("nameof(global::System.Threading.Tasks.Task.FromResult)"));
        Assert.That(generated, Does.Contain("requestedType == typeof(int)"));
        Assert.That(generated, Does.Contain("requestedType == typeof(string)"));
    }

    [Test]
    public void ParamsMethod_GeneratesNormalAndExpandedDispatch()
    {
        var source = """
            using Alder.Aot;
            using System.Linq;

            namespace TestTypes
            {
                public class MyModel
                {
                    public int Sum(params int[] values) => values.Sum();
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");

        var tryInvoke = ExtractMethod(generated, "TryInvoke");
        Assert.That(tryInvoke, Does.Contain("case \"Sum\":"));
        Assert.That(tryInvoke, Does.Contain("args[0] is int[]"), "Normal form: caller passes the array directly");
        Assert.That(tryInvoke, Does.Contain("__paramsArr"), "Expanded form: individual elements collected into array");
    }

    [Test]
    public void DelegateParameters_DoNotEmitGeneratedDelegateFactoryMachinery()
    {
        var source = """
            using Alder.Aot;
            using System;

            namespace TestTypes
            {
                public class MyModel
                {
                    public int Apply(Func<int, int> selector, int value) => selector(value);
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Not.Contain("AotDelegateFactories"),
            "AOT generator should not synthesize delegate factory breadth for registered delegate parameters.");
        Assert.That(generated, Does.Not.Contain("GetDelegateFactories()"),
            "Generated contexts should stay focused on explicit typed dispatch.");
    }

    [Test]
    public void AllTestCases_NoCompilationErrors()
    {
        var source = """
            using Alder.Aot;
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

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
    }

    private static string ExtractMethod(string source, string methodName)
    {
        var searchStr = $"public override bool {methodName}(";
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
