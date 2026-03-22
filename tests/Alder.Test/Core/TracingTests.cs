using Alder.Test._Infrastructure;
using Alder.Tracing;

namespace Alder.Test.Core;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class TracingTests(CompilationMode mode)
{
    [Test]
    public void EvaluateWithTrace_ReturnsTreeStructure()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        engine.SetVariable("x", 5);

        var trace = engine.EvaluateWithTrace("x * 4 + 2");

        PrintTree(trace.Tree);

        Assert.That(trace.Result, Is.EqualTo(22));
        Assert.That(trace.Error, Is.Null);
        Assert.That(trace.Tree, Is.Not.Null);
        Assert.That(trace.Tree.NodeKind, Is.EqualTo("BinaryOperator"));
        Assert.That(trace.Tree.Description, Is.EqualTo("+"));
        Assert.That(trace.Tree.Value, Is.EqualTo(22));
        Assert.That(trace.Tree.Source, Is.EqualTo("x * 4 + 2"));

        Assert.That(trace.Tree.Children, Has.Count.EqualTo(2));

        var left = trace.Tree.Children[0];
        Assert.That(left.NodeKind, Is.EqualTo("BinaryOperator"));
        Assert.That(left.Description, Is.EqualTo("*"));
        Assert.That(left.Value, Is.EqualTo(20));

        var right = trace.Tree.Children[1];
        Assert.That(right.NodeKind, Is.EqualTo("Literal"));
        Assert.That(right.Value, Is.EqualTo(2));
    }

    [Test]
    public void EvaluateWithTrace_CapturesSourceSubstrings()
    {
        var engine = TestEngineFactory.Create(mode);

        var trace = engine.EvaluateWithTrace("1 + 2 * 3");

        PrintTree(trace.Tree);

        Assert.That(trace.Tree.Source, Is.EqualTo("1 + 2 * 3"));
        Assert.That(trace.Tree.Description, Is.EqualTo("+"));
        Assert.That(trace.Tree.Value, Is.EqualTo(7));

        Assert.That(trace.Tree.Children[0].NodeKind, Is.EqualTo("Literal"));
        Assert.That(trace.Tree.Children[0].Value, Is.EqualTo(1));

        var multiply = trace.Tree.Children[1];
        Assert.That(multiply.NodeKind, Is.EqualTo("BinaryOperator"));
        Assert.That(multiply.Description, Is.EqualTo("*"));
        Assert.That(multiply.Source, Is.EqualTo("2 * 3"));
    }

    [Test]
    public void EvaluateWithTrace_CapturesValueTypes()
    {
        var engine = TestEngineFactory.Create(mode);

        var trace = engine.EvaluateWithTrace("42");

        Assert.That(trace.Tree.Value, Is.EqualTo(42));
        Assert.That(trace.Tree.ValueType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void EvaluateWithTrace_CapturesErrorWithPartialTree()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 0);

        var trace = engine.EvaluateWithTrace("10 / x");

        Assert.That(trace.Error, Is.Not.Null);
        Assert.That(trace.Result, Is.Null);
        Assert.That(trace.Tree, Is.Not.Null);
        Assert.That(trace.Tree.ErrorCode, Is.Not.Null);
    }

    [Test]
    public void EvaluateWithTrace_IdentifierShowsVariableName()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("price", 100.0);

        var trace = engine.EvaluateWithTrace("price");

        Assert.That(trace.Tree.NodeKind, Is.EqualTo("Identifier"));
        Assert.That(trace.Tree.Description, Is.EqualTo("price"));
        Assert.That(trace.Tree.Value, Is.EqualTo(100.0));
    }

    [Test]
    public void EvaluateWithTrace_NestedExpression_ShowsFullTree()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        engine.SetVariable("price", 100.0);
        engine.SetVariable("discount", 0.15);
        engine.SetVariable("tax", 8.0);

        var trace = engine.EvaluateWithTrace("price * (1 - discount) + tax");

        PrintTree(trace.Tree);

        Assert.That(trace.Result, Is.EqualTo(93.0));
        Assert.That(trace.Tree.Children, Has.Count.EqualTo(2));
    }

    private static void PrintTree(TraceNode node, string prefix = "", bool isLast = true)
    {
        var connector = prefix.Length == 0 ? "" : isLast ? "└── " : "├── ";
        var value = node.ErrorCode != null
            ? $"ERROR: {node.ErrorCode} {node.ErrorMessage}"
            : node.Value?.ToString() ?? "null";
        TestContext.WriteLine($"""{prefix}{connector}{node.Description ?? node.NodeKind} "{node.Source}" = {value}""");
        var childPrefix = prefix + (prefix.Length == 0 ? "" : isLast ? "    " : "│   ");
        for (var i = 0; i < node.Children.Count; i++)
            PrintTree(node.Children[i], childPrefix, i == node.Children.Count - 1);
    }
}
