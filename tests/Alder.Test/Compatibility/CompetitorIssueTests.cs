using Newtonsoft.Json.Linq;
using System.Dynamic;
using Alder.Test._Infrastructure;

namespace Alder.Test.Compatibility;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class CompetitorIssueTests(CompilationMode mode)
{
    // Dynamic Expresso issue #327:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/327
    [Test]
    public void Issue327_GenericInstanceMethodCall_ToObjectDateTime_Works()
    {
        var engine = TestEngineFactory.Create(mode);
        var test = JObject.Parse("""
                                 {
                                   "date": "2024-01-02T03:04:05"
                                 }
                                 """);

        engine.SetVariable("test", test);
        var result = engine.Evaluate("""test.SelectToken("$.date").ToObject<DateTime>()""");

        Assert.That(result, Is.TypeOf<DateTime>());
        Assert.That((DateTime)result!, Is.EqualTo(new DateTime(2024, 1, 2, 3, 4, 5)));
    }

    // Dynamic Expresso issue #366:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/366
    [Test]
    public void Issue366_NullConditionalWithLogicalAnd_Works()
    {
        var engine = TestEngineFactory.Create(mode);
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
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("myobj", null);

        var result = engine.Evaluate("""myobj != null && myobj.Text == "test" """);
        Assert.That(result, Is.EqualTo(false));
    }

    // Dynamic Expresso issue #363:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/363
    [Test]
    public void Issue363_DefaultLiteral_Works()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("default");
        Assert.That(result, Is.Null);
    }

    // Dynamic Expresso issue #337:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/337
    [Test]
    public void Issue337_LambdaWithStringIndexerLiteral_Works()
    {
        var engine = TestEngineFactory.Create(mode);
        var source = new[]
        {
            new Dictionary<string, string> { ["id"] = "A" },
            new Dictionary<string, string> { ["id"] = "B" }
        };
        engine.SetVariable("source", source);

        var result = engine.Evaluate("""source.Select(q => q["id"]).ToList()""");
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
        var engine = TestEngineFactory.Create(mode);
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
        var engine = TestEngineFactory.Create(mode);
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
        var engine = TestEngineFactory.Create(mode);

        var elementType = typeof(Dictionary<string, object?>);
        var dynamicListType = typeof(List<>).MakeGenericType(elementType);
        var dynamicList = (IList)Activator.CreateInstance(dynamicListType)!;

        dynamicList.Add(new Dictionary<string, object?> { ["Source"] = "Productions" });
        dynamicList.Add(new Dictionary<string, object?> { ["Source"] = "Test" });

        engine.SetVariable("dynamicList", dynamicList);

        var result = engine.Evaluate("""dynamicList.Where(x => x["Source"] == "Productions").Count()""");
        Assert.That(result, Is.EqualTo(1));
    }

    // Dynamic Expresso issue #335:
    // https://github.com/dynamicexpresso/DynamicExpresso/issues/335
    [Test]
    public void Issue335_MultiParameterIndexerOnCustomType_NotSupportedYet()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("context", new TwoKeyIndex());
        engine.SetVariable("source", new Dictionary<string, string> { ["Key"] = "A", ["i"] = "B" });

        var ex = Assert.Catch(() =>
            engine.Evaluate("""context[source["Key"], source["i"]]"""));
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
        var engine = TestEngineFactory.Create(mode);
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
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterFunction("PathCombine", args => System.IO.Path.Combine((string)args[0]!, (string)args[1]!));

        dynamic globalSettings = new ExpandoObject();
        globalSettings.MyTestPath = "C:\\delme\\";
        engine.SetVariable("GlobalSettings", globalSettings);

        var result = engine.Evaluate("""PathCombine(GlobalSettings.MyTestPath, "test.txt")""");

        Assert.That(result, Is.EqualTo(System.IO.Path.Combine("C:\\delme\\", "test.txt")));
    }

    // NCalc issue #538:
    // https://github.com/ncalc/ncalc/issues/538
    [Test]
    public void NCalcIssue538_XorSupport_Works()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("true ^ false");
        Assert.That(result, Is.EqualTo(true));
    }

    // NCalc issue #458:
    // https://github.com/ncalc/ncalc/issues/458
    [Test]
    public void NCalcIssue458_BackslashesInString_Works()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate(""" "C:\\temp\\file.txt" """);
        Assert.That(result, Is.EqualTo("C:\\temp\\file.txt"));
    }

    // NCalc issue #439:
    // https://github.com/ncalc/ncalc/issues/439
    [Test]
    public void NCalcIssue439_NaNCondition_Works()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("double.NaN == double.NaN ? 1 : 2");
        Assert.That(result, Is.EqualTo(2));
    }

    // NCalc issue #433:
    // https://github.com/ncalc/ncalc/issues/433
    [Test]
    public void NCalcIssue433_123DotE2_IsInvalidCSharp_HandledAsMemberAccessCurrently()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("123.E2");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Is.EqualTo("MethodRef"));
    }

    // Jace issue #92:
    // https://github.com/pieterderycke/Jace/issues/92
    [Test]
    public void JaceIssue92_InvalidScientificNotation_ThrowsParseStyleError()
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.Catch(() => engine.Evaluate("1e+"));
    }

    // Jace issue #93:
    // https://github.com/pieterderycke/Jace/issues/93
    [Test]
    public void JaceIssue93_EInScientificNotation_ParsesCorrectly()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("1e-2");
        Assert.That(result, Is.EqualTo(0.01).Within(1e-12));
    }

    private sealed class TwoKeyIndex
    {
        public string this[string a, string b] => $"{a}:{b}";
    }
}
