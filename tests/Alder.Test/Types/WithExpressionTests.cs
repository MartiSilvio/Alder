using Alder.Test._Infrastructure;

namespace Alder.Test.Types;

/// <summary>
/// Tests for with expressions (§12.18 — record/struct non-destructive mutation).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class WithExpressionTests(CompilationMode mode)
{
    [Test]
    public void With_Record_ChangesProperty()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("p", new PersonRecord("Alice", 30));

        var result = engine.Evaluate("""(p with { Name = "Bob" }).Name""");
        Assert.That(result, Is.EqualTo("Bob"));
    }

    [Test]
    public void With_Record_PreservesUnchangedProperties()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("p", new PersonRecord("Alice", 30));

        var result = engine.Evaluate("""(p with { Name = "Bob" }).Age""");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void With_Record_OriginalUnchanged()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("p", new PersonRecord("Alice", 30));

        var result = engine.Evaluate("""
            {
                var q = p with { Name = "Bob" };
                return p.Name;
            }
            """);
        Assert.That(result, Is.EqualTo("Alice"));
    }

    [Test]
    public void With_Record_MultipleProperties()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("p", new PersonRecord("Alice", 30));

        var result = engine.Evaluate("""p with { Name = "Bob", Age = 25 }""");
        Assert.That(result, Is.EqualTo(new PersonRecord("Bob", 25)));
    }

    [Test]
    public void With_Struct_ChangesField()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("pt", new PointStruct { X = 1, Y = 2 });

        var result = engine.Evaluate("(pt with { X = 10 }).X");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void With_Struct_PreservesUnchangedField()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("pt", new PointStruct { X = 1, Y = 2 });

        var result = engine.Evaluate("(pt with { X = 10 }).Y");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void With_Chained()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("p", new PersonRecord("Alice", 30));

        var result = engine.Evaluate("""(p with { Name = "Bob" } with { Age = 25 }).Age""");
        Assert.That(result, Is.EqualTo(25));
    }

    public record PersonRecord(string Name, int Age);

    public struct PointStruct
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
