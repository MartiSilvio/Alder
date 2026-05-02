using Alder.Compiled;
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class SecurityDiagnosticsAndLanguageDocTests(CompilationMode mode)
{
    [Test]
    public void SandboxSafe_BlocksOrdinaryMethodCalls_ButRegisteredFunctionsRemainCallable()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Sandbox = SandboxOptions.Safe();
            options.Functions.Register("doubleValue", args => (int)args[0]! * 2);
        });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(""" "hello".ToUpper() """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
        Assert.That(engine.Evaluate<int>("doubleValue(21)"), Is.EqualTo(42));
    }

    [Test]
    public void EmptySandboxOptions_IsMoreRestrictiveThanStrictPreset()
    {
        using var empty = TestEngineFactory.Create(mode, options =>
        {
            options.Sandbox = new SandboxOptions();
        });
        empty.SetVariable<string>("text", "hello");

        using var strict = TestEngineFactory.Create(mode, options =>
        {
            options.Sandbox = SandboxOptions.Strict();
        });
        strict.SetVariable<string>("text", "hello");

        Assert.Throws<AlderException>(() => empty.Evaluate("text.Length"));
        Assert.That(strict.Evaluate<int>("text.Length"), Is.EqualTo(5));
    }

    [Test]
    public void ReflectionMetadataBoundary_AllowsTypeObjects_ButBlocksMemberInfo()
    {
        using var engine = TestEngineFactory.Create(mode);

        Assert.That(engine.Evaluate<string>("typeof(string).Name"), Is.EqualTo("String"));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("typeof(string).GetMethods()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void ExecutionConstraints_ReportLimitType()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Constraints = new ExecutionConstraints { MaxLoopIterations = 5 };
        });

        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("var x = 0; for (var i = 0; i < 100; i++) { x++; } return x;"));

        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
    }

    [Test]
    public void MaxCollectionSize_AppliesToCollectionProducingResults()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Sandbox = SandboxOptions.Trusted() with { MaxCollectionSize = 10 };
        });

        Assert.That(engine.Evaluate<int>("new int[10].Length"), Is.EqualTo(10));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Enumerable.Range(1, 11).ToArray()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0202));
    }

    [Test]
    public void TryValidate_ReturnsStructuredDiagnosticsWithoutExecuting()
    {
        using var engine = TestEngineFactory.Create(mode);

        var ok = engine.TryValidate("missing + 1", out var diagnostics);

        Assert.That(ok, Is.False);
        Assert.That(diagnostics, Has.Some.Matches<AlderDiagnostic>(d => d.FormattedCode == "CS0103"));
    }

    [Test]
    public void EvaluateWithTrace_CapturesValuesTypesSourceAndPartialErrors()
    {
        using var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("price", 100m);
        engine.SetVariable("discount", 0.15m);
        engine.SetVariable("tax", 8m);

        var trace = engine.EvaluateWithTrace("price * (1 - discount) + tax");

        Assert.That(trace.Error, Is.Null);
        Assert.That(trace.Tree.Source, Is.EqualTo("price * (1 - discount) + tax"));
        Assert.That(trace.Tree.Value, Is.EqualTo(93.00m));
        Assert.That(trace.Tree.ValueType, Is.EqualTo(typeof(decimal)));
        Assert.That(trace.Tree.Children, Has.Count.EqualTo(2));

        var discounted = trace.Tree.Children[0];
        var discount = discounted.Children[1];
        Assert.That(discount.Source, Is.EqualTo("1 - discount"));
        Assert.That(discount.Value, Is.EqualTo(0.85m));
        Assert.That(discount.ValueType, Is.EqualTo(typeof(decimal)));
        Assert.That(discount.Span.End, Is.GreaterThan(discount.Span.Start));

        engine.SetVariable("x", 0);
        var failing = engine.EvaluateWithTrace("10 / x");
        Assert.That(failing.Error, Is.Not.Null);
        Assert.That(failing.Tree.ErrorCode, Is.Not.Null);
        Assert.That(failing.Tree.Source, Is.EqualTo("10 / x"));
        Assert.That(failing.Tree.Children[1].Source, Is.EqualTo("x"));
        Assert.That(failing.Tree.Children[1].Value, Is.EqualTo(0));
    }

    [Test]
    public void StandardMode_LocalFunctions_AreSupportedWithinStatementInput()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Standard;
        });

        var result = engine.Evaluate<int>("""
            int Twice(int x) => x * 2;
            Twice(21)
            """);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void StandardMode_GenericLocalFunctions_AreOutsideCurrentSurface()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Standard;
        });

        Assert.Throws<AlderException>(() => engine.Evaluate("T Id<T>(T value) => value; Id(1)"));
    }

    [Test]
    public void ExtendedMode_AddsSyntaxAndBuiltins_ButStandardRejectsThem()
    {
        using var extended = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Extended;
        });

        Assert.That(extended.Evaluate<double>("2 ** 10"), Is.EqualTo(1024d));
        Assert.That(extended.Evaluate<bool>("Total between 100m and 500m", new { Total = 250m }), Is.True);
        Assert.That(extended.Evaluate("5 |> (x => x * 2)"), Is.EqualTo(10));

        using var standard = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Standard;
        });

        var ex = Assert.Throws<AlderException>(() => standard.Evaluate("2 ** 10"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedBuiltInPrecedence_MatchesDocumentedCollisionRules()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Extended;
            options.Functions.Register("sin", _ => 42);
        });

        engine.SetVariable("pi", 3);
        Assert.That(engine.Evaluate("pi"), Is.EqualTo(3));
        Assert.That(engine.Evaluate("sin(0.0)"), Is.EqualTo(42));

        using var moduleNamedSin = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Extended;
            options.Modules.Register("sin", typeof(Math));
        });

        Assert.That(moduleNamedSin.Evaluate("sin(0.0)"), Is.EqualTo(0.0).Within(1e-12));
    }

    [Test]
    public void ExpressionTreeExport_RejectsExtendedOnlySyntax_EvenWhenEngineIsExtended()
    {
        using var engine = new AlderEngine(options =>
        {
            options.LanguageMode = LanguageMode.Extended;
            options.UseCompiler();
        });

        Assert.Throws<AlderException>(() => engine.ParseAsExpression<Func<int, int>>("x => x ** 2"));
    }
}
