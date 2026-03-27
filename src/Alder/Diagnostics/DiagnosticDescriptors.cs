namespace Alder.Diagnostics;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor BadBinaryOps =
        new(DiagnosticCode.CS0019, "Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'");

    public static readonly DiagnosticDescriptor BadIndexerAccess =
        new(DiagnosticCode.CS0021, "Cannot apply indexing with [] to an expression of type '{0}'");

    public static readonly DiagnosticDescriptor BadUnaryOp =
        new(DiagnosticCode.CS0023, "Operator '{0}' cannot be applied to operand of type '{1}'");

    public static readonly DiagnosticDescriptor NoImplicitConversion =
        new(DiagnosticCode.CS0029, "Cannot implicitly convert type '{0}' to '{1}'");

    public static readonly DiagnosticDescriptor NoExplicitConversion =
        new(DiagnosticCode.CS0030, "Cannot convert type '{0}' to '{1}'");

    public static readonly DiagnosticDescriptor ConstantValueCannotConvert =
        new(DiagnosticCode.CS0031, "Constant value '{0}' cannot be converted to a '{1}'");

    public static readonly DiagnosticDescriptor NullToNonNullable =
        new(DiagnosticCode.CS0037, "Cannot convert null to '{0}' because it is a non-nullable value type");

    public static readonly DiagnosticDescriptor ExplicitConversionExists =
        new(DiagnosticCode.CS0266, "Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)");

    public static readonly DiagnosticDescriptor NameNotInContext =
        new(DiagnosticCode.CS0103, "The name '{0}' does not exist in the current context");

    public static readonly DiagnosticDescriptor AmbiguousReference =
        new(DiagnosticCode.CS0104, "'{0}' is an ambiguous reference between '{1}' and '{2}'");

    public static readonly DiagnosticDescriptor NoMemberOnType =
        new(DiagnosticCode.CS0117, "'{0}' does not contain a definition for '{1}'");

    public static readonly DiagnosticDescriptor TypeNotFound =
        new(DiagnosticCode.CS0246, "The type or namespace name '{0}' could not be found (are you missing a using directive or an assembly reference?)");

    public static readonly DiagnosticDescriptor AmbiguousMethodInvocation =
        new(DiagnosticCode.CS0121, "The call is ambiguous between the following methods or properties: '{0}'");

    public static readonly DiagnosticDescriptor DelegateConversionFailed =
        new(DiagnosticCode.CS0123, "Cannot convert '{0}' to delegate type '{1}'.");

    public static readonly DiagnosticDescriptor NoApplicableOverload =
        new(DiagnosticCode.CS1501, "No overload for method '{0}' takes the given number of arguments");

    public static readonly DiagnosticDescriptor CantConvAnonMethParams =
        new(DiagnosticCode.CS1661, "Cannot convert lambda expression to type '{0}' because the parameter types do not match the delegate parameter types");

    public static readonly DiagnosticDescriptor NonCallableType =
        new(DiagnosticCode.CS1955, "Non-invocable member '{0}' cannot be used like a method");

    public static readonly DiagnosticDescriptor MissingRequiredArgument =
        new(DiagnosticCode.CS7036, "There is no argument given that corresponds to the required parameter '{0}' of '{1}'");

    public static readonly DiagnosticDescriptor CantConvAnonMethReturnType =
        new(DiagnosticCode.CS8934, "Cannot convert lambda expression to type '{0}' because the return type does not match the delegate return type");

    public static readonly DiagnosticDescriptor DuplicateLocalVariable =
        new(DiagnosticCode.CS0128, "A local variable or function named '{0}' is already defined in this scope");

    public static readonly DiagnosticDescriptor AssignmentRequiresVariable =
        new(DiagnosticCode.CS0131, "The left-hand side of an assignment must be a variable, property or indexer");

    public static readonly DiagnosticDescriptor ReadonlyAssignment =
        new(DiagnosticCode.CS0191, "A readonly field cannot be assigned to");

    public static readonly DiagnosticDescriptor NullToImplicitlyTyped =
        new(DiagnosticCode.CS0815, "Cannot assign null to an implicitly-typed variable");

    public static readonly DiagnosticDescriptor BreakOrContinueOutsideLoop =
        new(DiagnosticCode.CS0139, "No enclosing loop out of which to break or continue");

    public static readonly DiagnosticDescriptor ThrowExpressionMustBeException =
        new(DiagnosticCode.CS0155, "The type caught or thrown must be derived from System.Exception");

    public static readonly DiagnosticDescriptor ThrowOutsideCatch =
        new(DiagnosticCode.CS0156, "A throw statement with no arguments is not allowed outside of a catch clause");

    public static readonly DiagnosticDescriptor LabelNotFound =
        new(DiagnosticCode.CS0159, "No such label '{0}' within the scope of the goto statement");

    public static readonly DiagnosticDescriptor CaseFallThrough =
        new(DiagnosticCode.CS0163, "Control cannot fall through from one case label to another");

    public static readonly DiagnosticDescriptor LockRequiresNonNull =
        new(DiagnosticCode.CS0185, "A lock expression must be a reference type");

    public static readonly DiagnosticDescriptor MemberNotFound =
        new(DiagnosticCode.CS1061, "'{0}' does not contain a definition for '{1}'");

    public static readonly DiagnosticDescriptor ForeachRequiresIEnumerable =
        new(DiagnosticCode.CS1579, "foreach statement cannot operate on variables of type '{0}' because '{0}' does not contain a public instance or extension definition for 'GetEnumerator'");

    public static readonly DiagnosticDescriptor NoMatchingConstructor =
        new(DiagnosticCode.CS1729, "'{0}' does not contain a constructor that takes {1} arguments");

    public static readonly DiagnosticDescriptor SyntaxExpected =
        new(DiagnosticCode.CS1003, "Syntax error, '{0}' expected");

    public static readonly DiagnosticDescriptor InvalidNumber =
        new(DiagnosticCode.CS1013, "Invalid number");

    public static readonly DiagnosticDescriptor GeneralCatchMustBeLast =
        new(DiagnosticCode.CS1017, "Try statement already has an empty catch block");

    public static readonly DiagnosticDescriptor IntegralConstantTooLarge =
        new(DiagnosticCode.CS1021, "Integral constant is too large");

    public static readonly DiagnosticDescriptor InvalidExpressionTerm =
        new(DiagnosticCode.CS1525, "Invalid expression term '{0}'");

    public static readonly DiagnosticDescriptor UnterminatedRawStringLiteral =
        new(DiagnosticCode.CS8997, "Unterminated raw string literal");

    public static readonly DiagnosticDescriptor ExpressionExpected =
        new(DiagnosticCode.CS1733, "Expression expected");

    public static readonly DiagnosticDescriptor QueryBodyMustEndWithSelectOrGroup =
        new(DiagnosticCode.CS0742, "A query body must end with a select clause or a group clause");

    public static readonly DiagnosticDescriptor ExpectedContextualKeyword =
        new(DiagnosticCode.CS0744, "Expected contextual keyword '{0}'");

    public static readonly DiagnosticDescriptor SizeofUnsupportedType =
        new(DiagnosticCode.CS0233, "'{0}' does not have a predefined size, therefore sizeof can only be used in an unsafe context");

    public static readonly DiagnosticDescriptor FeatureNotValidInExpressionTree =
        new(DiagnosticCode.CS7053, "An expression tree may not contain '{0}'");

    public static readonly DiagnosticDescriptor ExpressionNestingDepthExceeded =
        new(DiagnosticCode.CS8078, "An expression is too long or complex to compile");

    public static readonly DiagnosticDescriptor TupleTooFewElements =
        new(DiagnosticCode.CS8124, "Tuple must contain at least two elements");

    public static readonly DiagnosticDescriptor DeconstructionFailed =
        new(DiagnosticCode.CS8129, "No suitable 'Deconstruct' instance or extension method was found for type '{0}'");

    public static readonly DiagnosticDescriptor DeconstructionCountMismatch =
        new(DiagnosticCode.CS8132, "Cannot deconstruct a tuple of '{1}' elements into '{0}' variables");

    public static readonly DiagnosticDescriptor SwitchExpressionNonExhaustive =
        new(DiagnosticCode.CS8510, "Switch expression does not handle all possible values of its input type. Value '{0}' is not handled.");

    public static readonly DiagnosticDescriptor StrictCompilationFailed =
        new(DiagnosticCode.ALDR0001, "Strict compilation mode could not compile the expression to IL: {0}");

    public static readonly DiagnosticDescriptor BindingFailed =
        new(DiagnosticCode.ALDR0002, "{0}");

    public static readonly DiagnosticDescriptor RuntimeOverloadResolutionRequired =
        new(DiagnosticCode.ALDR0003, "Call '{0}' requires runtime overload resolution");

    public static readonly DiagnosticDescriptor ParseAsExpressionRequiresGenericDelegate =
        new(DiagnosticCode.ALDR0010, "ParseAsExpression requires a generic Func-style delegate type; got '{0}'.");

    public static readonly DiagnosticDescriptor ParseAsExpressionRequiresLambda =
        new(DiagnosticCode.ALDR0011, "Expression must be a lambda (e.g., '{0}').");

    public static readonly DiagnosticDescriptor ExtendedModeRequired =
        new(DiagnosticCode.ALDR0020, "Feature '{0}' is not available in Standard mode. Use LanguageMode.Extended to enable non-standard syntax extensions.");

    public static readonly DiagnosticDescriptor SandboxMethodCallBlocked =
        new(DiagnosticCode.ALDR0100, "Method calls blocked by sandbox: {0}");

    public static readonly DiagnosticDescriptor SandboxAssignmentBlocked =
        new(DiagnosticCode.ALDR0101, "Assignment blocked by sandbox: {0}");

    public static readonly DiagnosticDescriptor SandboxIndexAssignmentBlocked =
        new(DiagnosticCode.ALDR0102, "Index assignment blocked by sandbox: [{0}] = ...");

    public static readonly DiagnosticDescriptor SandboxPropertyAccessBlocked =
        new(DiagnosticCode.ALDR0103, "Property access blocked by sandbox: {0}");

    public static readonly DiagnosticDescriptor SandboxStaticMemberAccessBlocked =
        new(DiagnosticCode.ALDR0104, "Static member access blocked by sandbox: {0}.{1}");

    public static readonly DiagnosticDescriptor SandboxPropertyAssignmentBlocked =
        new(DiagnosticCode.ALDR0105, "Property assignment blocked by sandbox: {0}");

    public static readonly DiagnosticDescriptor SandboxConstructionBlocked =
        new(DiagnosticCode.ALDR0106, "Object construction blocked by sandbox: new {0}()");

    public static readonly DiagnosticDescriptor SandboxTypeBlocked =
        new(DiagnosticCode.ALDR0107, "Type '{0}' is blocked by the sandbox");

    public static readonly DiagnosticDescriptor ReflectionTypeAccessBlocked =
        new(DiagnosticCode.ALDR0108, "Access to reflection types is not allowed: {0} ({1})");


    public static readonly DiagnosticDescriptor StatementLimitExceeded =
        new(DiagnosticCode.ALDR0200, "Execution exceeded maximum statement count ({0}). {1} statements executed.");

    public static readonly DiagnosticDescriptor TimeoutExceeded =
        new(DiagnosticCode.ALDR0201, "Execution exceeded maximum timeout ({0}ms). {1}ms elapsed.");

    public static readonly DiagnosticDescriptor ArrayLengthExceeded =
        new(DiagnosticCode.ALDR0202, "Array length {0} exceeds maximum allowed ({1})");

    public static readonly DiagnosticDescriptor LoopIterationLimitExceeded =
        new(DiagnosticCode.ALDR0203, "Execution exceeded maximum loop iteration count ({0}). {1} iterations executed.");

    #region Runtime (Alder)

    public static readonly DiagnosticDescriptor NullMemberAccess =
        new(DiagnosticCode.ALDR0300, "Cannot access {0} '{1}' on null");

    public static readonly DiagnosticDescriptor NullMethodCall =
        new(DiagnosticCode.ALDR0301, "Cannot call method '{0}' on null");

    public static readonly DiagnosticDescriptor NullInvocation =
        new(DiagnosticCode.ALDR0302, "Cannot call null as a function");

    public static readonly DiagnosticDescriptor NullPropertyAssignment =
        new(DiagnosticCode.ALDR0303, "Cannot assign to property '{0}' on null");

    public static readonly DiagnosticDescriptor MethodInvocationFailed =
        new(DiagnosticCode.ALDR0304, "Method '{0}' invocation failed");

    public static readonly DiagnosticDescriptor MultiParameterIndexerNotSupported =
        new(DiagnosticCode.ALDR0305, "Indexer overloads with multiple parameters are not supported yet on type '{0}'.");

    public static readonly DiagnosticDescriptor UnsupportedMemberType =
        new(DiagnosticCode.ALDR0306, "Unsupported member type '{0}'");

    public static readonly DiagnosticDescriptor IndexerAccessFailed =
        new(DiagnosticCode.ALDR0307, "Indexer access failed: {0}");

    public static readonly DiagnosticDescriptor SemanticValidationFailed =
        new(DiagnosticCode.ALDR0308, "Semantic validation failed: {0}");

    public static readonly DiagnosticDescriptor PatternNotImplemented =
        new(DiagnosticCode.ALDR0309, "Pattern type '{0}' not yet implemented");

    public static readonly DiagnosticDescriptor UnknownRelationalPatternOperator =
        new(DiagnosticCode.ALDR0310, "Unknown relational pattern operator '{0}'");

    public static readonly DiagnosticDescriptor InvalidOutArgumentIndex =
        new(DiagnosticCode.ALDR0311, "Invalid out argument index '{0}'.");

    public static readonly DiagnosticDescriptor UnsupportedTupleArity =
        new(DiagnosticCode.ALDR0312, "Unsupported tuple arity: {0}");

    public static readonly DiagnosticDescriptor UnsupportedDelegateArity =
        new(DiagnosticCode.ALDR0313, "Unsupported delegate arity: {0}");

    public static readonly DiagnosticDescriptor DelegateTypeDefinitionNotFound =
        new(DiagnosticCode.ALDR0314, "Could not resolve delegate type definition for '{0}'");

    public static readonly DiagnosticDescriptor CannotResolveModuleInstance =
        new(DiagnosticCode.ALDR0315, "Cannot resolve instance of '{0}'. Either register it in IServiceProvider or ensure it has a parameterless constructor.");

    #endregion

    #region Extended mode (Alder)

    public static readonly DiagnosticDescriptor SliceNull =
        new(DiagnosticCode.ALDR0400, "Cannot slice null");

    public static readonly DiagnosticDescriptor SliceStepZero =
        new(DiagnosticCode.ALDR0401, "Slice step cannot be zero");

    public static readonly DiagnosticDescriptor SliceUnsupportedType =
        new(DiagnosticCode.ALDR0402, "Cannot slice type '{0}'");

    public static readonly DiagnosticDescriptor UnknownCompoundAssignmentOperator =
        new(DiagnosticCode.ALDR0403, "Unsupported compound assignment operator '{0}'");

    public static readonly DiagnosticDescriptor UnsupportedChainedComparisonOperator =
        new(DiagnosticCode.ALDR0404, "Unsupported chained comparison operator: {0}");

    public static readonly DiagnosticDescriptor SpreadOutsideLiteral =
        new(DiagnosticCode.ALDR0405, "Spread operator can only be used in array or object literals");

    #endregion

    #region AOT (ALDR05xx)

    public static readonly DiagnosticDescriptor AotTypeNotRegistered =
        new(DiagnosticCode.ALDR0500,
            "Type '{0}' is not available in this environment. " +
            "In NativeAOT, add [AlderRegistered(typeof({0}))] to your AlderTypeContext");

    #endregion
}
