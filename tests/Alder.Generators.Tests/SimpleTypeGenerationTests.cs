using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Alder.Generators.Tests;

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
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public string Name { get; set; }
                    public int Value { get; set; }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("TryGet"));
        Assert.That(generated, Does.Contain("typed.Name"));
        Assert.That(generated, Does.Contain("typed.Value"));
        Assert.That(generated, Does.Contain("(global::TestTypes.MyModel)instance"));
    }

    [Test]
    public void MultipleTypes_GeneratesMetadataForEach()
    {
        var source = """
            using Alder.Aot;

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

                [AlderRegistered(typeof(TypeA))]
                [AlderRegistered(typeof(TypeB))]
                public partial class TestContext : AlderTypeContext { }
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
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public int Id { get; set; }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
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
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public int Id { get; set; }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
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
    public void TypedDispatchTypeOverride_GuardsTrimAttributeForModernTargets()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public MyModel() { }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("#if NET5_0_OR_GREATER"));
        Assert.That(generated, Does.Contain("DynamicallyAccessedMembers"));
        Assert.That(generated, Does.Contain("DynamicallyAccessedMemberTypes.PublicConstructors"));
        Assert.That(generated, Does.Contain("public override global::System.Type Type => typeof(global::TestTypes.MyModel);"));
    }

    [Test]
    public void TypedDispatchTypeOverride_CompilesWithModernTrimAttributeEnabled()
    {
        var source = """
            using Alder.Aot;

            namespace TestTypes
            {
                public class MyModel
                {
                    public MyModel() { }
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(
            source,
            preprocessorSymbols: "NET5_0_OR_GREATER");
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers"));
        Assert.That(generated, Does.Contain("public override global::System.Type Type => typeof(global::TestTypes.MyModel);"));
    }

    [Test]
    public void TrimAndAotUnsafeMembers_AreExcludedFromGeneratedDispatch()
    {
        var source = """
            using Alder.Aot;
            using System.Diagnostics.CodeAnalysis;

            namespace TestTypes
            {
                public class MyModel
                {
                    public MyModel() { }

                    [RequiresDynamicCode("Not AOT-safe.")]
                    public MyModel(int value) { }

                    public string SafeProperty => "safe";

                    public string TrimUnsafeProperty
                    {
                        [RequiresUnreferencedCode("Not trim-safe.")]
                        get => "trim";
                    }

                    public int SafeMethod() => 1;

                    [RequiresDynamicCode("Not AOT-safe.")]
                    public int DynamicUnsafeMethod() => 2;

                    [RequiresUnreferencedCode("Not trim-safe.")]
                    public int TrimUnsafeMethod() => 3;
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("SafeProperty"));
        Assert.That(generated, Does.Contain("SafeMethod"));
        Assert.That(generated, Does.Not.Contain("TrimUnsafeProperty"));
        Assert.That(generated, Does.Not.Contain("DynamicUnsafeMethod"));
        Assert.That(generated, Does.Not.Contain("TrimUnsafeMethod"));
        Assert.That(generated, Does.Not.Contain("new global::TestTypes.MyModel(value)"));
    }

    [Test]
    public void UnsupportedMemberShapes_AreExcludedFromGeneratedDispatch()
    {
        var source = """
            using Alder.Aot;
            using System;

            namespace TestTypes
            {
                public unsafe class MyModel
                {
                    public int SafeField;
                    public int* PointerField;

                    public string SafeProperty => "safe";
                    public Span<int> RefLikeProperty => default;

                    public string this[int index] => "safe";
                    public Span<int> this[long index] => default;
                    public string this[Span<int> index] => "bad";

                    public string SafeMethod() => "safe";
                    public Span<int> RefLikeReturn() => default;
                    public int* PointerReturn() => default;
                    public delegate*<void> FunctionPointerReturn() => default;
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source, allowUnsafe: true);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("SafeField"));
        Assert.That(generated, Does.Contain("SafeProperty"));
        Assert.That(generated, Does.Contain("SafeMethod"));
        Assert.That(generated, Does.Contain("key is int"));
        Assert.That(generated, Does.Contain("[(int)key]"));
        Assert.That(generated, Does.Not.Contain("PointerField"));
        Assert.That(generated, Does.Not.Contain("RefLikeProperty"));
        Assert.That(generated, Does.Not.Contain("RefLikeReturn"));
        Assert.That(generated, Does.Not.Contain("PointerReturn"));
        Assert.That(generated, Does.Not.Contain("FunctionPointerReturn"));
        Assert.That(generated, Does.Not.Contain("Span<int> index"));
        Assert.That(generated, Does.Not.Contain("long)key"));
    }

    [Test]
    public void TrimAndAotUnsafeRegisteredType_ExcludesGeneratedDispatchMembers()
    {
        var source = """
            using Alder.Aot;
            using System.Diagnostics.CodeAnalysis;

            namespace TestTypes
            {
                [RequiresDynamicCode("Not AOT-safe.")]
                public class MyModel
                {
                    public MyModel() { }
                    public string SafeProperty => "safe";
                    public int SafeMethod() => 1;
                }

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
            }
            """;

        var (diagnostics, outputCompilation, generatedTrees) = GeneratorTestHelper.RunGenerator(source);
        var errors = GetCompilationErrors(outputCompilation);
        var generated = GetAllGeneratedSource(generatedTrees);

        Assert.That(errors, Is.Empty, $"Generated code has compilation errors:\n{string.Join("\n", errors)}");
        Assert.That(generated, Does.Contain("global::TestTypes.MyModel"));
        Assert.That(generated, Does.Not.Contain("typed.SafeProperty"));
        Assert.That(generated, Does.Not.Contain("SafeMethod"));
        Assert.That(generated, Does.Not.Contain("new global::TestTypes.MyModel"));
    }

    [Test]
    public void MultipleConstructors_GeneratesSwitchOnArgLength()
    {
        var source = """
            using Alder.Aot;

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

                [AlderRegistered(typeof(MyModel))]
                public partial class TestContext : AlderTypeContext { }
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
