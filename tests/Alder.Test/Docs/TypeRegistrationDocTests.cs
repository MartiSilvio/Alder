using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class TypeRegistrationDocTests(CompilationMode mode)
{
    [Test]
    public void AddNamespace()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Types.AddNamespace("System.Text.RegularExpressions");
        });

        var result = engine.Evaluate<bool>("""Regex.IsMatch("hello123", @"\d+")""");
        Assert.That(result, Is.True);
    }
}
