namespace CsEval.Diagnostics;

/// <summary>
/// Static catalog of all diagnostic descriptors. Each field pairs a <see cref="DiagnosticCode"/>
/// with its Roslyn-matching message template. All throw sites reference descriptors from this catalog.
/// </summary>
public static class DiagnosticDescriptors
{
    // ECMA-334 operator applicability

    /// <summary>CS0019: Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'</summary>
    public static readonly DiagnosticDescriptor BadBinaryOps =
        new(DiagnosticCode.CS0019, "Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'");

    /// <summary>CS0021: Cannot apply indexing with [] to an expression of type '{0}'</summary>
    public static readonly DiagnosticDescriptor BadIndexerAccess =
        new(DiagnosticCode.CS0021, "Cannot apply indexing with [] to an expression of type '{0}'");

    /// <summary>CS0023: Operator '{0}' cannot be applied to operand of type '{1}'</summary>
    public static readonly DiagnosticDescriptor BadUnaryOp =
        new(DiagnosticCode.CS0023, "Operator '{0}' cannot be applied to operand of type '{1}'");

    // ECMA-334 type conversion

    /// <summary>CS0029: Cannot implicitly convert type '{0}' to '{1}'</summary>
    public static readonly DiagnosticDescriptor NoImplicitConversion =
        new(DiagnosticCode.CS0029, "Cannot implicitly convert type '{0}' to '{1}'");

    /// <summary>CS0030: Cannot convert type '{0}' to '{1}'</summary>
    public static readonly DiagnosticDescriptor NoExplicitConversion =
        new(DiagnosticCode.CS0030, "Cannot convert type '{0}' to '{1}'");

    /// <summary>CS0037: Cannot convert null to '{0}' because it is a non-nullable value type</summary>
    public static readonly DiagnosticDescriptor NullToNonNullable =
        new(DiagnosticCode.CS0037, "Cannot convert null to '{0}' because it is a non-nullable value type");

    // ECMA-334 name resolution

    /// <summary>CS0103: The name '{0}' does not exist in the current context</summary>
    public static readonly DiagnosticDescriptor NameNotInContext =
        new(DiagnosticCode.CS0103, "The name '{0}' does not exist in the current context");

    /// <summary>CS0104: '{0}' is an ambiguous reference between '{1}' and '{2}'</summary>
    public static readonly DiagnosticDescriptor AmbiguousReference =
        new(DiagnosticCode.CS0104, "'{0}' is an ambiguous reference between '{1}' and '{2}'");

    /// <summary>CS0117: '{0}' does not contain a definition for '{1}'</summary>
    public static readonly DiagnosticDescriptor NoMemberOnType =
        new(DiagnosticCode.CS0117, "'{0}' does not contain a definition for '{1}'");

    /// <summary>CS0128: A local variable or function named '{0}' is already defined in this scope</summary>
    public static readonly DiagnosticDescriptor DuplicateLocalVariable =
        new(DiagnosticCode.CS0128, "A local variable or function named '{0}' is already defined in this scope");

    // ECMA-334 control flow

    /// <summary>CS0139: No enclosing loop out of which to break or continue</summary>
    public static readonly DiagnosticDescriptor BreakOrContinueOutsideLoop =
        new(DiagnosticCode.CS0139, "No enclosing loop out of which to break or continue");

    /// <summary>CS0156: A throw statement with no arguments is not allowed outside of a catch clause</summary>
    public static readonly DiagnosticDescriptor ThrowOutsideCatch =
        new(DiagnosticCode.CS0156, "A throw statement with no arguments is not allowed outside of a catch clause");

    /// <summary>CS0163: Control cannot fall through from one case label to another</summary>
    public static readonly DiagnosticDescriptor CaseFallThrough =
        new(DiagnosticCode.CS0163, "Control cannot fall through from one case label to another");

    // ECMA-334 assignment

    /// <summary>CS0191: A readonly field cannot be assigned to</summary>
    public static readonly DiagnosticDescriptor ReadonlyAssignment =
        new(DiagnosticCode.CS0191, "A readonly field cannot be assigned to");

    // ECMA-334 type/namespace resolution

    /// <summary>CS0246: The type or namespace name '{0}' could not be found (are you missing a using directive or an assembly reference?)</summary>
    public static readonly DiagnosticDescriptor TypeNotFound =
        new(DiagnosticCode.CS0246, "The type or namespace name '{0}' could not be found (are you missing a using directive or an assembly reference?)");

    /// <summary>CS0266: Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)</summary>
    public static readonly DiagnosticDescriptor ExplicitConversionExists =
        new(DiagnosticCode.CS0266, "Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)");

    // ECMA-334 null and implicit typing

    /// <summary>CS0815: Cannot assign null to an implicitly-typed variable</summary>
    public static readonly DiagnosticDescriptor NullToImplicitlyTyped =
        new(DiagnosticCode.CS0815, "Cannot assign null to an implicitly-typed variable");

    // ECMA-334 exception handling

    /// <summary>CS1017: Try statement already has an empty catch block</summary>
    public static readonly DiagnosticDescriptor GeneralCatchMustBeLast =
        new(DiagnosticCode.CS1017, "Try statement already has an empty catch block");

    // ECMA-334 member resolution

    /// <summary>CS1061: '{0}' does not contain a definition for '{1}'</summary>
    public static readonly DiagnosticDescriptor MemberNotFound =
        new(DiagnosticCode.CS1061, "'{0}' does not contain a definition for '{1}'");

    // ECMA-334 iteration

    /// <summary>CS1579: foreach statement cannot operate on variables of type '{0}' because '{0}' does not contain a public instance or extension definition for 'GetEnumerator'</summary>
    public static readonly DiagnosticDescriptor ForeachRequiresIEnumerable =
        new(DiagnosticCode.CS1579, "foreach statement cannot operate on variables of type '{0}' because '{0}' does not contain a public instance or extension definition for 'GetEnumerator'");

    // ECMA-334 constructor resolution

    /// <summary>CS1729: '{0}' does not contain a constructor that takes {1} arguments</summary>
    public static readonly DiagnosticDescriptor NoMatchingConstructor =
        new(DiagnosticCode.CS1729, "'{0}' does not contain a constructor that takes {1} arguments");
}
