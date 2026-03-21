namespace Alder.Test.Linq;

// Engine-only: Tests RegisterExtensionMethods() API - Alder-specific extension method registration system.
// Standard C# resolves extension methods via using directives and assembly references;
// Alder requires explicit registration for sandbox control.

[TestFixture]
public class ExtensionMethodTests
{
    [Test]
    public void CustomExtension_OnString_Works()
    {
        var engine = new AlderEngine();
        engine.RegisterExtensionMethods(typeof(TestExtensions));
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate<string>("text.Reverse()");

        Assert.That(result, Is.EqualTo("olleh"));
    }

    [Test]
    public void CustomExtension_OnInt_Works()
    {
        var engine = new AlderEngine();
        engine.RegisterExtensionMethods(typeof(TestExtensions));
        engine.SetVariable("num", 5);

        var result = engine.Evaluate<int>("num.Square()");

        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void CustomExtension_WithParameter_Works()
    {
        var engine = new AlderEngine();
        engine.RegisterExtensionMethods(typeof(TestExtensions));
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate<string>("text.Repeat(3)");

        Assert.That(result, Is.EqualTo("hellohellohello"));
    }

    [Test]
    public void CustomExtension_Generic_Works()
    {
        var engine = new AlderEngine();
        engine.RegisterExtensionMethods(typeof(TestExtensions));
        engine.SetVariable("items", new[] { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate<int>("items.Second()");

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void CustomExtension_OnList_Works()
    {
        var engine = new AlderEngine();
        engine.RegisterExtensionMethods(typeof(TestExtensions));
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate<int>("items.Product()");

        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void CustomExtension_ChainedWithLinq_Works()
    {
        var engine = new AlderEngine();
        engine.RegisterExtensionMethods(typeof(TestExtensions));
        engine.SetVariable("items", new[] { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate<int>("items.Where(x => x > 2).Product()");

        Assert.That(result, Is.EqualTo(60));
    }

    [Test]
    public void CustomExtension_MultipleTypes_Works()
    {
        var engine = new AlderEngine();
        engine.RegisterExtensionMethods(typeof(TestExtensions));
        engine.RegisterExtensionMethods(typeof(MoreExtensions));
        engine.SetVariable("text", "hello");

        var reversed = engine.Evaluate<string>("text.Reverse()");
        var upper = engine.Evaluate<string>("text.Shout()");

        Assert.That(reversed, Is.EqualTo("olleh"));
        Assert.That(upper, Is.EqualTo("HELLO!"));
    }

    [Test]
    public void BuiltInLinq_StillWorks()
    {
        var engine = new AlderEngine();
        engine.SetVariable("items", new[] { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate<List<int>>("items.Where(x => x > 2).Select(x => x * 2).ToList()");

        Assert.That(result, Is.EqualTo(new[] { 6, 8, 10 }));
    }

    [Test]
    public void CustomExtension_WithDefaultParameter_Works()
    {
        var engine = new AlderEngine();
        engine.RegisterExtensionMethods(typeof(TestExtensions));
        engine.SetVariable("num", 10);

        var result = engine.Evaluate<int>("num.AddWithDefault()");

        Assert.That(result, Is.EqualTo(11));
    }

    [Test]
    public void CustomExtension_OverridesDefaultParameter_Works()
    {
        var engine = new AlderEngine();
        engine.RegisterExtensionMethods(typeof(TestExtensions));
        engine.SetVariable("num", 10);

        var result = engine.Evaluate<int>("num.AddWithDefault(5)");

        Assert.That(result, Is.EqualTo(15));
    }
}

public static class TestExtensions
{
    public static string Reverse(this string s)
    {
        var chars = s.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    public static int Square(this int n) => n * n;

    public static string Repeat(this string s, int count) => string.Concat(Enumerable.Repeat(s, count));

    public static T Second<T>(this IEnumerable<T> source) => source.Skip(1).First();

    public static int Product(this IEnumerable<int> source) => source.Aggregate(1, (a, b) => a * b);

    public static int AddWithDefault(this int n, int add = 1) => n + add;
}

public static class MoreExtensions
{
    public static string Shout(this string s) => s.ToUpper() + "!";
}
