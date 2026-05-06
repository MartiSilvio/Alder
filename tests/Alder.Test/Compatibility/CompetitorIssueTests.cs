using Newtonsoft.Json.Linq;
using System.Dynamic;
using Alder.Test._Infrastructure;

namespace Alder.Test.Compatibility;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class CompatibilityRegressionTests(CompilationMode mode)
{
    [Test]
    public void GenericInstanceMethodCall_OnJObjectToken_CanMaterializeDateTime()
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

    [Test]
    public void LambdaCanProjectDictionaryStringIndexerValues()
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

    [Test]
    public void RuntimeConstructedGenericCollection_CanFlowThroughLinqLambda()
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

    [Test]
    public void MultiParameterIndexer_CanUseNestedIndexerArguments()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("context", new TwoKeyIndex());
        engine.SetVariable("source", new Dictionary<string, string> { ["Key"] = "A", ["i"] = "B" });

        var result = engine.Evaluate("""context[source["Key"], source["i"]]""");
        Assert.That(result, Is.EqualTo("A:B"));
    }

    [Test]
    public void ExpandoStringMember_CanFlowIntoRegisteredFunction()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Functions.Register("PathCombine", args => Path.Combine((string)args[0]!, (string)args[1]!)));

        dynamic globalSettings = new ExpandoObject();
        globalSettings.MyTestPath = "C:\\delme\\";
        engine.SetVariable("GlobalSettings", globalSettings);

        var result = engine.Evaluate("""PathCombine(GlobalSettings.MyTestPath, "test.txt")""");

        Assert.That(result, Is.EqualTo(Path.Combine("C:\\delme\\", "test.txt")));
    }

    private sealed class TwoKeyIndex
    {
        public string this[string a, string b] => $"{a}:{b}";
    }
}
