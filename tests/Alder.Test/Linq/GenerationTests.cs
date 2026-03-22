using Alder.Test._Infrastructure;

namespace Alder.Test.Linq;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class GenerationTests(CompilationMode mode)
{
    #region DefaultIfEmpty

    [Test]
    public void DefaultIfEmpty_NonEmpty_ReturnsOriginal()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.DefaultIfEmpty().ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void DefaultIfEmpty_Empty_ReturnsDefault()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.DefaultIfEmpty().ToList()");
        Assert.That(result, Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void DefaultIfEmpty_WithValue_Empty_ReturnsSpecifiedDefault()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.DefaultIfEmpty(42).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 42 }));
    }

    #endregion

    #region Append / Prepend

    [Test]
    public void Append_AddsToEnd()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Append(4).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Prepend_AddsToStart()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 2, 3, 4 });

        var result = engine.Evaluate("numbers.Prepend(1).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    #endregion

    #region Static LINQ Methods (Enumerable.Range, Repeat)

    [Test]
    public void Enumerable_Range()
    {
        var engine = new AlderEngine(o =>
        {
            if (mode == CompilationMode.Compiled) o.UseCompiler();
            o.Modules.Register("Enumerable", typeof(Enumerable));
        });

        var result = engine.Evaluate("Enumerable.Range(1, 5).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void Enumerable_Repeat()
    {
        var engine = new AlderEngine(o =>
        {
            if (mode == CompilationMode.Compiled) o.UseCompiler();
            o.Modules.Register("Enumerable", typeof(Enumerable));
        });

        var result = engine.Evaluate("Enumerable.Repeat(42, 3).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 42, 42, 42 }));
    }

    // Note: Enumerable.Empty<T>() requires generic method invocation which is not yet supported

    #endregion
}
