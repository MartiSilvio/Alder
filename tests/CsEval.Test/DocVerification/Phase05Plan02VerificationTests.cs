using CsEval.Diagnostics;
using CsEval.Parsing;

namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase05Plan02VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => TestEngineFactory.Create(mode);

    private CsEvalEngine Engine(CsEvalOptions options)
        => TestEngineFactory.Create(mode, options);

    // ═══════════════════════════════════════════════════════════════
    // DIAG-01: CsEvalParserException
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ParserException_SyntaxError_HasLocationInfo()
    {
        var engine = Engine();
        var ex = Assert.Throws<CsEvalParserException>(() => engine.Evaluate("x +"));
        Assert.That(ex, Is.Not.Null);
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-01: CsEvalLexerException
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void LexerException_UnterminatedString()
    {
        var engine = Engine();
        var ex = Assert.Throws<CsEvalLexerException>(() => engine.Evaluate("\"unterminated"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("Unterminated string"));
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-01: CsEvalDepthException
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void DepthException_MaxDepthProperty()
    {
        var engine = Engine(new CsEvalOptions { MaxExpressionDepth = 3 });
        // Ternary nesting creates deep expression trees
        var expr = "true ? (true ? (true ? (true ? 1 : 2) : 2) : 2) : 2";
        var ex = Assert.Throws<CsEvalDepthException>(() => engine.Evaluate(expr));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.MaxDepth, Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-01: CsEvalLanguageModeException
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void LanguageModeException_FeatureNameProperty()
    {
        var engine = Engine(); // Standard mode
        var ex = Assert.Throws<CsEvalLanguageModeException>(() => engine.Evaluate("2 ** 3"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.FeatureName, Is.EqualTo("**"));
        Assert.That(ex.FormattedCode, Is.EqualTo("CSEV0009"));
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-01: CsEvalExecutionLimitException
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ExecutionLimitException_MaxStatements_Properties()
    {
        var engine = Engine(new CsEvalOptions
        {
            Constraints = new ExecutionConstraints { MaxStatements = 10 }
        });
        var ex = Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ while (true) {} }"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
        Assert.That(ex.StatementsExecuted, Is.GreaterThanOrEqualTo(10));
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-01: CsEvalSandboxException
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SandboxException_MethodCallBlocked()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Strict()
        });
        engine.SetVariable("s", "hello");
        var ex = Assert.Throws<CsEvalSandboxException>(() => engine.Evaluate("s.ToUpper()"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.FormattedCode, Does.StartWith("CSEV"));
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-01: Catch order -- CsEvalSandboxException before CsEvalException
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void CatchOrder_SandboxException_CaughtBeforeBase()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Strict()
        });
        engine.SetVariable("s", "hello");

        var caughtSpecific = false;
        try
        {
            engine.Evaluate("s.ToUpper()");
        }
        catch (CsEvalSandboxException)
        {
            caughtSpecific = true;
        }
        catch (CsEvalException)
        {
            Assert.Fail("CsEvalSandboxException should be caught before CsEvalException");
        }

        Assert.That(caughtSpecific, Is.True);
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-01: TryValidate
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TryValidate_InvalidExpression_ReturnsFalseWithDiagnostics()
    {
        var engine = Engine();
        var result = engine.TryValidate("undeclaredVar", out var diagnostics);
        Assert.That(result, Is.False);
        Assert.That(diagnostics, Is.Not.Empty);
        var diag = diagnostics[0];
        Assert.That(diag.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(diag.Message, Is.Not.Null.And.Not.Empty);
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-01: TryEvaluate
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TryEvaluate_InvalidExpression_ReturnsFalse()
    {
        var engine = Engine();
        var result = engine.TryEvaluate("undeclaredVar", out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryEvaluate_ValidExpression_ReturnsTrue()
    {
        var engine = Engine();
        var result = engine.TryEvaluate("1 + 2", out var value);
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-01: TryParse
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TryParse_SyntaxError_ReturnsFalse()
    {
        var engine = Engine();
        var result = engine.TryParse("x +", out _);
        Assert.That(result, Is.False);
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-01: FormattedCode format
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void FormattedCode_CSFormat()
    {
        var engine = Engine();
        var ex = Assert.Catch<CsEvalException>(() => engine.Evaluate("undeclaredVar"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.FormattedCode, Does.Match(@"^CS\d{4}$"));
    }

    [Test]
    public void FormattedCode_CSEVFormat()
    {
        var engine = Engine(); // Standard mode
        var ex = Assert.Catch<CsEvalException>(() => engine.Evaluate("2 ** 3"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.FormattedCode, Does.Match(@"^CSEV\d{4}$"));
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-02: CS0103 (undeclared variable)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ErrorCode_CS0103_UndeclaredVariable()
    {
        var engine = Engine();
        var ex = Assert.Catch<CsEvalException>(() => engine.Evaluate("undeclaredVar"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0103"));
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-02: CS0019 (operator type mismatch)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ErrorCode_CS0019_OperatorTypeMismatch()
    {
        var engine = Engine();
        engine.SetVariable("s", "hello");
        var ex = Assert.Catch<CsEvalException>(() => engine.Evaluate("s - 1"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-02: Sandbox violation produces CSEV code
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ErrorCode_SandboxViolation_ProducesCSEVCode()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Strict()
        });
        engine.SetVariable("s", "hello");
        var ex = Assert.Throws<CsEvalSandboxException>(() => engine.Evaluate("s.ToUpper()"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.FormattedCode, Does.StartWith("CSEV"));
        var codeNum = (int)ex.ErrorCode!.Value;
        Assert.That(codeNum, Is.GreaterThanOrEqualTo(1_000_010).And.LessThanOrEqualTo(1_000_019));
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-02: Formatting logic
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void DiagnosticCode_Formatting_CSCodes()
    {
        Assert.That(DiagnosticCode.CS0103.ToDiagnosticId(), Is.EqualTo("CS0103"));
        Assert.That(DiagnosticCode.CS0019.ToDiagnosticId(), Is.EqualTo("CS0019"));
        Assert.That(DiagnosticCode.CS8124.ToDiagnosticId(), Is.EqualTo("CS8124"));
    }

    [Test]
    public void DiagnosticCode_Formatting_CSEVCodes()
    {
        Assert.That(DiagnosticCode.CSEV0001.ToDiagnosticId(), Is.EqualTo("CSEV0001"));
        Assert.That(DiagnosticCode.CSEV0010.ToDiagnosticId(), Is.EqualTo("CSEV0010"));
        Assert.That(DiagnosticCode.CSEV0049.ToDiagnosticId(), Is.EqualTo("CSEV0049"));
    }

    // ═══════════════════════════════════════════════════════════════
    // DIAG-02: Spot-check message templates
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void DiagnosticDescriptor_CS0103_MessageTemplate()
    {
        var formatted = DiagnosticDescriptors.NameNotInContext.FormatMessage("x");
        Assert.That(formatted, Is.EqualTo("The name 'x' does not exist in the current context"));
    }

    [Test]
    public void DiagnosticDescriptor_CSEV0011_MessageTemplate()
    {
        var formatted = DiagnosticDescriptors.SandboxMethodCallBlocked.FormatMessage("ToUpper");
        Assert.That(formatted, Is.EqualTo("Method calls blocked by sandbox: ToUpper"));
    }

    [Test]
    public void DiagnosticDescriptor_CS0029_MessageTemplate()
    {
        var formatted = DiagnosticDescriptors.NoImplicitConversion.FormatMessage("string", "int");
        Assert.That(formatted, Is.EqualTo("Cannot implicitly convert type 'string' to 'int'"));
    }
}
