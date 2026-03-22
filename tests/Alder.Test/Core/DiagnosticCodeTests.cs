using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Test._Infrastructure;

namespace Alder.Test.Core;

/// <summary>
/// Verifies that every CS error code in the DiagnosticCode enum is correctly wired
/// through DiagnosticDescriptors to AlderException throw sites.
///
/// Each test triggers a specific error via the public AlderEngine API, then asserts:
///   1. ErrorCode matches the expected DiagnosticCode enum value
///   2. FormattedCode matches the "CS####" string format
///   3. Message contains formatted arguments (no raw {0} placeholders)
///
/// All errors are AlderException with DiagnosticCode. Exception subtypes removed —
/// error identity is the diagnostic code, not the exception type.
///
///   Alder-specific breakdown (no C# compiler equivalent):
///     Sandbox/security:    12 (assignment/method/property/index blocked)
///     Null access:         15 (runtime NullReferenceException in real C#)
///     Execution limits:     5 (statement/timeout constraints)
///     Reflection guards:    3 (reflection type leakage prevention)
///     Internal/unreachable: 12 (unknown operator, unsupported pattern)
///     Tuple/deconstruction: 7 (tuple size limits, deconstruction mismatch)
///     Spread operator:      4 (Alder extension syntax)
///     Other Alder-only:   11 (method invocation, compilation wrapper, etc.)
///     Parser errors:        15 (parsing-specific messages)
///     Lexer errors:         26 (deferred per 10-07 decision)
/// </summary>
[TestFixture]
public class DiagnosticCodeTests
{
    private AlderEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new AlderEngine();
    }

    // --- CS0019: Operator cannot be applied to operands ---

    [Test]
    public void CS0019_BadBinaryOps_StringMinusInt()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate(""" "hello" - 5 """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0019"));
        Assert.That(ex.Message, Does.Contain("-"));
        Assert.That(ex.Message, Does.Contain("String"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    [Test]
    public void CS0019_BadBinaryOps_LogicalAnd_IntInt()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("1 && 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0019"));
    }

    // --- CS0021: Cannot apply indexing ---

    [Test]
    public void CS0021_BadIndexerAccess_IntIndexed()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("42[0]"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0021));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0021"));
        Assert.That(ex.Message, Does.Contain("Int32"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    [Test]
    public void CS0021_BadIndexerAccess_MultiDimOnNonArray_AllCompilationModes()
    {
        const string expr = "{ var x = 42; return x[0,0]; }";
        foreach (var mode in new[] { CompilationMode.Interpreted, CompilationMode.Compiled })
        {
            var engine = TestEngineFactory.Create(mode);
            var ex = Assert.Throws<AlderException>(() => engine.Evaluate(expr));
            Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0021), $"Mode: {mode}");
            Assert.That(ex.FormattedCode, Is.EqualTo("CS0021"), $"Mode: {mode}");
        }
    }

    // --- CS0023: Operator cannot be applied to operand ---

    [Test]
    public void CS0023_BadUnaryOp_NegateString()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("""-"hello" """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0023));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0023"));
        Assert.That(ex.Message, Does.Contain("-"));
        Assert.That(ex.Message, Does.Contain("String"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS0029: Cannot implicitly convert type ---

    [Test]
    public void CS0029_NoImplicitConversion_IntToString()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("{ string s = 42; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0029));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0029"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS0030: Cannot convert type ---

    [Test]
    public void CS0030_NoExplicitConversion_IntToString()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("(string)42"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0030));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0030"));
        Assert.That(ex.Message, Does.Contain("Int32"));
        Assert.That(ex.Message, Does.Contain("String"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS0037: Cannot convert null to non-nullable value type ---

    [Test]
    public void CS0037_NullToNonNullable_CastNullToInt()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("(int)null"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0037));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0037"));
        Assert.That(ex.Message, Does.Contain("Int32"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS0103: The name does not exist in the current context ---

    [Test]
    public void CS0103_NameNotInContext_UndefinedVariable()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("undefinedVar"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0103"));
        Assert.That(ex.Message, Does.Contain("undefinedVar"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS0104: Ambiguous reference ---

    [Test]
    public void CS0104_AmbiguousReference_DirectConstruction()
    {
        // CS0104 requires specific namespace ambiguity setup that is difficult
        // to trigger through the public API. Verify via direct construction.
        var ex = new AlderException(DiagnosticDescriptors.AmbiguousReference,
            "Timer", "System.Threading.Timer", "System.Timers.Timer");
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0104));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0104"));
        Assert.That(ex.Message, Does.Contain("Timer"));
        Assert.That(ex.Message, Does.Contain("System.Threading.Timer"));
        Assert.That(ex.Message, Does.Contain("System.Timers.Timer"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS0117: Type does not contain a definition for member ---

    [Test]
    public void CS0117_NoMemberOnType_ModuleMemberNotFound()
    {
        // CS0117 fires for module member access and dict property access.
        // RegisterModule creates a module; accessing a non-existent member throws CS0117.
        var engine = new AlderEngine(o => o.Modules.Register<TestModule>("MyMod", instance: new TestModule()));
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("MyMod.NonExistentProp"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0117));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0117"));
        Assert.That(ex.Message, Does.Contain("TestModule"));
        Assert.That(ex.Message, Does.Contain("NonExistentProp"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    private class TestModule
    {
        public int Value => 42;
    }

    // --- CS0128: Duplicate local variable ---

    [Test]
    public void CS0128_DuplicateLocalVariable()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("{ int x = 1; int x = 2; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0128));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0128"));
        Assert.That(ex.Message, Does.Contain("x"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS0139: No enclosing loop out of which to break or continue ---

    [Test]
    public void CS0139_BreakOrContinueOutsideLoop()
    {
        // CS0139 is thrown by the IL compiler when break/continue appears outside a loop.
        // The TryCompile wrapper catches it and stores the reason as a string, so the
        // original diagnostic-aware exception is not directly observable via the public API.
        // Verify the descriptor wiring via direct construction.
        var ex = new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0139));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0139"));
        Assert.That(ex.Message, Does.Contain("No enclosing loop"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    [Test]
    public void CS0139_BreakOutsideLoop_AllCompilationModes()
    {
        foreach (var mode in new[] { CompilationMode.Interpreted, CompilationMode.Compiled })
        {
            var engine = TestEngineFactory.Create(mode);
            var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ break; }"));
            Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0139), $"Mode: {mode}");
            Assert.That(ex.FormattedCode, Is.EqualTo("CS0139"), $"Mode: {mode}");
        }
    }

    // --- CS0156: Throw statement with no arguments outside catch ---

    [Test]
    public void CS0156_ThrowOutsideCatch()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("{ throw; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0156));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0156"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    [Test]
    public void CS0155_ThrowOperandMustBeException_AllCompilationModes()
    {
        foreach (var mode in new[] { CompilationMode.Interpreted, CompilationMode.Compiled })
        {
            var engine = TestEngineFactory.Create(mode);
            var ex = Assert.Throws<AlderException>(() => engine.Evaluate("throw 42"));
            Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0155), $"Mode: {mode}");
            Assert.That(ex.FormattedCode, Is.EqualTo("CS0155"), $"Mode: {mode}");
        }
    }

    // --- CS0163: Control cannot fall through from one case label to another ---

    [Test]
    public void CS0163_CaseFallThrough()
    {
        // Switch case without break falls through to next case
        var expr = @"
        {
            var x = 1;
            switch (x)
            {
                case 1:
                    x = 10;
                case 2:
                    x = 20;
                    break;
            }
            return x;
        }";
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0163));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0163"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS0191: A readonly field cannot be assigned to ---

    [Test]
    public void CS0191_ReadonlyAssignment()
    {
        // DateTime.MaxValue is a readonly field -- assigning to a property on the
        // result of a member access triggers CS0191 when the property has no setter.
        _engine.SetVariable("dt", DateTime.Now);
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("dt.Ticks = 0"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0191));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0191"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS0131: The left-hand side of an assignment must be a variable, property or indexer ---

    [Test]
    public void CS0131_ConstLocalAssignment()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("{ const int x = 1; x = 2; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0131));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0131"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS0246: The type or namespace name could not be found ---

    [Test]
    public void CS0246_TypeNotFound_NewNonExistentType()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("new NonExistentType()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0246));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0246"));
        Assert.That(ex.Message, Does.Contain("NonExistentType"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS0266: Cannot implicitly convert; explicit conversion exists ---

    [Test]
    public void CS0266_ExplicitConversionExists_DirectConstruction()
    {
        // Verify descriptor wiring independent of runtime throw paths.
        var ex = new AlderException(DiagnosticDescriptors.ExplicitConversionExists, "long", "int");
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0266));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0266"));
        Assert.That(ex.Message, Does.Contain("long"));
        Assert.That(ex.Message, Does.Contain("int"));
        Assert.That(ex.Message, Does.Contain("missing a cast"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    [Test]
    public void CS0266_ExplicitConversionExists_AssignmentLongToInt()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("{ int x = 1L; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0266));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0266"));
    }

    [Test]
    public void CS0031_ConstantValueOutOfRange_AssignmentIntToByte()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("{ byte b = 256; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0031));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0031"));
    }

    [Test]
    public void CS1021_IntegerLiteralTooLarge()
    {
        var ex = Assert.Catch<AlderException>(() => _engine.Evaluate("9999999999999999999999999999999999999999999"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1021));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS1021"));
    }

    // --- CS0815: Cannot assign null to an implicitly-typed variable ---

    [Test]
    public void CS0815_NullToImplicitlyTyped()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("{ var x = null; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0815));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0815"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS1017: Try statement already has an empty catch block ---

    [Test]
    public void CS1017_GeneralCatchMustBeLast()
    {
        var expr = @"
        {
            try { }
            catch { }
            catch { }
        }";
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1017));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS1017"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS1061: Type does not contain a definition for member ---

    [Test]
    public void CS1061_MemberNotFound_DictMemberAccess()
    {
        // CS1061 fires in the Evaluator's GetMember for IDictionary member access.
        // Use Interpreted mode to ensure the Evaluator path is exercised.
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""{ var obj = new { Name = "test" }; return obj.NonExistent; } """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1061));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS1061"));
        Assert.That(ex.Message, Does.Contain("NonExistent"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS1579: foreach requires IEnumerable ---

    [Test]
    public void CS1579_ForeachRequiresIEnumerable()
    {
        var ex = Assert.Throws<AlderException>(() =>
            _engine.Evaluate("{ foreach (var x in 42) { } return 0; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1579));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS1579"));
        Assert.That(ex.Message, Does.Contain("Int32"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- CS1729: No matching constructor ---

    [Test]
    public void CS1729_NoMatchingConstructor_TooManyArgs()
    {
        var ex = Assert.Throws<AlderException>(() =>
            _engine.Evaluate("new DateTime(1, 2, 3, 4, 5, 6, 7, 8, 9)"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1729));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS1729"));
        Assert.That(ex.Message, Does.Contain("DateTime"));
        Assert.That(ex.Message, Does.Contain("9"));
        Assert.That(ex.Message, Does.Not.Contain("{0}"));
    }

    // --- Backward compatibility tests ---

    [Test]
    public void BackwardCompat_CatchAlderException_CatchesAllErrors()
    {
        // Proves that catch(AlderException) catches diagnostic-aware exceptions
        Assert.Throws<AlderException>(() => _engine.Evaluate("undefinedVar"));
    }

    [Test]
    public void BackwardCompat_AlderSpecificError_HasDiagnosticCode()
    {
        var engine = new AlderEngine(new AlderOptions
        {
            Sandbox = SandboxOptions.Strict() with { AllowAssignment = false }
        });
        engine.SetVariable("x", 10);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("x = 5"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
        Assert.That(ex.FormattedCode, Is.EqualTo("ALDR0101"));
    }

    [Test]
    public void ParserError_IsAlderException_WithCorrectCode()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("{ var x = null; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0815));
    }

    // --- Descriptor wiring completeness ---

    [Test]
    public void AllDiagnosticCodes_HaveDescriptors()
    {
        // Verify every DiagnosticCode enum value has a corresponding DiagnosticDescriptor
        var allCodes = Enum.GetValues<DiagnosticCode>();
        var descriptorFields = typeof(DiagnosticDescriptors)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
            .Select(f => ((DiagnosticDescriptor)f.GetValue(null)!).Code)
            .ToHashSet();

        foreach (var code in allCodes)
        {
            Assert.That(descriptorFields, Does.Contain(code),
                $"DiagnosticCode.{code} has no matching DiagnosticDescriptor field");
        }
    }

    [Test]
    public void AllDescriptors_HaveNonEmptyMessageTemplates()
    {
        var descriptorFields = typeof(DiagnosticDescriptors)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
            .Select(f => (DiagnosticDescriptor)f.GetValue(null)!);

        foreach (var descriptor in descriptorFields)
        {
            Assert.That(descriptor.MessageTemplate, Is.Not.Null.And.Not.Empty,
                $"DiagnosticDescriptor for {descriptor.Code} has empty message template");
        }
    }
}
