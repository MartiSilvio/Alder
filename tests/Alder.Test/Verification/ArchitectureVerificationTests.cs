namespace Alder.Test.Verification;

[TestFixture]
public class ArchitectureVerificationTests
{
    [Test]
    public void TransitionalReflectionWrappers_AreRemoved()
    {
        var root = FindRepositoryRoot();

        Assert.That(File.Exists(Path.Combine(root, "src", "Alder", "Runtime", "ReflectionRuntime.cs")), Is.False);
        Assert.That(File.Exists(Path.Combine(root, "src", "Alder", "Runtime", "RuntimeGenericFactory.cs")), Is.False);
    }

    [Test]
    public void Source_DoesNotReferenceRemovedWrapperSymbols()
    {
        var root = FindRepositoryRoot();
        var src = Path.Combine(root, "src");

        var offenders = EnumerateSourceFiles(src)
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("RuntimeGenericFactory", StringComparison.Ordinal)
                    || text.Contains("ReflectionRuntime", StringComparison.Ordinal);
            })
            .ToArray();

        Assert.That(offenders, Is.Empty, "Found removed wrapper symbol references:\n" + string.Join('\n', offenders));
    }

    [Test]
    public void RuntimeGenericClosure_UsesCanonicalApiNames()
    {
        var root = FindRepositoryRoot();
        var file = Path.Combine(root, "src", "Alder", "Runtime", "Introspection", "RuntimeGenericClosure.cs");
        var text = File.ReadAllText(file);

        Assert.That(text, Does.Not.Contain("CloseGenericType("));
        Assert.That(text, Does.Not.Contain("CloseGenericMethod("));
        Assert.That(text, Does.Not.Contain("TryCloseGenericType("));
        Assert.That(text, Does.Not.Contain("TryCloseGenericMethod("));
    }

    [Test]
    public void RuntimeGenericClosure_DoesNotThrowForExpectedDynamicCodeMisses()
    {
        var root = FindRepositoryRoot();
        var file = Path.Combine(root, "src", "Alder", "Runtime", "Introspection", "RuntimeGenericClosure.cs");
        var text = File.ReadAllText(file);

        Assert.That(text, Does.Not.Contain("Runtime generic type closure requires dynamic code support."));
        Assert.That(text, Does.Not.Contain("Runtime generic method closure requires dynamic code support."));
    }

    [Test]
    public void StructuralObjectRuntime_DoesNotUseReflectionEmit()
    {
        var root = FindRepositoryRoot();
        var file = Path.Combine(root, "src", "Alder", "Runtime", "StructuralObjectTypeFactory.cs");
        var text = File.ReadAllText(file);

        Assert.That(text, Does.Not.Contain("System.Reflection.Emit"));
        Assert.That(text, Does.Not.Contain("AssemblyBuilder.DefineDynamicAssembly"));
        Assert.That(text, Does.Not.Contain("TypeBuilder"));
        Assert.That(text, Does.Not.Contain("CreateType("));
    }

    [Test]
    public void ExtensionDispatch_IsCentralizedInExtensionMethodResolver()
    {
        var root = FindRepositoryRoot();
        var methodInvoker = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "MethodInvoker.cs"));

        Assert.That(methodInvoker, Does.Not.Contain("private static (bool Success, object? Value) TryInvokeExtensionMethod("));
        Assert.That(methodInvoker, Does.Not.Contain("private static object?[]? TryResolveLambdaArgs("));
        Assert.That(methodInvoker, Does.Not.Contain("private static object?[] PrependTarget("));
    }

    [Test]
    public void ExtensionMethodNameNormalization_IsNotRequiredOutsideResolver()
    {
        var root = FindRepositoryRoot();
        var callBinder = File.ReadAllText(Path.Combine(root, "src", "Alder", "Binding", "Services", "CallBinderService.cs"));

        Assert.That(callBinder, Does.Not.Contain("NormalizeMethodName("));
    }

    [Test]
    public void ExtensionLambdaAdaptation_DoesNotUseExceptionFallback()
    {
        var root = FindRepositoryRoot();
        var resolver = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "ExtensionMethodResolver.cs"));

        Assert.That(resolver, Does.Not.Contain("""
            catch
                        {
                            resolved[i] = arg;
                            continue;
                        }
            """.Trim()));
    }

    [Test]
    public void ExtensionMethodResolver_DoesNotCarryDeadInvocationScaffolding()
    {
        var root = FindRepositoryRoot();
        var resolver = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "ExtensionMethodResolver.cs"));

        Assert.That(resolver, Does.Not.Contain("var hasSpecialArgs ="));
        Assert.That(resolver, Does.Not.Contain("private static object?[] BuildInvocationArgs("));
    }

    [Test]
    public void BoundedRuntimeCaches_AreCentralized()
    {
        var root = FindRepositoryRoot();
        var extensionResolver = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "ExtensionMethodResolver.cs"));
        var resolutionCache = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "OverloadResolution", "ResolutionCache.cs"));
        var typeResolver = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "TypeResolver.cs"));

        Assert.That(File.Exists(Path.Combine(root, "src", "Alder", "Runtime", "Collections", "BoundedConcurrentCache.cs")), Is.True);
        Assert.That(extensionResolver, Does.Not.Contain("ConcurrentQueue<InvocationCacheKey>"));
        Assert.That(extensionResolver, Does.Contain("BoundedConcurrentCache<InvocationCacheKey, ResolvedCall?>"));
        Assert.That(resolutionCache, Does.Not.Contain("ConcurrentQueue<ResolutionKey>"));
        Assert.That(resolutionCache, Does.Contain("BoundedConcurrentCache<ResolutionKey, ResolvedCall>"));
        Assert.That(typeResolver, Does.Not.Contain("ConcurrentQueue<string>"));
        Assert.That(typeResolver, Does.Contain("BoundedConcurrentCache<string, Type?>"));
    }

    [Test]
    public void TypedDispatchTraversal_IsCentralized()
    {
        var root = FindRepositoryRoot();
        var helper = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "TypedDispatchHelper.cs"));

        Assert.That(helper, Does.Contain("private static bool TryDispatchChain("));
        Assert.That(helper, Does.Contain("private static bool TryDispatchNamed("));
    }

    [Test]
    public void MethodInvoker_StaticDispatch_IsCentralized()
    {
        var root = FindRepositoryRoot();
        var methodInvoker = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "MethodInvoker.cs"));

        Assert.That(methodInvoker, Does.Contain("private static bool TryInvokeStaticDispatch("));
        Assert.That(CountOccurrences(methodInvoker, "GenericStaticDispatchHelper.TryInvoke("), Is.EqualTo(1));
        Assert.That(CountOccurrences(methodInvoker, "TypedDispatchHelper.TryInvokeStatic("), Is.EqualTo(1));
        Assert.That(methodInvoker, Does.Not.Contain("target is Type staticType && !HasNamedArgs(args)"));
    }

    [Test]
    public void MemberAccess_ResolvedReads_AreCentralized()
    {
        var root = FindRepositoryRoot();
        var helper = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "MemberAccess.cs"));

        Assert.That(helper, Does.Contain("private static object? GetResolvedStaticValue("));
        Assert.That(helper, Does.Contain("private static object? GetResolvedInstanceValue("));
    }

    [Test]
    public void MemberAccess_StaticTypeBranch_IsCentralized()
    {
        var root = FindRepositoryRoot();
        var helper = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "MemberAccess.cs"));

        Assert.That(helper, Does.Contain("private static bool TryGetStaticTypeMember("));
        Assert.That(helper, Does.Not.Contain("case Type staticType when TypedDispatchHelper.TryGetStaticMember"));
    }

    [Test]
    public void MemberAccess_ObjectPath_IsCentralized()
    {
        var root = FindRepositoryRoot();
        var helper = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "MemberAccess.cs"));

        Assert.That(helper, Does.Contain("private static object? GetObjectMember("));
        Assert.That(helper, Does.Contain("private static object? GetDictionaryMember("));
    }

    [Test]
    public void StaticMethodRef_RuntimeCarrier_IsRemoved()
    {
        var root = FindRepositoryRoot();
        var src = Path.Combine(root, "src");

        var offenders = EnumerateSourceFiles(src)
            .Where(file => File.ReadAllText(file).Contains("StaticMethodRef", StringComparison.Ordinal))
            .ToArray();

        Assert.That(offenders, Is.Empty, "Found stale StaticMethodRef references:\n" + string.Join('\n', offenders));
    }

    [Test]
    public void ModuleMethodRef_DoesNotCarryMethodInfo()
    {
        var root = FindRepositoryRoot();
        var file = Path.Combine(root, "src", "Alder", "Runtime", "ValueTypes.cs");
        var text = File.ReadAllText(file);

        Assert.That(text, Does.Not.Contain("record ModuleMethodRef(ModuleInfo Module, IServiceProvider? ServiceProvider, MethodInfo Method)"));
    }

    [Test]
    public void ResolvedCallExecution_IsCentralizedInMethodInvoker()
    {
        var root = FindRepositoryRoot();
        var resolvedEvaluator = File.ReadAllText(Path.Combine(root, "src", "Alder", "Interpretation", "Evaluators", "ResolvedCallEvaluator.cs"));
        var extensionResolver = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "ExtensionMethodResolver.cs"));

        Assert.That(resolvedEvaluator, Does.Not.Contain("ArgumentPreparer.Prepare("));
        Assert.That(resolvedEvaluator, Does.Not.Contain("MethodInvoker.InvokeMethodCore("));
        Assert.That(extensionResolver, Does.Not.Contain("ArgumentPreparer.Prepare("));
        Assert.That(extensionResolver, Does.Contain("MethodInvoker.InvokeResolvedCall("));
    }

    [Test]
    public void AlderEngine_ConfigAssembly_IsExtracted()
    {
        var root = FindRepositoryRoot();
        var engine = File.ReadAllText(Path.Combine(root, "src", "Alder", "AlderEngine.cs"));

        Assert.That(engine, Does.Not.Contain("private static AlderConfig BuildConfig("));
        Assert.That(engine, Does.Not.Contain("private static void RegisterGlobalFunctions("));
        Assert.That(engine, Does.Not.Contain("private static Func<object?[], object?> CreateFunctionDelegate("));
        Assert.That(File.Exists(Path.Combine(root, "src", "Alder", "AlderConfigFactory.cs")), Is.True);
    }

    [Test]
    public void AlderEngine_VariableProjection_IsExtracted()
    {
        var root = FindRepositoryRoot();
        var registration = File.ReadAllText(Path.Combine(root, "src", "Alder", "AlderEngine.Registration.cs"));
        var variablesFile = Path.Combine(root, "src", "Alder", "AlderEngine.Variables.cs");

        Assert.That(registration, Does.Not.Contain("ToTypedVariables("));
        Assert.That(File.Exists(Path.Combine(root, "src", "Alder", "VariableBindingProjector.cs")), Is.True);
        Assert.That(File.Exists(variablesFile), Is.False);
    }

    [Test]
    public void AlderEngine_EvaluationState_IsCentralized()
    {
        var root = FindRepositoryRoot();
        var eval = File.ReadAllText(Path.Combine(root, "src", "Alder", "AlderEngine.Evaluation.cs"));
        var evalAsync = File.ReadAllText(Path.Combine(root, "src", "Alder", "AlderEngine.EvaluationAsync.cs"));

        Assert.That(File.Exists(Path.Combine(root, "src", "Alder", "AlderEngine.EvaluationState.cs")), Is.True);
        Assert.That(eval, Does.Not.Contain("var constraintState = RentExecutionConstraintState();"));
        Assert.That(evalAsync, Does.Not.Contain("var constraintState = RentExecutionConstraintState();"));
        Assert.That(eval, Does.Not.Contain("var executionContext = CreateExecutionContext(context, cancellationToken);"));
        Assert.That(evalAsync, Does.Not.Contain("var executionContext = CreateExecutionContext(context, cancellationToken);"));
    }

    [Test]
    public void AlderEngine_CompiledInvocationState_IsCentralized()
    {
        var root = FindRepositoryRoot();
        var compiledExpression = File.ReadAllText(Path.Combine(root, "src", "Alder", "CompiledExpression.cs"));
        var compiledExtensions = File.ReadAllText(Path.Combine(root, "src", "Alder.Compiled", "AlderCompiledEngineExtensions.cs"));

        Assert.That(File.Exists(Path.Combine(root, "src", "Alder", "AlderEngine.CompiledInvocationState.cs")), Is.True);
        Assert.That(compiledExpression, Does.Not.Contain("var executionContext = _engine.CreateCompiledInvocationContext("));
        Assert.That(compiledExpression, Does.Not.Contain("var constraintState = _engine.RentExecutionConstraintState();"));
        Assert.That(compiledExtensions, Does.Not.Contain("var childContext = engine.CreateCompiledInvocationContext("));
        Assert.That(compiledExtensions, Does.Not.Contain("var constraintState = engine.RentExecutionConstraintState();"));
    }

    [Test]
    public void BindingContext_DoesNotOwnRuntimeTypeProbeLogic()
    {
        var root = FindRepositoryRoot();
        var bindingContext = File.ReadAllText(Path.Combine(root, "src", "Alder", "Binding", "BindingContext.cs"));

        Assert.That(bindingContext, Does.Not.Contain("public bool TryGetVariableType("));
        Assert.That(bindingContext, Does.Not.Contain("if (RuntimeContext.TryGetVariableType("));
        Assert.That(bindingContext, Does.Not.Contain("if (RuntimeContext.TryGet(name, out var fallbackValue)"));
    }

    [Test]
    public void AlderExpression_DoesNotOwnRuntimeOrCompilationState()
    {
        var root = FindRepositoryRoot();
        var expression = File.ReadAllText(Path.Combine(root, "src", "Alder", "AlderExpression.cs"));

        Assert.That(expression, Does.Not.Contain("ConditionalWeakTable<AlderContext, CachedBoundExpression>"));
        Assert.That(expression, Does.Not.Contain("_bindingUnavailable"));
        Assert.That(expression, Does.Not.Contain("_bindingUnavailableReason"));
        Assert.That(expression, Does.Not.Contain("internal volatile CompiledExpressionInfo? CompiledInfo"));
        Assert.That(expression, Does.Not.Contain("public bool IsCompiled =>"));
        Assert.That(expression, Does.Not.Contain("public bool? IsCompilable =>"));
        Assert.That(expression, Does.Not.Contain("public string? CompilationFailureReason =>"));
        Assert.That(expression, Does.Not.Contain("GetOrCreateBoundExpression("));
        Assert.That(expression, Does.Not.Contain("TryGetOrCreateBoundExpression("));
        Assert.That(expression, Does.Not.Contain("RecordBoundExecution("));
        Assert.That(expression, Does.Not.Contain("RecordBoundFallback("));
    }

    [Test]
    public void AlderEngine_CentralizesExpressionRuntimeState()
    {
        var root = FindRepositoryRoot();
        var stateFile = Path.Combine(root, "src", "Alder", "ExpressionRuntimeState.cs");
        var engineFile = Path.Combine(root, "src", "Alder", "AlderEngine.ExpressionState.cs");

        Assert.That(File.Exists(stateFile), Is.True);
        Assert.That(File.Exists(engineFile), Is.True);

        var engine = File.ReadAllText(engineFile);
        Assert.That(engine, Does.Contain("ConditionalWeakTable<AlderExpression, ExpressionRuntimeState>"));
        Assert.That(engine, Does.Contain("GetOrCreateBoundExpression("));
        Assert.That(engine, Does.Contain("GetCompiledInfo("));
    }

    [Test]
    public void CompiledConvenienceWrappers_AreRemoved()
    {
        var root = FindRepositoryRoot();

        Assert.That(File.Exists(Path.Combine(root, "src", "Alder.Compiled", "AlderStringCompileExtensions.cs")), Is.False);
        Assert.That(File.Exists(Path.Combine(root, "src", "Alder.Compiled", "AlderEvalCompileExtensions.cs")), Is.False);
    }

    [Test]
    public void CallEvaluation_ResultFinalization_IsCentralized()
    {
        var root = FindRepositoryRoot();
        var dynamicCall = File.ReadAllText(Path.Combine(root, "src", "Alder", "Interpretation", "Evaluators", "DynamicCallEvaluator.cs"));
        var resolvedCall = File.ReadAllText(Path.Combine(root, "src", "Alder", "Interpretation", "Evaluators", "ResolvedCallEvaluator.cs"));

        Assert.That(dynamicCall, Does.Not.Contain("ResolvedCallEvaluator.DefineOutVariablesIfAny("));
        Assert.That(dynamicCall, Does.Not.Contain("TypeHelpers.GuardReflectionLeak("));
        Assert.That(dynamicCall, Does.Not.Contain("ExecutionRuntime.CheckCollectionSize("));
        Assert.That(resolvedCall, Does.Contain("internal static object? FinalizeCallResult("));
    }

    [Test]
    public void FunctionRef_DoesNotWrapSingleDelegateMethod()
    {
        var root = FindRepositoryRoot();
        var file = Path.Combine(root, "src", "Alder", "Runtime", "ValueTypes.cs");
        var text = File.ReadAllText(file);

        Assert.That(text, Does.Not.Contain("public object? Invoke(object?[] args) => Function(args);"));
    }

    [Test]
    public void CallableClassification_IsCentralizedInMethodInvoker()
    {
        var root = FindRepositoryRoot();
        var identifierRuntime = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "Semantics", "IdentifierRuntime.cs"));
        var pipelineOperator = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "Extensions", "PipelineOperator.cs"));
        var methodInvoker = File.ReadAllText(Path.Combine(root, "src", "Alder", "Runtime", "MethodInvoker.cs"));

        Assert.That(identifierRuntime, Does.Not.Contain("private static bool IsPipelineCallable("));
        Assert.That(pipelineOperator, Does.Not.Contain("private static bool IsCallable("));
        Assert.That(methodInvoker, Does.Contain("internal static bool IsCallable(object? callee)"));
    }

    [Test]
    public void Parser_StatementStartClassification_IsCentralizedInStatementParser()
    {
        var root = FindRepositoryRoot();
        var expressionParser = File.ReadAllText(Path.Combine(root, "src", "Alder", "Parsing", "ExpressionParser.cs"));
        var statementParser = File.ReadAllText(Path.Combine(root, "src", "Alder", "Parsing", "StatementParser.cs"));

        Assert.That(expressionParser, Does.Not.Contain("private bool IsStatementKeyword("));
        Assert.That(expressionParser, Does.Contain("_statement.IsProgramStatementStart()"));
        Assert.That(expressionParser, Does.Contain("_statement.IsStatementStart()"));
        Assert.That(statementParser, Does.Contain("internal bool IsProgramStatementStart()"));
        Assert.That(statementParser, Does.Contain("internal bool IsStatementStart()"));
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root) =>
        Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(static file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Alder.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing Alder.sln.");
    }
}
