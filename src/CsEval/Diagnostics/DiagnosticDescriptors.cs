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

    /// <summary>CSEV0001: Strict compilation mode could not compile the expression to IL: {0}</summary>
    public static readonly DiagnosticDescriptor StrictCompilationFailed =
        new(DiagnosticCode.CSEV0001, "Strict compilation mode could not compile the expression to IL: {0}");

    /// <summary>CSEV0002: Expression tree output does not support '{0}'.</summary>
    public static readonly DiagnosticDescriptor ExpressionTreeUnsupportedNode =
        new(DiagnosticCode.CSEV0002, "Expression tree output does not support '{0}'.");

    /// <summary>CSEV0003: Expression tree output does not support call shape '{0}'.</summary>
    public static readonly DiagnosticDescriptor ExpressionTreeUnsupportedCallShape =
        new(DiagnosticCode.CSEV0003, "Expression tree output does not support call shape '{0}'.");

    /// <summary>CSEV0004: ParseAsExpression requires a generic Func-style delegate type; got '{0}'.</summary>
    public static readonly DiagnosticDescriptor ParseAsExpressionRequiresGenericDelegate =
        new(DiagnosticCode.CSEV0004, "ParseAsExpression requires a generic Func-style delegate type; got '{0}'.");

    /// <summary>CSEV0005: ParseAsExpression requires a lambda expression input.</summary>
    public static readonly DiagnosticDescriptor ParseAsExpressionRequiresLambda =
        new(DiagnosticCode.CSEV0005, "Expression must be a lambda (e.g., '{0}').");

    /// <summary>CSEV0006: ParseAsExpression lambda parameter count mismatch.</summary>
    public static readonly DiagnosticDescriptor ParseAsExpressionParameterCountMismatch =
        new(DiagnosticCode.CSEV0006, "Expression has {0} parameter(s) but {1} expects {2}.");

    /// <summary>CSEV0007: ParseAsExpression return type conversion failed.</summary>
    public static readonly DiagnosticDescriptor ParseAsExpressionReturnTypeMismatch =
        new(DiagnosticCode.CSEV0007, "Cannot convert expression body type '{0}' to return type '{1}'.");

    /// <summary>CSEV0008: Expression binding failed: {0}</summary>
    public static readonly DiagnosticDescriptor BindingFailed =
        new(DiagnosticCode.CSEV0008, "{0}");

    /// <summary>CS0123: Cannot convert '{0}' to delegate type '{1}'.</summary>
    public static readonly DiagnosticDescriptor DelegateConversionFailed =
        new(DiagnosticCode.CS0123, "Cannot convert '{0}' to delegate type '{1}'.");

    // CSEV02xx — Language mode and parsing

    /// <summary>CSEV0009: Feature '{0}' is not available in Standard mode. Use LanguageMode.Extended to enable non-standard syntax extensions.</summary>
    public static readonly DiagnosticDescriptor ExtendedModeRequired =
        new(DiagnosticCode.CSEV0009, "Feature '{0}' is not available in Standard mode. Use LanguageMode.Extended to enable non-standard syntax extensions.");

    // CSEV03xx — Sandbox and security

    /// <summary>CSEV0010: {0} access blocked by sandbox: {1}.{2}</summary>
    public static readonly DiagnosticDescriptor SandboxAccessBlocked =
        new(DiagnosticCode.CSEV0010, "{0} access blocked by sandbox: {1}.{2}");

    /// <summary>CSEV0011: Method calls blocked by sandbox: {0}</summary>
    public static readonly DiagnosticDescriptor SandboxMethodCallBlocked =
        new(DiagnosticCode.CSEV0011, "Method calls blocked by sandbox: {0}");

    /// <summary>CSEV0012: Assignment blocked by sandbox: {0}</summary>
    public static readonly DiagnosticDescriptor SandboxAssignmentBlocked =
        new(DiagnosticCode.CSEV0012, "Assignment blocked by sandbox: {0}");

    /// <summary>CSEV0013: Index assignment blocked by sandbox: [{0}] = ...</summary>
    public static readonly DiagnosticDescriptor SandboxIndexAssignmentBlocked =
        new(DiagnosticCode.CSEV0013, "Index assignment blocked by sandbox: [{0}] = ...");

    /// <summary>CSEV0014: Property access blocked by sandbox: {0}</summary>
    public static readonly DiagnosticDescriptor SandboxPropertyAccessBlocked =
        new(DiagnosticCode.CSEV0014, "Property access blocked by sandbox: {0}");

    /// <summary>CSEV0015: Static field access blocked by sandbox: {0}.{1}</summary>
    public static readonly DiagnosticDescriptor SandboxStaticFieldAccessBlocked =
        new(DiagnosticCode.CSEV0015, "Static field access blocked by sandbox: {0}.{1}");

    /// <summary>CSEV0016: Static property access blocked by sandbox: {0}.{1}</summary>
    public static readonly DiagnosticDescriptor SandboxStaticPropertyAccessBlocked =
        new(DiagnosticCode.CSEV0016, "Static property access blocked by sandbox: {0}.{1}");

    /// <summary>CSEV0017: Property assignment blocked by sandbox: {0}</summary>
    public static readonly DiagnosticDescriptor SandboxPropertyAssignmentBlocked =
        new(DiagnosticCode.CSEV0017, "Property assignment blocked by sandbox: {0}");

    /// <summary>CSEV0018: Object construction blocked by sandbox: new {0}()</summary>
    public static readonly DiagnosticDescriptor SandboxConstructionBlocked =
        new(DiagnosticCode.CSEV0018, "Object construction blocked by sandbox: new {0}()");

    /// <summary>CSEV0019: Type '{0}' is not in the sandbox allowlist</summary>
    public static readonly DiagnosticDescriptor SandboxTypeBlocked =
        new(DiagnosticCode.CSEV0019, "Type '{0}' is not in the sandbox allowlist");

    // CSEV05xx — Null access and null safety

    /// <summary>CSEV0020: Cannot access {0} '{1}' on null</summary>
    public static readonly DiagnosticDescriptor NullMemberAccess =
        new(DiagnosticCode.CSEV0020, "Cannot access {0} '{1}' on null");

    /// <summary>CSEV0021: Cannot call method '{0}' on null</summary>
    public static readonly DiagnosticDescriptor NullMethodCall =
        new(DiagnosticCode.CSEV0021, "Cannot call method '{0}' on null");

    /// <summary>CSEV0022: Cannot call null as a function</summary>
    public static readonly DiagnosticDescriptor NullInvocation =
        new(DiagnosticCode.CSEV0022, "Cannot call null as a function");

    /// <summary>CSEV0023: Cannot assign to property '{0}' on null</summary>
    public static readonly DiagnosticDescriptor NullPropertyAssignment =
        new(DiagnosticCode.CSEV0023, "Cannot assign to property '{0}' on null");

    // CSEV06xx — Method resolution and invocation

    /// <summary>CS1955: Non-invocable member '{0}' cannot be used like a method</summary>
    public static readonly DiagnosticDescriptor NonCallableType =
        new(DiagnosticCode.CS1955, "Non-invocable member '{0}' cannot be used like a method");

    /// <summary>CSEV0024: Method '{0}' invocation failed</summary>
    public static readonly DiagnosticDescriptor MethodInvocationFailed =
        new(DiagnosticCode.CSEV0024, "Method '{0}' invocation failed");

    /// <summary>CS0121: The call is ambiguous between the following methods or properties: '{0}'</summary>
    public static readonly DiagnosticDescriptor AmbiguousMethodInvocation =
        new(DiagnosticCode.CS0121, "The call is ambiguous between the following methods or properties: '{0}'");

    /// <summary>CSEV0025: Call '{0}' requires runtime overload resolution</summary>
    public static readonly DiagnosticDescriptor RuntimeOverloadResolutionRequired =
        new(DiagnosticCode.CSEV0025, "Call '{0}' requires runtime overload resolution");

    /// <summary>CS1501: No overload for method '{0}' takes the given number of arguments</summary>
    public static readonly DiagnosticDescriptor NoApplicableOverload =
        new(DiagnosticCode.CS1501, "No overload for method '{0}' takes the given number of arguments");

    // CSEV07xx — Member access and property resolution

    /// <summary>CSEV0026: Indexer overloads with multiple parameters are not supported yet on type '{0}'.</summary>
    public static readonly DiagnosticDescriptor MultiParameterIndexerNotSupported =
        new(DiagnosticCode.CSEV0026, "Indexer overloads with multiple parameters are not supported yet on type '{0}'.");

    /// <summary>CSEV0027: Unsupported member type '{0}'</summary>
    public static readonly DiagnosticDescriptor UnsupportedMemberType =
        new(DiagnosticCode.CSEV0027, "Unsupported member type '{0}'");

    /// <summary>CSEV0028: Indexer access failed: {0}</summary>
    public static readonly DiagnosticDescriptor IndexerAccessFailed =
        new(DiagnosticCode.CSEV0028, "Indexer access failed: {0}");

    /// <summary>CSEV0029: No indexer found on type '{0}'</summary>
    public static readonly DiagnosticDescriptor NoIndexerOnType =
        new(DiagnosticCode.CSEV0029, "No indexer found on type '{0}'");

    // CSEV08xx — Type system and conversion

    /// <summary>CS0233: '{0}' does not have a predefined size, therefore sizeof can only be used in an unsafe context</summary>
    public static readonly DiagnosticDescriptor SizeofUnsupportedType =
        new(DiagnosticCode.CS0233, "'{0}' does not have a predefined size, therefore sizeof can only be used in an unsafe context");

    /// <summary>CSEV0030: Access to reflection types is not allowed: {0} ({1})</summary>
    public static readonly DiagnosticDescriptor ReflectionTypeAccessBlocked =
        new(DiagnosticCode.CSEV0030, "Access to reflection types is not allowed: {0} ({1})");

    /// <summary>CS8129: No suitable 'Deconstruct' instance or extension method was found for type '{0}'</summary>
    public static readonly DiagnosticDescriptor DeconstructionFailed =
        new(DiagnosticCode.CS8129, "No suitable 'Deconstruct' instance or extension method was found for type '{0}'");

    /// <summary>CS8124: Tuple must contain at least two elements</summary>
    public static readonly DiagnosticDescriptor TupleTooFewElements =
        new(DiagnosticCode.CS8124, "Tuple must contain at least two elements");

    /// <summary>CS8132: Cannot deconstruct a tuple of '{0}' elements into '{1}' variables</summary>
    public static readonly DiagnosticDescriptor DeconstructionCountMismatch =
        new(DiagnosticCode.CS8132, "Cannot deconstruct a tuple of '{1}' elements into '{0}' variables");

    /// <summary>CSEV0031: Type '{0}' does not have an 'Add' method for collection initializer</summary>
    public static readonly DiagnosticDescriptor CollectionInitializerNoAdd =
        new(DiagnosticCode.CSEV0031, "Type '{0}' does not have an 'Add' method for collection initializer");

    // CSEV09xx — Control flow and semantics

    /// <summary>CSEV0032: Semantic validation failed: {0}</summary>
    public static readonly DiagnosticDescriptor SemanticValidationFailed =
        new(DiagnosticCode.CSEV0032, "Semantic validation failed: {0}");

    /// <summary>CSEV0033: Expression nesting depth exceeded available stack space.</summary>
    public static readonly DiagnosticDescriptor ExpressionNestingDepthExceeded =
        new(DiagnosticCode.CSEV0033, "Expression nesting depth exceeded available stack space.");

    /// <summary>CSEV0034: Cannot slice null</summary>
    public static readonly DiagnosticDescriptor SliceNull =
        new(DiagnosticCode.CSEV0034, "Cannot slice null");

    /// <summary>CSEV0035: Slice step cannot be zero</summary>
    public static readonly DiagnosticDescriptor SliceStepZero =
        new(DiagnosticCode.CSEV0035, "Slice step cannot be zero");

    /// <summary>CSEV0036: Cannot slice type '{0}'</summary>
    public static readonly DiagnosticDescriptor SliceUnsupportedType =
        new(DiagnosticCode.CSEV0036, "Cannot slice type '{0}'");

    /// <summary>CSEV0037: Unknown compound assignment operator '{0}'</summary>
    public static readonly DiagnosticDescriptor UnknownCompoundAssignmentOperator =
        new(DiagnosticCode.CSEV0037, "Unknown compound assignment operator '{0}'");

    /// <summary>CSEV0038: Unsupported compound assignment base operator '{0}'</summary>
    public static readonly DiagnosticDescriptor UnsupportedCompoundBaseOperator =
        new(DiagnosticCode.CSEV0038, "Unsupported compound assignment base operator '{0}'");

    /// <summary>CSEV0039: Unsupported chained comparison operator: {0}</summary>
    public static readonly DiagnosticDescriptor UnsupportedChainedComparisonOperator =
        new(DiagnosticCode.CSEV0039, "Unsupported chained comparison operator: {0}");

    /// <summary>CSEV0040: Spread operator can only be used in array or object literals</summary>
    public static readonly DiagnosticDescriptor SpreadOutsideLiteral =
        new(DiagnosticCode.CSEV0040, "Spread operator can only be used in array or object literals");

    /// <summary>CS0185: A lock expression must be a reference type</summary>
    public static readonly DiagnosticDescriptor LockRequiresNonNull =
        new(DiagnosticCode.CS0185, "A lock expression must be a reference type");

    /// <summary>CSEV0041: goto case/default target not found</summary>
    public static readonly DiagnosticDescriptor GotoCaseTargetNotFound =
        new(DiagnosticCode.CSEV0041, "goto case/default target not found");

    /// <summary>CSEV0042: Pattern type '{0}' not yet implemented</summary>
    public static readonly DiagnosticDescriptor PatternNotImplemented =
        new(DiagnosticCode.CSEV0042, "Pattern type '{0}' not yet implemented");

    /// <summary>CSEV0043: Invalid out argument index '{0}'.</summary>
    public static readonly DiagnosticDescriptor InvalidOutArgumentIndex =
        new(DiagnosticCode.CSEV0043, "Invalid out argument index '{0}'.");

    /// <summary>CSEV0044: Unknown relational pattern operator '{0}'</summary>
    public static readonly DiagnosticDescriptor UnknownRelationalPatternOperator =
        new(DiagnosticCode.CSEV0044, "Unknown relational pattern operator '{0}'");

    /// <summary>CSEV0045: Unsupported tuple arity: {0}</summary>
    public static readonly DiagnosticDescriptor UnsupportedTupleArity =
        new(DiagnosticCode.CSEV0045, "Unsupported tuple arity: {0}");

    /// <summary>CSEV0046: Sequence contains no elements</summary>
    public static readonly DiagnosticDescriptor SequenceContainsNoElements =
        new(DiagnosticCode.CSEV0046, "Sequence contains no elements");

    /// <summary>CSEV0047: Unsupported delegate arity: {0}</summary>
    public static readonly DiagnosticDescriptor UnsupportedDelegateArity =
        new(DiagnosticCode.CSEV0047, "Unsupported delegate arity: {0}");

    /// <summary>CSEV0048: Could not resolve delegate type definition for '{0}'</summary>
    public static readonly DiagnosticDescriptor DelegateTypeDefinitionNotFound =
        new(DiagnosticCode.CSEV0048, "Could not resolve delegate type definition for '{0}'");

    /// <summary>CSEV0049: Cannot resolve instance of '{0}'. Either register it in IServiceProvider or ensure it has a parameterless constructor.</summary>
    public static readonly DiagnosticDescriptor CannotResolveModuleInstance =
        new(DiagnosticCode.CSEV0049, "Cannot resolve instance of '{0}'. Either register it in IServiceProvider or ensure it has a parameterless constructor.");
}
