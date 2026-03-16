using Newtonsoft.Json.Linq;
using System.Dynamic;

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class MiscTests(CompilationMode mode)
{
    // Dynamic Expresso issue #327:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/327
    [Test]
    public void Issue327_GenericInstanceMethodCall_ToObjectDateTime_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var test = JObject.Parse("""
                                 {
                                   "date": "2024-01-02T03:04:05"
                                 }
                                 """);

        engine.SetVariable("test", test);
        var result = engine.Evaluate("test.SelectToken(\"$.date\").ToObject<DateTime>()");

        Assert.That(result, Is.TypeOf<DateTime>());
        Assert.That((DateTime)result!, Is.EqualTo(new DateTime(2024, 1, 2, 3, 4, 5)));
    }

    // Dynamic Expresso issue #366:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/366
    [Test]
    public void Issue366_NullConditionalWithLogicalAnd_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var dto = new { Object1 = (object?)null };
        engine.SetVariable("dto", dto);

        var result = engine.Evaluate("dto?.Object1 != null && dto.Object1 != null");
        Assert.That(result, Is.EqualTo(false));
    }

    // Dynamic Expresso issue #367:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/367
    [Test]
    public void Issue367_NullGuardWithLogicalAnd_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("myobj", null);

        var result = engine.Evaluate("myobj != null && myobj.Text == \"test\"");
        Assert.That(result, Is.EqualTo(false));
    }

    // Dynamic Expresso issue #363:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/363
    [Test]
    public void Issue363_DefaultLiteral_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("default");
        Assert.That(result, Is.Null);
    }

    // Dynamic Expresso issue #337:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/337
    [Test]
    public void Issue337_LambdaWithStringIndexerLiteral_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var source = new[]
        {
            new Dictionary<string, string> { ["id"] = "A" },
            new Dictionary<string, string> { ["id"] = "B" }
        };
        engine.SetVariable("source", source);

        var result = engine.Evaluate("source.Select(q => q[\"id\"]).ToList()");
        Assert.That(result, Is.InstanceOf<IList>());
        var list = (IList)result!;
        Assert.That(list.Count, Is.EqualTo(2));
        Assert.That(list[0], Is.EqualTo("A"));
        Assert.That(list[1], Is.EqualTo("B"));
    }

    // Dynamic Expresso issue (closed) #95:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/95
    [Test]
    public void Issue95_ExpandoCollectionProperty_WithLinq_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        dynamic root = new ExpandoObject();
        root.Items = new List<int> { 1, 2, 3, 4 };
        engine.SetVariable("root", root);

        var result = engine.Evaluate("root.Items.Where(x => x > 2).Count()");
        Assert.That(result, Is.EqualTo(2));
    }

    // Dynamic Expresso issue #328:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/328
    [Test]
    public void Issue328_DynamicBoolPropertyAccess_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        dynamic obj = new ExpandoObject();
        obj.IsEnabled = true;
        engine.SetVariable("obj", obj);

        var result = engine.Evaluate("obj.IsEnabled");
        Assert.That(result, Is.EqualTo(true));
    }

    // Dynamic Expresso issue #90:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/90
    [Test]
    public void Issue90_RuntimeConstructedGenericCollection_WithLambdaWhere_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var elementType = typeof(Dictionary<string, object?>);
        var dynamicListType = typeof(List<>).MakeGenericType(elementType);
        var dynamicList = (IList)Activator.CreateInstance(dynamicListType)!;

        dynamicList.Add(new Dictionary<string, object?> { ["Source"] = "Productions" });
        dynamicList.Add(new Dictionary<string, object?> { ["Source"] = "Test" });

        engine.SetVariable("dynamicList", dynamicList);

        var result = engine.Evaluate("dynamicList.Where(x => x[\"Source\"] == \"Productions\").Count()");
        Assert.That(result, Is.EqualTo(1));
    }

    // Dynamic Expresso issue #335:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/335
    [Test]
    public void Issue335_MultiParameterIndexerOnCustomType_NotSupportedYet()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("context", new TwoKeyIndex());
        engine.SetVariable("source", new Dictionary<string, string> { ["Key"] = "A", ["i"] = "B" });

        var ex = Assert.Catch(() =>
            engine.Evaluate("context[source[\"Key\"], source[\"i\"]]"));
        Assert.That(
            ex!.Message,
            Does.Contain("not supported")
                .Or.Contain("Unable to cast object of type")
                .Or.Contain("not in a correct format"));
    }

    // Dynamic Expresso issue (closed) #351:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/351
    [Test]
    public void Issue351_TryValidate_PropertyAccessOnRegisteredVariable_HasNoUnknownIdentifierDiagnostics()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("test", new { Name = "abc", Age = 1 });

        var success = engine.TryValidate("test.Name", out var diagnostics);

        Assert.That(success, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    // Dynamic Expresso issue (closed) #295:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/295
    [Test]
    public void Issue295_ExpandoStringMember_CanBePassedToStringFunctionWithoutExplicitCast()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.RegisterFunction("PathCombine", args => System.IO.Path.Combine((string)args[0]!, (string)args[1]!));

        dynamic globalSettings = new ExpandoObject();
        globalSettings.MyTestPath = "C:\\delme\\";
        engine.SetVariable("GlobalSettings", globalSettings);

        var result = engine.Evaluate("PathCombine(GlobalSettings.MyTestPath, \"test.txt\")");

        Assert.That(result, Is.EqualTo(System.IO.Path.Combine("C:\\delme\\", "test.txt")));
    }

    // Dynamic Expresso issue (closed) #285:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/285
    [Test]
    public void Issue285_UserDefinedImplicitConversion_InMethodBinding_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var consumer = new ImplicitConsumer();
        engine.SetVariable("consumer", consumer);
        engine.SetVariable("from", new ImplicitFrom(7));

        var result = engine.Evaluate("consumer.Accept(from)");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void UserDefinedImplicitConversion_MultipleArgs_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var consumer = new ImplicitConsumer();
        engine.SetVariable("consumer", consumer);
        engine.SetVariable("a", new ImplicitFrom(3));
        engine.SetVariable("b", new ImplicitFrom(5));

        var result = engine.Evaluate("consumer.AcceptSum(a, b)");
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void UserDefinedImplicitConversion_PreferExactMatchOverConversion()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var consumer = new ImplicitConsumer();
        engine.SetVariable("consumer", consumer);
        engine.SetVariable("to", new ImplicitTo(99));

        var result = engine.Evaluate("consumer.Accept(to)");
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void MultiTypeParamGenericInference_Zip_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.RegisterAssembly(typeof(Enumerable).Assembly);
        engine.RegisterNamespace("System.Linq");
        var a = new[] { 1, 2, 3 };
        var b = new[] { "a", "b", "c" };
        engine.SetVariable("a", a);
        engine.SetVariable("b", b);

        var result = engine.Evaluate("a.Zip(b, (x, y) => x + y)");
        Assert.That(result, Is.InstanceOf<IEnumerable<string>>());
        Assert.That(((IEnumerable<string>)result!).ToArray(), Is.EqualTo(new[] { "1a", "2b", "3c" }));
    }

    [Test]
    public void MultiTypeParamGenericInference_ToDictionary_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.RegisterAssembly(typeof(Enumerable).Assembly);
        engine.RegisterNamespace("System.Linq");
        var items = new[] { "apple", "banana", "cherry" };
        engine.SetVariable("items", items);

        var result = engine.Evaluate("items.ToDictionary(x => x, x => x.Length)");
        Assert.That(result, Is.InstanceOf<Dictionary<string, int>>());
        var dict = (Dictionary<string, int>)result!;
        Assert.That(dict["apple"], Is.EqualTo(5));
        Assert.That(dict["banana"], Is.EqualTo(6));
    }

    [Test]
    public void Sandbox_AllowConstruction_Blocks_New()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });

        Assert.Throws<CsEvalSandboxException>(() => engine.Evaluate("new object()"));
    }

    [Test]
    public void Sandbox_AllowConstruction_Permits_When_Enabled()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowConstruction = true }
        });

        var result = engine.Evaluate("new object()");
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Sandbox_AllowedTypes_Blocks_Unlisted_Type()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Trusted() with
            {
                AllowedTypes = new HashSet<Type> { typeof(Math) }
            }
        });
        engine.SetVariable("x", 5);

        var result = engine.Evaluate("Math.Abs(x)");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Sandbox_AllowedTypes_Blocks_Construction_Of_Unlisted_Type()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Trusted() with
            {
                AllowedTypes = new HashSet<Type> { typeof(Math) }
            }
        });

        Assert.Throws<CsEvalSandboxException>(() => engine.Evaluate("new object()"));
    }

    // NCalc issue #538:
    // https://github.com/ncalc/ncalc/issues/538
    [Test]
    public void NCalcIssue538_XorSupport_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("true ^ false");
        Assert.That(result, Is.EqualTo(true));
    }

    // NCalc issue #458:
    // https://github.com/ncalc/ncalc/issues/458
    [Test]
    public void NCalcIssue458_BackslashesInString_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("\"C:\\\\temp\\\\file.txt\"");
        Assert.That(result, Is.EqualTo("C:\\temp\\file.txt"));
    }

    // NCalc issue #439:
    // https://github.com/ncalc/ncalc/issues/439
    [Test]
    public void NCalcIssue439_NaNCondition_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("double.NaN == double.NaN ? 1 : 2");
        Assert.That(result, Is.EqualTo(2));
    }

    // NCalc issue #433:
    // https://github.com/ncalc/ncalc/issues/433
    [Test]
    public void NCalcIssue433_123DotE2_IsInvalidCSharp_HandledAsMemberAccessCurrently()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("123.E2");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Is.EqualTo("MethodRef"));
    }

    // NCalc issue #494:
    // https://github.com/ncalc/ncalc/issues/494
    [Test]
    public void NCalcIssue494_InvalidDateTimeAddition_DoesNotCrashRuntime()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        engine.SetVariable("a", new DateTime(2024, 1, 1));
        engine.SetVariable("b", new DateTime(2024, 1, 2));
        var result = engine.Evaluate("a + b");
        Assert.That(result, Is.InstanceOf<IDictionary<string, object?>>());
    }

    [Test]
    public void DateArithmeticSugar_NumericUnitsAndDateOps_Work()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        engine.SetVariable("date1", new DateTime(2026, 1, 31));
        engine.SetVariable("date2", new DateTime(2026, 1, 1));

        Assert.That(engine.Evaluate("30.days"), Is.EqualTo(TimeSpan.FromDays(30)));
        Assert.That(engine.Evaluate("2.hours + 30.minutes"), Is.EqualTo(TimeSpan.FromMinutes(150)));
        Assert.That(engine.Evaluate("date1 - date2"), Is.EqualTo(TimeSpan.FromDays(30)));
        Assert.That(engine.Evaluate("date2 + 30.days"), Is.EqualTo(new DateTime(2026, 1, 31)));
        Assert.That(engine.Evaluate("now()"), Is.TypeOf<DateTime>());
        Assert.That(engine.Evaluate("today()"), Is.TypeOf<DateTime>());
    }

    // Jace issue #92:
    // https://github.com/pieterderycke/Jace/issues/92
    [Test]
    public void JaceIssue92_InvalidScientificNotation_ThrowsParseStyleError()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch(() => engine.Evaluate("1e+"));
    }

    // Jace issue #93:
    // https://github.com/pieterderycke/Jace/issues/93
    [Test]
    public void JaceIssue93_EInScientificNotation_ParsesCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("1e-2");
        Assert.That(result, Is.EqualTo(0.01).Within(1e-12));
    }

    [Test]
    public void LockOnNull_ThrowsCsEvalExceptionWithConsistentMessage()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("{ object o = null; lock (o) { } }"));
        Assert.That(ex!.Message, Does.Contain("lock statement requires a non-null reference"));
    }

    [Test]
    public void UsingStatement_DisposesIAsyncDisposableInAllModes()
    {
        var probe = new AsyncDisposeProbe();
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("probe", probe);

        engine.Evaluate("{ using (probe) { } }");

        Assert.That(probe.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void CatchVariable_WithWhenFalse_IsOutOfScopeAfterTryCatch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("""
        {
            try { throw new Exception("boom"); }
            catch (Exception ex) when (false) { }
            catch (Exception) { }
            return ex;
        }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CS0103));
    }

    [Test]
    public void CatchFilter_WhenGuardThrows_IsTreatedAsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
        {
            var r = "";
            try { throw new Exception("boom"); }
            catch (Exception ex) when (1 / 0 == 0) { r = "bad"; }
            catch (Exception) { r = "ok"; }
            return r;
        }
        """);
        Assert.That(result, Is.EqualTo("ok"));
    }

    [Test]
    public void CatchFilter_WhenGuardThrows_CatchVariableRemainsOutOfScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("""
        {
            try { throw new Exception("boom"); }
            catch (Exception ex) when (1 / 0 == 0) { }
            catch (Exception) { }
            return ex;
        }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CS0103));
    }

    [Test]
    public void LogicalError_DoesNotEvaluateRightOperandForTypeDiagnostics()
    {
        var counter = new CounterProbe();
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("counter", counter);

        Assert.Throws<CsEvalException>(() => engine.Evaluate("1 && counter.Bump()"));
        Assert.That(counter.Count, Is.EqualTo(0));
    }

    [Test]
    public void SwitchExpression_WhenGuardThrows_DoesNotLeakPatternVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("""
        {
            try
            {
                var z = 1 switch { int n when (1/0 == 0) => n, _ => 0 };
            }
            catch { }
            return n;
        }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CS0103));
    }

    [Test]
    public void TopLevelComparison_IdentifierLessIdentifier_ParsesAsExpression()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("a", 1);
        engine.SetVariable("b", 2);
        Assert.That(engine.Evaluate("a < b"), Is.EqualTo(true));
    }

    [Test]
    public void ExtensionMethodLookup_RespectsCaseSensitivityOption()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            IsCaseSensitive = true
        });
        engine.RegisterExtensionMethods(typeof(CaseSensitiveExtensionProbe));

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("\"abc\".flipcasex()"));
        Assert.That(ex!.Message, Does.Contain("Method"));
    }

    [Test]
    public void ExtensionMethod_SupportsNamedArguments()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.RegisterExtensionMethods(typeof(CaseSensitiveExtensionProbe));

        var result = engine.Evaluate("1.ExtAdd(y: 2, z: 3)");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void ExtensionMethod_SignedPreferredOverUnsigned()
    {
        // ECMA-334 §12.6.4.7 rule 4: signed integral preferred over unsigned
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.RegisterExtensionMethods(typeof(CaseSensitiveExtensionProbe));

        var result = engine.Evaluate("1.ExtAmb((byte)1)");
        Assert.That(result, Is.EqualTo("short"));
    }

    private sealed class TwoKeyIndex
    {
        public string this[string a, string b] => $"{a}:{b}";
    }

    private sealed class AsyncDisposeProbe : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CounterProbe
    {
        public int Count { get; private set; }
        public bool Bump()
        {
            Count++;
            return true;
        }
    }

    public sealed class ImplicitFrom
    {
        public ImplicitFrom(int value) => Value = value;
        public int Value { get; }
        public static implicit operator ImplicitTo(ImplicitFrom input) => new(input.Value);
    }

    public sealed class ImplicitTo
    {
        public ImplicitTo(int value) => Value = value;
        public int Value { get; }
    }

    public sealed class ImplicitConsumer
    {
        public int Accept(ImplicitTo value) => value.Value;
        public int AcceptSum(ImplicitTo a, ImplicitTo b) => a.Value + b.Value;
    }
}

internal static class CaseSensitiveExtensionProbe
{
    public static string FlipCaseX(this string value) => value.ToUpperInvariant();
    public static int ExtAdd(this int value, int y, int z = 0) => value + y + z;
    public static string ExtAmb(this int value, short x) => "short";
    public static string ExtAmb(this int value, ushort x) => "ushort";
}
