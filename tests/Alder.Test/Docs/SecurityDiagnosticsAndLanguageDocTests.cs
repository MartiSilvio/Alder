#if NET8_0_OR_GREATER
using Alder.Compiled;
#endif
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
[Parallelizable(ParallelScope.Children)]
public class SecurityDiagnosticsAndLanguageDocTests(CompilationMode mode)
{
    [Test]
    public void SecuritySafe_BlocksOrdinaryMethodCalls_ButRegisteredFunctionsRemainCallable()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Security = SecurityOptions.Safe();
            options.Functions.Register("doubleValue", args => (int)args[0]! * 2);
        });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(""" "hello".ToUpper() """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
        Assert.That(engine.Evaluate<int>("doubleValue(21)"), Is.EqualTo(42));
    }

    [Test]
    public void SecurityPolicyPresetOverrides_ConfigureOperationPolicy()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Security = SecurityOptions.Safe() with
            {
                AllowConstruction = true,
                AllowPropertySet = false,
                TrustedTypes = [typeof(System.Text.StringBuilder)]
            };
        });

        Assert.That(engine.Evaluate<object>("new StringBuilder()"), Is.InstanceOf<System.Text.StringBuilder>());
    }

    [Test]
    public void EmptySecurityOptions_IsMoreRestrictiveThanStrictPreset()
    {
        using var empty = TestEngineFactory.Create(mode, options =>
        {
            options.Security = new SecurityOptions();
        });
        empty.SetVariable<string>("text", "hello");

        using var strict = TestEngineFactory.Create(mode, options =>
        {
            options.Security = SecurityOptions.Strict();
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
            options.Security = SecurityOptions.Trusted() with { MaxCollectionSize = 10 };
        });

        Assert.That(engine.Evaluate<int>("new int[10].Length"), Is.EqualTo(10));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Enumerable.Range(1, 11).ToArray()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0202));
    }

    [Test]
    public void DiagnosticModel_ExposesFormattedCodeAndSourceLocation()
    {
        var diagnostic = new AlderDiagnostic(
            DiagnosticSeverity.Error,
            "missing name",
            DiagnosticCode.CS0103,
            Alder.Text.TextSpan.FromBounds(2, 7),
            Line: 3,
            Column: 5);

        Assert.That(diagnostic.FormattedCode, Is.EqualTo("CS0103"));
        Assert.That(diagnostic.Line, Is.EqualTo(3));
        Assert.That(diagnostic.Column, Is.EqualTo(5));
        Assert.That(diagnostic.Span.Start, Is.EqualTo(2));
        Assert.That(diagnostic.Span.End, Is.EqualTo(7));
    }

    [Test]
    public void TryParseAndTryValidate_ReturnStructuredDiagnosticsWithoutExecuting()
    {
        using var engine = TestEngineFactory.Create(mode);

        var parsed = engine.TryParse("1 +", out var expression, out var parseDiagnostics);
        var valid = engine.TryValidate("missing + 1", out var validateDiagnostics);

        Assert.That(parsed, Is.False);
        Assert.That(expression, Is.Null);
        Assert.That(parseDiagnostics, Is.Not.Empty);
        Assert.That(valid, Is.False);
        Assert.That(validateDiagnostics, Has.Some.Matches<AlderDiagnostic>(d => d.FormattedCode == "CS0103"));
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
    public void ExtendedRuleSurface_EvaluatesCompactRuleFragments()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Extended;
        });

        var orders = new[]
        {
            new { Status = "Open", Total = 600m },
            new { Status = "Open", Total = 75m },
            new { Status = "Closed", Total = 1200m }
        };

        var count = engine.Evaluate<int>(
            """let open = orders.Where(o => o.Status == "Open") in count(open)""",
            new { orders });
        var revenue = engine.Evaluate<decimal>(
            """let open = orders.Where(o => o.Status == "Open") in sum(open.Select(o => o.Total))""",
            new { orders });
        var large = engine.Evaluate<int>(
            """let open = orders.Where(o => o.Status == "Open") in count(open.Where(o => o.Total between 500m and 5000m))""",
            new { orders });

        Assert.That(count, Is.EqualTo(2));
        Assert.That(revenue, Is.EqualTo(675m));
        Assert.That(large, Is.EqualTo(1));
    }

    [Test]
    public void ExtendedOperators_EvaluateNumericComparisonAndBooleanForms()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Extended;
        });

        engine.SetVariable("score", 50);
        engine.SetVariable("expectedStatus", "Open");
        engine.SetVariable("status", "Open");
        engine.SetVariable("actualStatus", "Open");
        engine.SetVariable("isActive", true);

        Assert.That(engine.Evaluate<double>("2 ** 8"), Is.EqualTo(256d));
        Assert.That(engine.Evaluate<double>("""
            var scale = 3.0;
            scale **= 2;
            return scale;
            """), Is.EqualTo(9d));
        Assert.That(engine.Evaluate("1 === 1"), Is.True);
        Assert.That(engine.Evaluate("1 === 1L"), Is.False);
        Assert.That(engine.Evaluate("1 !== 1L"), Is.True);
        Assert.That(engine.Evaluate(""" "alpha" <=> "beta" """), Is.EqualTo(-1));
        Assert.That(engine.Evaluate("null <=> 5"), Is.EqualTo(-1));
        Assert.That(engine.Evaluate("0 <= score <= 100"), Is.True);
        Assert.That(engine.Evaluate("expectedStatus == status == actualStatus"), Is.True);
        Assert.That(engine.Evaluate("""isActive and score >= 50"""), Is.True);
        Assert.That(engine.Evaluate("""not false"""), Is.True);
    }

    [Test]
    public void ExtendedPredicates_EvaluateMembershipLikeRegexAndBetween()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Extended;
        });

        engine.SetVariable("allowedStatuses", new[] { "Open", "Pending" });
        engine.SetVariable("status", "Open");
        engine.SetVariable("region", "Active");
        engine.SetVariable("CustomerName", "Acme Northwest");
        engine.SetVariable("Code", "LIVE_001");
        engine.SetVariable("Email", "ada@example.com");
        engine.SetVariable("Sku", "PROD-1");
        engine.SetVariable("Total", 250m);

        Assert.That(engine.Evaluate("""status in allowedStatuses"""), Is.True);
        Assert.That(engine.Evaluate("""region not in new[] { "Blocked", "Retired" }"""), Is.True);
        Assert.That(engine.Evaluate("""CustomerName like "Acme%" """), Is.True);
        Assert.That(engine.Evaluate("""Code not like "TEMP_%" """), Is.True);
        Assert.That(engine.Evaluate("""Email =~ "^[^@]+@example\\.com$" """), Is.True);
        Assert.That(engine.Evaluate("""Sku !~ "^TEST-" """), Is.True);
        Assert.That(engine.Evaluate("Total between 100m and 500m"), Is.True);
    }

    [Test]
    public void ExtendedPipelines_InvokeLambdasAndRegisteredFunctions()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Extended;
            options.Functions.Register("normalize", args =>
                args[0]?.ToString()?.Trim().ToUpperInvariant());
        });

        Assert.That(engine.Evaluate("5 |> (x => x * 2)"), Is.EqualTo(10));
        Assert.That(engine.Evaluate(""" "  open " |> normalize """), Is.EqualTo("OPEN"));
    }

    [Test]
    public void ExtendedCollectionsRangesAndLocalSyntax_EvaluateDocumentedForms()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Extended;
        });

        engine.SetVariable("values", new[] { 10, 20, 30, 40, 50 });
        engine.SetVariable("first", new[] { 1, 2 });
        engine.SetVariable("second", new[] { 3, 4 });
        engine.SetVariable("customer", new { Name = "Ada" });
        engine.SetVariable("order", new { Name = "Ada", Total = 125m });
        engine.SetVariable("price", 100m);
        engine.SetVariable("minimum", 75m);
        engine.SetVariable("score", 95);

        Assert.That(engine.Evaluate<int>("(1..=5).Count()"), Is.EqualTo(5));
        Assert.That(engine.Evaluate<int>("(1..<5).Count()"), Is.EqualTo(4));
        Assert.That(engine.Evaluate<int[]>("values[1:4]"), Is.EqualTo(new[] { 20, 30, 40 }));
        Assert.That(engine.Evaluate<int[]>("values[::2]"), Is.EqualTo(new[] { 10, 30, 50 }));
        Assert.That(engine.Evaluate<string>(""" "alphabet"[2:6] """), Is.EqualTo("phab"));
        Assert.That(engine.Evaluate<int[]>("[x * x for x in 1..=10 if x % 2 == 0]"), Is.EqualTo(new[] { 4, 16, 36, 64, 100 }));
        Assert.That(engine.Evaluate<int[]>("[1, 2, 3]"), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(engine.Evaluate<int[]>("[..first, ..second, 5]"), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        var projection = engine.Evaluate<IReadOnlyDictionary<string, object?>>("""new { Name = customer.Name, Total = order.Total }""");
        Assert.That(projection, Is.Not.Null);
        Assert.That(projection!["Total"], Is.EqualTo(125m));
        Assert.That(engine.Evaluate<bool>("""
            let discounted = price * 0.9m in
            discounted >= minimum
            """), Is.True);
        Assert.That(engine.Evaluate<string>("""
            let { Name, Total } = order in
            Name + ": " + Total.ToString()
            """), Is.EqualTo("Ada: 125"));
        Assert.That(engine.Evaluate<string>("""if (score >= 90) "pass" else "review" """), Is.EqualTo("pass"));
        Assert.That(engine.Evaluate<int>("""
            var attempts = 0;
            until (attempts == 3)
                attempts++;
            return attempts;
            """), Is.EqualTo(3));
    }

    [Test]
    public void ExtendedBuiltIns_EvaluateMathAggregatesAndDateSugar()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Extended;
        });

        engine.SetVariable("score", 127);
        engine.SetVariable("amount", 12.346m);
        engine.SetVariable("values", new[] { 10, 20, 30 });

        Assert.That(engine.Evaluate<double>("sin(pi / 2)"), Is.EqualTo(1.0d).Within(1e-12));
        Assert.That(engine.Evaluate("clamp(score, 0, 100)"), Is.EqualTo(100));
        Assert.That(engine.Evaluate("round(amount, 2)"), Is.EqualTo(12.35m));
        Assert.That(engine.Evaluate("sum(values)"), Is.EqualTo(60));
        Assert.That(engine.Evaluate("avg(values)"), Is.EqualTo(20.0d));
        Assert.That(engine.Evaluate("count(values)"), Is.EqualTo(3));
        Assert.That(engine.Evaluate("min(values)"), Is.EqualTo(10));
        Assert.That(engine.Evaluate("max(values)"), Is.EqualTo(30));
        Assert.That(engine.Evaluate("30.days"), Is.EqualTo(TimeSpan.FromDays(30)));
        Assert.That(engine.Evaluate("2.hours + 30.minutes"), Is.EqualTo(TimeSpan.FromMinutes(150)));
        Assert.That(engine.Evaluate("today()"), Is.TypeOf<DateTime>());
        Assert.That(engine.Evaluate("now()"), Is.TypeOf<DateTime>());
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
    public void ExtendedMigrationPolicy_CanCombineSecurityAndExecutionLimits()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.LanguageMode = LanguageMode.Extended;
            options.Security = SecurityOptions.Safe();
            options.Constraints = new ExecutionConstraints
            {
                MaxStatements = 10_000,
                MaxLoopIterations = 1_000,
                MaxTimeout = TimeSpan.FromSeconds(2)
            };
        });

        Assert.That(engine.Evaluate<double>("2 ** 5"), Is.EqualTo(32d));
    }

#if NET8_0_OR_GREATER
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
#endif
}
