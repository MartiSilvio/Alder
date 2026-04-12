using Alder.Mcp;
using ModelContextProtocol.Protocol;

namespace Alder.Test.Integration;

[TestFixture]
public sealed class McpServerTests
{
    [Test]
    public void ParseArguments_WithoutArgs_UsesTrustedDefaults()
    {
        var parsed = McpServerConfig.ParseArguments([]);

        Assert.That(parsed.ShowHelp, Is.False);
        Assert.That(parsed.Config, Is.Not.Null);
        Assert.That(parsed.Config!.SandboxPreset, Is.EqualTo("trusted"));
        Assert.That(parsed.Config.Sandbox.AllowMethodCalls, Is.True);
        Assert.That(parsed.Config.Sandbox.AllowConstruction, Is.True);
        Assert.That(parsed.Config.MaxStatements, Is.EqualTo(1_000_000));
        Assert.That(parsed.Config.MaxLoopIterations, Is.EqualTo(1_000_000));
        Assert.That(parsed.Config.MaxTimeout, Is.EqualTo(TimeSpan.FromSeconds(10)));
    }

    [Test]
    public void ParseArguments_Help_ReturnsShowHelp()
    {
        var parsed = McpServerConfig.ParseArguments(["--help"]);

        Assert.That(parsed.ShowHelp, Is.True);
        Assert.That(parsed.Config, Is.Null);
    }

    [Test]
    public void ParseArguments_RejectsUnknownOption()
    {
        var ex = Assert.Throws<ArgumentException>(() => McpServerConfig.ParseArguments(["--sandbx", "safe"]));

        Assert.That(ex!.Message, Does.Contain("--sandbx"));
    }

    [Test]
    public void Validate_WithVariables_AllowsTheSamePreflightFlowAsEvaluate()
    {
        using var engine = new AlderEngine(o => o.Sandbox = SandboxOptions.Safe());

        var result = AlderTools.Validate(engine, "x + 1", """{"x": 41}""");

        Assert.That(result.IsError, Is.False);
        Assert.That(GetText(result), Is.EqualTo("Valid"));
    }

    [Test]
    public void Validate_WithoutVariables_ReportsMissingIdentifiers()
    {
        using var engine = new AlderEngine(o => o.Sandbox = SandboxOptions.Safe());

        var result = AlderTools.Validate(engine, "x + 1");

        Assert.That(result.IsError, Is.False);
        Assert.That(GetText(result), Does.Contain("CS0103"));
    }

    [Test]
    public void Evaluate_PreservesDecimalVariables()
    {
        using var engine = new AlderEngine(o => o.Sandbox = SandboxOptions.Safe());

        var result = AlderTools.Evaluate(engine, "price + tax", """{"price": 1.20, "tax": 0.30}""");

        Assert.That(result.IsError, Is.False);
        Assert.That(GetText(result), Does.Contain("(Decimal)"));
        Assert.That(GetText(result), Does.StartWith("1.50").Or.StartWith("1.5"));
    }

    [Test]
    public void GetInfo_ReportsConfiguredNamespacesAndDefaultImports()
    {
        var parsed = McpServerConfig.ParseArguments(["--namespace", "MyCompany.Domain"]);

        var result = AlderTools.GetInfo(parsed.Config!);
        var text = GetText(result);

        Assert.That(text, Does.Contain("Sandbox: trusted"));
        Assert.That(text, Does.Contain("System.Collections.Generic"));
        Assert.That(text, Does.Contain("System.Text.Json"));
        Assert.That(text, Does.Contain("MyCompany.Domain"));
    }

    private static string GetText(CallToolResult result)
    {
        return result.Content.OfType<TextContentBlock>().Single().Text;
    }
}
