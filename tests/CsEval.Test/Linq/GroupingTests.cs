namespace CsEval.Test.Linq;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class GroupingTests(CompilationMode mode)
{
    [Test]
    public void GroupBy_GroupsByKey()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Category"] = "A", ["Value"] = 1 },
            new() { ["Category"] = "B", ["Value"] = 2 },
            new() { ["Category"] = "A", ["Value"] = 3 }
        });

        var result = engine.Evaluate("items.GroupBy(x => x.Category).ToList()");
        Assert.That(result, Is.InstanceOf<IList>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(2));

        var groupA = list.Cast<IGrouping<object?, object?>>().First(g => (string)g.Key! == "A");
        Assert.That(groupA.Count(), Is.EqualTo(2));
    }

    [Test]
    public void GroupBy_ReturnsIGrouping()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 6 });

        var result = engine.Evaluate("numbers.GroupBy(x => x > 3).ToList()");
        Assert.That(result, Is.InstanceOf<IList>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(2));

        foreach (var group in list)
        {
            Assert.That(group, Is.InstanceOf<IGrouping<bool, int>>());
            var g = (IGrouping<bool, int>)group!;
            Assert.That(g.Any(), Is.True);
        }
    }

    [Test]
    public void GroupBy_CanPassToFunctionAcceptingIGrouping()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("nums", new[] { 1, 2, 3, 4, 5 });

        // Register a function that accepts IGrouping<bool, int>
        engine.RegisterFunction("SumGroup", args =>
        {
            var group = (IGrouping<bool, int>)args[0]!;
            return group.Sum();
        });

        var result = engine.Evaluate("nums.GroupBy(x => x > 2).Select(g => SumGroup(g)).ToList()");
        var sums = ((IList)result!).Cast<object>().Select(x => (int)x).OrderBy(x => x).ToList();

        Assert.That(sums, Is.EqualTo(new[] { 3, 12 })); // 1+2=3, 3+4+5=12
    }

    #region Join

    [Test]
    public void Join_InnerJoin()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("people", new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Alice" },
            new() { ["Id"] = 2, ["Name"] = "Bob" }
        });
        engine.SetVariable("orders", new List<Dictionary<string, object?>>
        {
            new() { ["PersonId"] = 1, ["Product"] = "Apple" },
            new() { ["PersonId"] = 1, ["Product"] = "Banana" },
            new() { ["PersonId"] = 2, ["Product"] = "Orange" }
        });

        var result = engine.Evaluate(
            "people.Join(orders, p => p.Id, o => o.PersonId, (p, o) => p.Name + \": \" + o.Product).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Alice: Apple", "Alice: Banana", "Bob: Orange" }));
    }

    #endregion

    #region GroupJoin

    [Test]
    public void GroupJoin_GroupsMatchingElements()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("categories", new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Fruit" },
            new() { ["Id"] = 2, ["Name"] = "Vegetable" }
        });
        engine.SetVariable("products", new List<Dictionary<string, object?>>
        {
            new() { ["CategoryId"] = 1, ["Name"] = "Apple" },
            new() { ["CategoryId"] = 1, ["Name"] = "Banana" },
            new() { ["CategoryId"] = 2, ["Name"] = "Carrot" }
        });

        var result = engine.Evaluate(
            "categories.GroupJoin(products, c => c.Id, p => p.CategoryId, (c, ps) => c.Name + \": \" + ps.Count()).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Fruit: 2", "Vegetable: 1" }));
    }

    #endregion
}
