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

    /// <summary>CS0031: Constant value '{0}' cannot be converted to a '{1}'</summary>
    public static readonly DiagnosticDescriptor ConstantValueCannotConvert =
        new(DiagnosticCode.CS0031, "Constant value '{0}' cannot be converted to a '{1}'");

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

    /// <summary>CS0155: The type caught or thrown must be derived from System.Exception</summary>
    public static readonly DiagnosticDescriptor ThrowExpressionMustBeException =
        new(DiagnosticCode.CS0155, "The type caught or thrown must be derived from System.Exception");

    /// <summary>CS0156: A throw statement with no arguments is not allowed outside of a catch clause</summary>
    public static readonly DiagnosticDescriptor ThrowOutsideCatch =
        new(DiagnosticCode.CS0156, "A throw statement with no arguments is not allowed outside of a catch clause");

    /// <summary>CS0163: Control cannot fall through from one case label to another</summary>
    public static readonly DiagnosticDescriptor CaseFallThrough =
        new(DiagnosticCode.CS0163, "Control cannot fall through from one case label to another");

    // ECMA-334 assignment

    /// <summary>CS0131: The left-hand side of an assignment must be a variable, property or indexer</summary>
    public static readonly DiagnosticDescriptor AssignmentRequiresVariable =
        new(DiagnosticCode.CS0131, "The left-hand side of an assignment must be a variable, property or indexer");

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

    /// <summary>CS1021: Integral constant is too large</summary>
    public static readonly DiagnosticDescriptor IntegralConstantTooLarge =
        new(DiagnosticCode.CS1021, "Integral constant is too large");

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

    /// <summary>CS7036: There is no argument given that corresponds to the required parameter '{0}' of '{1}'</summary>
    public static readonly DiagnosticDescriptor MissingRequiredArgument =
        new(DiagnosticCode.CS7036, "There is no argument given that corresponds to the required parameter '{0}' of '{1}'");

    // CSEV01xx — Compilation and expression tree

    /// <summary>CSEV0100: Strict compilation mode could not compile the expression to IL: {0}</summary>
    public static readonly DiagnosticDescriptor StrictCompilationFailed =
        new(DiagnosticCode.CSEV0100, "Strict compilation mode could not compile the expression to IL: {0}");

    /// <summary>CSEV0101: Expression tree output does not support '{0}'.</summary>
    public static readonly DiagnosticDescriptor ExpressionTreeUnsupportedNode =
        new(DiagnosticCode.CSEV0101, "Expression tree output does not support '{0}'.");

    /// <summary>CSEV0102: Expression tree output does not support call shape '{0}'.</summary>
    public static readonly DiagnosticDescriptor ExpressionTreeUnsupportedCallShape =
        new(DiagnosticCode.CSEV0102, "Expression tree output does not support call shape '{0}'.");

    /// <summary>CSEV0103: ParseAsExpression requires a generic Func-style delegate type; got '{0}'.</summary>
    public static readonly DiagnosticDescriptor ParseAsExpressionRequiresGenericDelegate =
        new(DiagnosticCode.CSEV0103, "ParseAsExpression requires a generic Func-style delegate type; got '{0}'.");

    /// <summary>CSEV0104: ParseAsExpression requires a lambda expression input.</summary>
    public static readonly DiagnosticDescriptor ParseAsExpressionRequiresLambda =
        new(DiagnosticCode.CSEV0104, "Expression must be a lambda (e.g., '{0}').");

    /// <summary>CSEV0105: ParseAsExpression lambda parameter count mismatch.</summary>
    public static readonly DiagnosticDescriptor ParseAsExpressionParameterCountMismatch =
        new(DiagnosticCode.CSEV0105, "Expression has {0} parameter(s) but {1} expects {2}.");

    /// <summary>CSEV0106: ParseAsExpression return type conversion failed.</summary>
    public static readonly DiagnosticDescriptor ParseAsExpressionReturnTypeMismatch =
        new(DiagnosticCode.CSEV0106, "Cannot convert expression body type '{0}' to return type '{1}'.");

    /// <summary>CSEV0107: Expression binding failed: {0}</summary>
    public static readonly DiagnosticDescriptor BindingFailed =
        new(DiagnosticCode.CSEV0107, "{0}");

    /// <summary>CSEV0108: Cannot convert '{0}' to delegate type '{1}'.</summary>
    public static readonly DiagnosticDescriptor DelegateConversionFailed =
        new(DiagnosticCode.CSEV0108, "Cannot convert '{0}' to delegate type '{1}'.");

    // CSEV02xx — Language mode and parsing

    /// <summary>CSEV0200: Feature '{0}' is not available in Standard mode. Use LanguageMode.Extended to enable non-standard syntax extensions.</summary>
    public static readonly DiagnosticDescriptor ExtendedModeRequired =
        new(DiagnosticCode.CSEV0200, "Feature '{0}' is not available in Standard mode. Use LanguageMode.Extended to enable non-standard syntax extensions.");

    // CSEV03xx — Sandbox and security

    /// <summary>CSEV0300: {0} access blocked by sandbox: {1}.{2}</summary>
    public static readonly DiagnosticDescriptor SandboxAccessBlocked =
        new(DiagnosticCode.CSEV0300, "{0} access blocked by sandbox: {1}.{2}");

    /// <summary>CSEV0301: Method calls blocked by sandbox: {0}</summary>
    public static readonly DiagnosticDescriptor SandboxMethodCallBlocked =
        new(DiagnosticCode.CSEV0301, "Method calls blocked by sandbox: {0}");

    /// <summary>CSEV0302: Assignment blocked by sandbox: {0}</summary>
    public static readonly DiagnosticDescriptor SandboxAssignmentBlocked =
        new(DiagnosticCode.CSEV0302, "Assignment blocked by sandbox: {0}");

    /// <summary>CSEV0303: Index assignment blocked by sandbox: [{0}] = ...</summary>
    public static readonly DiagnosticDescriptor SandboxIndexAssignmentBlocked =
        new(DiagnosticCode.CSEV0303, "Index assignment blocked by sandbox: [{0}] = ...");

    /// <summary>CSEV0304: Property access blocked by sandbox: {0}</summary>
    public static readonly DiagnosticDescriptor SandboxPropertyAccessBlocked =
        new(DiagnosticCode.CSEV0304, "Property access blocked by sandbox: {0}");

    /// <summary>CSEV0305: Static field access blocked by sandbox: {0}.{1}</summary>
    public static readonly DiagnosticDescriptor SandboxStaticFieldAccessBlocked =
        new(DiagnosticCode.CSEV0305, "Static field access blocked by sandbox: {0}.{1}");

    /// <summary>CSEV0306: Static property access blocked by sandbox: {0}.{1}</summary>
    public static readonly DiagnosticDescriptor SandboxStaticPropertyAccessBlocked =
        new(DiagnosticCode.CSEV0306, "Static property access blocked by sandbox: {0}.{1}");

    /// <summary>CSEV0307: Property assignment blocked by sandbox: {0}</summary>
    public static readonly DiagnosticDescriptor SandboxPropertyAssignmentBlocked =
        new(DiagnosticCode.CSEV0307, "Property assignment blocked by sandbox: {0}");

    /// <summary>CSEV0308: Object construction blocked by sandbox: new {0}()</summary>
    public static readonly DiagnosticDescriptor SandboxConstructionBlocked =
        new(DiagnosticCode.CSEV0308, "Object construction blocked by sandbox: new {0}()");

    /// <summary>CSEV0309: Type '{0}' is not in the sandbox allowlist</summary>
    public static readonly DiagnosticDescriptor SandboxTypeBlocked =
        new(DiagnosticCode.CSEV0309, "Type '{0}' is not in the sandbox allowlist");

    // CSEV05xx — Null access and null safety

    /// <summary>CSEV0500: Cannot access {0} '{1}' on null</summary>
    public static readonly DiagnosticDescriptor NullMemberAccess =
        new(DiagnosticCode.CSEV0500, "Cannot access {0} '{1}' on null");

    /// <summary>CSEV0501: Cannot call method '{0}' on null</summary>
    public static readonly DiagnosticDescriptor NullMethodCall =
        new(DiagnosticCode.CSEV0501, "Cannot call method '{0}' on null");

    /// <summary>CSEV0502: Cannot call null as a function</summary>
    public static readonly DiagnosticDescriptor NullInvocation =
        new(DiagnosticCode.CSEV0502, "Cannot call null as a function");

    /// <summary>CSEV0503: Cannot assign to property '{0}' on null</summary>
    public static readonly DiagnosticDescriptor NullPropertyAssignment =
        new(DiagnosticCode.CSEV0503, "Cannot assign to property '{0}' on null");

    // CSEV06xx — Method resolution and invocation

    /// <summary>CSEV0600: Cannot call '{0}' as a function</summary>
    public static readonly DiagnosticDescriptor NonCallableType =
        new(DiagnosticCode.CSEV0600, "Cannot call '{0}' as a function");

    /// <summary>CSEV0601: Method '{0}' invocation failed</summary>
    public static readonly DiagnosticDescriptor MethodInvocationFailed =
        new(DiagnosticCode.CSEV0601, "Method '{0}' invocation failed");

    /// <summary>CSEV0602: Ambiguous method invocation: '{0}'</summary>
    public static readonly DiagnosticDescriptor AmbiguousMethodInvocation =
        new(DiagnosticCode.CSEV0602, "Ambiguous method invocation: '{0}'");

    /// <summary>CSEV0603: Call '{0}' requires runtime overload resolution</summary>
    public static readonly DiagnosticDescriptor RuntimeOverloadResolutionRequired =
        new(DiagnosticCode.CSEV0603, "Call '{0}' requires runtime overload resolution");

    /// <summary>CSEV0604: No applicable overload found for method '{0}'</summary>
    public static readonly DiagnosticDescriptor NoApplicableOverload =
        new(DiagnosticCode.CSEV0604, "No applicable overload found for method '{0}'");

    // CSEV07xx — Member access and property resolution

    /// <summary>CSEV0700: Indexer overloads with multiple parameters are not supported yet on type '{0}'.</summary>
    public static readonly DiagnosticDescriptor MultiParameterIndexerNotSupported =
        new(DiagnosticCode.CSEV0700, "Indexer overloads with multiple parameters are not supported yet on type '{0}'.");

    /// <summary>CSEV0701: Unsupported member type '{0}'</summary>
    public static readonly DiagnosticDescriptor UnsupportedMemberType =
        new(DiagnosticCode.CSEV0701, "Unsupported member type '{0}'");

    /// <summary>CSEV0702: Indexer access failed: {0}</summary>
    public static readonly DiagnosticDescriptor IndexerAccessFailed =
        new(DiagnosticCode.CSEV0702, "Indexer access failed: {0}");

    /// <summary>CSEV0703: No indexer found on type '{0}'</summary>
    public static readonly DiagnosticDescriptor NoIndexerOnType =
        new(DiagnosticCode.CSEV0703, "No indexer found on type '{0}'");

    // CSEV08xx — Type system and conversion

    /// <summary>CSEV0800: Cannot take the sizeof of type '{0}'</summary>
    public static readonly DiagnosticDescriptor SizeofUnsupportedType =
        new(DiagnosticCode.CSEV0800, "Cannot take the sizeof of type '{0}'");

    /// <summary>CSEV0801: Access to reflection types is not allowed: {0} ({1})</summary>
    public static readonly DiagnosticDescriptor ReflectionTypeAccessBlocked =
        new(DiagnosticCode.CSEV0801, "Access to reflection types is not allowed: {0} ({1})");

    /// <summary>CSEV0802: Cannot deconstruct type '{0}'</summary>
    public static readonly DiagnosticDescriptor DeconstructionFailed =
        new(DiagnosticCode.CSEV0802, "Cannot deconstruct type '{0}'");

    /// <summary>CSEV0803: Tuples must have at least 2 elements</summary>
    public static readonly DiagnosticDescriptor TupleTooFewElements =
        new(DiagnosticCode.CSEV0803, "Tuples must have at least 2 elements");

    /// <summary>CSEV0804: Deconstruction requires {0} values but tuple has {1} elements</summary>
    public static readonly DiagnosticDescriptor DeconstructionCountMismatch =
        new(DiagnosticCode.CSEV0804, "Deconstruction requires {0} values but tuple has {1} elements");

    /// <summary>CSEV0805: Type '{0}' does not have an 'Add' method for collection initializer</summary>
    public static readonly DiagnosticDescriptor CollectionInitializerNoAdd =
        new(DiagnosticCode.CSEV0805, "Type '{0}' does not have an 'Add' method for collection initializer");

    // CSEV09xx — Control flow and semantics

    /// <summary>CSEV0900: Semantic validation failed: {0}</summary>
    public static readonly DiagnosticDescriptor SemanticValidationFailed =
        new(DiagnosticCode.CSEV0900, "Semantic validation failed: {0}");

    /// <summary>CSEV0901: Expression nesting depth exceeded available stack space.</summary>
    public static readonly DiagnosticDescriptor ExpressionNestingDepthExceeded =
        new(DiagnosticCode.CSEV0901, "Expression nesting depth exceeded available stack space.");

    /// <summary>CSEV0902: Cannot slice null</summary>
    public static readonly DiagnosticDescriptor SliceNull =
        new(DiagnosticCode.CSEV0902, "Cannot slice null");

    /// <summary>CSEV0903: Slice step cannot be zero</summary>
    public static readonly DiagnosticDescriptor SliceStepZero =
        new(DiagnosticCode.CSEV0903, "Slice step cannot be zero");

    /// <summary>CSEV0904: Cannot slice type '{0}'</summary>
    public static readonly DiagnosticDescriptor SliceUnsupportedType =
        new(DiagnosticCode.CSEV0904, "Cannot slice type '{0}'");

    /// <summary>CSEV0905: Unknown compound assignment operator '{0}'</summary>
    public static readonly DiagnosticDescriptor UnknownCompoundAssignmentOperator =
        new(DiagnosticCode.CSEV0905, "Unknown compound assignment operator '{0}'");

    /// <summary>CSEV0906: Unsupported compound assignment base operator '{0}'</summary>
    public static readonly DiagnosticDescriptor UnsupportedCompoundBaseOperator =
        new(DiagnosticCode.CSEV0906, "Unsupported compound assignment base operator '{0}'");

    /// <summary>CSEV0907: Unsupported chained comparison operator: {0}</summary>
    public static readonly DiagnosticDescriptor UnsupportedChainedComparisonOperator =
        new(DiagnosticCode.CSEV0907, "Unsupported chained comparison operator: {0}");

    /// <summary>CSEV0908: Spread operator can only be used in array or object literals</summary>
    public static readonly DiagnosticDescriptor SpreadOutsideLiteral =
        new(DiagnosticCode.CSEV0908, "Spread operator can only be used in array or object literals");

    /// <summary>CSEV0909: lock statement requires a non-null reference</summary>
    public static readonly DiagnosticDescriptor LockRequiresNonNull =
        new(DiagnosticCode.CSEV0909, "lock statement requires a non-null reference");

    /// <summary>CSEV0910: goto case/default target not found</summary>
    public static readonly DiagnosticDescriptor GotoCaseTargetNotFound =
        new(DiagnosticCode.CSEV0910, "goto case/default target not found");

    /// <summary>CSEV0911: Pattern type '{0}' not yet implemented</summary>
    public static readonly DiagnosticDescriptor PatternNotImplemented =
        new(DiagnosticCode.CSEV0911, "Pattern type '{0}' not yet implemented");

    /// <summary>CSEV0912: Invalid out argument index '{0}'.</summary>
    public static readonly DiagnosticDescriptor InvalidOutArgumentIndex =
        new(DiagnosticCode.CSEV0912, "Invalid out argument index '{0}'.");

    /// <summary>CSEV0913: Unknown relational pattern operator '{0}'</summary>
    public static readonly DiagnosticDescriptor UnknownRelationalPatternOperator =
        new(DiagnosticCode.CSEV0913, "Unknown relational pattern operator '{0}'");
}
