using System.Reflection;

namespace Alder.Test.Core;

/// <summary>
/// Reflection-based API surface inventory for AlderEngine.
/// Acts as a living specification: if a public method is added, removed, or
/// renamed without updating this test, the build will catch it immediately.
/// </summary>
[TestFixture]
public class ApiSurfaceTests
{
    [Test]
    public void AlderEngine_PublicMethodNames_MatchExpectedInventory()
    {
        var methods = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var expected = new[]
        {
            "Compile",
            "CreateChild",
            "Dispose",
            "Evaluate",
            "EvaluateAsync",
            "EvaluateWithTrace",
            "GetRegisteredModules",
            "Parse",
            "SetVariable",
            "SetVariables",
            "SetVariablesPreservingRuntimeTypes",
            "TryCompile",
            "TryEvaluate",
            "TryParse",
            "TryValidate",
        }.OrderBy(n => n).ToList();

        Assert.That(methods, Is.EqualTo(expected));
    }

    [Test]
    public void Evaluate_Has4Overloads()
    {
        var overloads = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Evaluate")
            .ToList();

        Assert.That(overloads, Has.Count.EqualTo(10));
    }

    [Test]
    public void Evaluate_Has4NonGeneric_And4Generic()
    {
        var overloads = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Evaluate")
            .ToList();

        var nonGeneric = overloads.Where(m => !m.IsGenericMethod).ToList();
        var generic = overloads.Where(m => m.IsGenericMethod).ToList();

        Assert.That(nonGeneric, Has.Count.EqualTo(5));
        Assert.That(generic, Has.Count.EqualTo(5));
    }

    [Test]
    public void AlderOptions_HasExpectedBuilders()
    {
        var builderTypes = typeof(AlderOptions).GetNestedTypes(BindingFlags.Public)
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        var expected = new[] { "AotBuilder", "FunctionBuilder", "ModuleBuilder", "TypeBuilder" }
            .OrderBy(n => n).ToList();

        Assert.That(builderTypes, Is.EqualTo(expected));
    }

    [Test]
    public void EvaluateAsync_Exists()
    {
        var asyncMethods = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "EvaluateAsync")
            .ToList();

        Assert.That(asyncMethods, Has.Count.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void AlderExpression_Ast_IsInternal()
    {
        var astProp = typeof(AlderExpression).GetProperty("Ast", BindingFlags.Public | BindingFlags.Instance);
        Assert.That(astProp, Is.Null, "Ast should not be public");

        var internalAst = typeof(AlderExpression).GetProperty("Ast", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(internalAst, Is.Not.Null, "Ast should be internal");
    }

    [Test]
    public void AlderExpression_GetVariables_Exists()
    {
        var method = typeof(AlderExpression).GetMethod("GetVariables", BindingFlags.Public | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(IReadOnlyList<string>)));
    }

    [Test]
    public void AlderEngine_TryCompile_IsPublic()
    {
        var method = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "TryCompile" && m.GetParameters().Length == 1)
            .SingleOrDefault();
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void AlderEngine_Compile_IsPublic()
    {
        var method = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Compile" && m.GetParameters().Length == 1)
            .SingleOrDefault();
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void AlderDiagnostic_TypeExists_WithExpectedProperties()
    {
        var type = typeof(AlderDiagnostic);
        Assert.That(type, Is.Not.Null);

        Assert.That(type.GetProperty("Span", BindingFlags.Public | BindingFlags.Instance), Is.Not.Null);
        Assert.That(type.GetProperty("Severity", BindingFlags.Public | BindingFlags.Instance), Is.Not.Null);
        Assert.That(type.GetProperty("Message", BindingFlags.Public | BindingFlags.Instance), Is.Not.Null);
        Assert.That(type.GetProperty("Code", BindingFlags.Public | BindingFlags.Instance), Is.Not.Null);
    }

    [Test]
    public void AlderCompiledExpression_GenericType_Exists_WithInvokeMethods()
    {
        var openType = typeof(AlderCompiledExpression<>);
        Assert.That(openType, Is.Not.Null);
        Assert.That(openType.IsGenericTypeDefinition, Is.True);

        var closedType = typeof(AlderCompiledExpression<int>);
        var invokeMethods = closedType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Invoke")
            .ToList();

        Assert.That(invokeMethods, Has.Count.EqualTo(2));
    }

    [Test]
    public void CompiledExpressionDelegate_IsInternal()
    {
        var type = typeof(CompiledExpressionDelegate);
        Assert.That(type, Is.Not.Null);
        Assert.That(typeof(Delegate).IsAssignableFrom(type), Is.True);
        Assert.That(type.IsPublic, Is.False, "CompiledExpressionDelegate should be internal");
    }

    [Test]
    public void TryEvaluate_Has2Overloads()
    {
        var overloads = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "TryEvaluate")
            .ToList();

        Assert.That(overloads, Has.Count.EqualTo(2));
    }

    [Test]
    public void TryValidate_Exists()
    {
        var method = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "TryValidate")
            .SingleOrDefault();

        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void Compile_Methods_ExistOnCoreEngine()
    {
        var compileMethods = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Compile")
            .ToList();

        Assert.That(compileMethods, Has.Count.EqualTo(1));

        var tryCompileMethods = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "TryCompile")
            .ToList();

        Assert.That(tryCompileMethods, Has.Count.EqualTo(1));
    }

    [Test]
    public void CompileToFunc_DoesNotExistOnCoreEngine()
    {
        var method = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "CompileToFunc")
            .SingleOrDefault();

        Assert.That(method, Is.Null);
    }

    [Test]
    public void CompiledExtensionApi_Exists()
    {
        var extensionMethods = typeof(AlderCompiledEngineExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), inherit: false))
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var expected = new[]
        {
            "Compile",
            "CompileExpression",
            "CompileToFunc",
            "ParseAndCompile",
            "ParseAsExpression",
            "TryParseAsExpression",
        }.OrderBy(n => n).ToList();

        Assert.That(extensionMethods, Is.EqualTo(expected));
    }

    [Test]
    public void Evaluate_Overloads_HaveConsistentParameterOrder()
    {
        var overloads = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Evaluate")
            .ToList();

        foreach (var method in overloads)
        {
            var parameters = method.GetParameters();
            Assert.That(parameters.Length, Is.GreaterThanOrEqualTo(1),
                $"Evaluate must have at least 1 parameter");

            var firstParam = parameters[0];
            Assert.That(
                firstParam.ParameterType == typeof(string) ||
                firstParam.ParameterType == typeof(AlderExpression),
                Is.True,
                $"First parameter of {method} must be expression, was {firstParam.ParameterType.Name}");

            var lastParam = parameters[^1];
            var isParams = lastParam.IsDefined(typeof(ParamArrayAttribute), false);
            Assert.That(
                lastParam.ParameterType == typeof(CancellationToken) || isParams,
                Is.True,
                $"Last parameter of {method} must be CancellationToken or params");

            var variablesIdx = Array.FindIndex(parameters, p => p.Name == "variables");
            var cancellationIdx = Array.FindIndex(parameters, p => p.Name == "cancellationToken");

            if (variablesIdx >= 0 && cancellationIdx >= 0)
            {
                Assert.That(variablesIdx, Is.LessThan(cancellationIdx),
                    $"variables must come before cancellationToken in {method}");
            }
        }
    }

    [Test]
    public void TryEvaluate_Overloads_HaveConsistentParameterOrder()
    {
        var overloads = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "TryEvaluate")
            .ToList();

        foreach (var method in overloads)
        {
            var parameters = method.GetParameters();
            Assert.That(parameters.Length, Is.GreaterThanOrEqualTo(2));

            // First parameter is string expression
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
            Assert.That(parameters[0].Name, Is.EqualTo("expression"));

            // Second parameter is out result
            Assert.That(parameters[1].IsOut, Is.True,
                $"Second parameter of TryEvaluate must be 'out result'");
            Assert.That(parameters[1].Name, Is.EqualTo("result"));

            // CancellationToken is last
            var lastParam = parameters[^1];
            Assert.That(lastParam.ParameterType, Is.EqualTo(typeof(CancellationToken)),
                $"Last parameter of TryEvaluate must be CancellationToken");

            var variablesIdx = Array.FindIndex(parameters, p => p.Name == "variables");
            var cancellationIdx = Array.FindIndex(parameters, p => p.Name == "cancellationToken");
            if (variablesIdx >= 0 && cancellationIdx >= 0)
            {
                Assert.That(variablesIdx, Is.LessThan(cancellationIdx),
                    $"variables must come before cancellationToken in {method}");
            }
        }
    }

    [Test]
    public void DiagnosticSeverity_IsPublicEnum()
    {
        var type = typeof(DiagnosticSeverity);
        Assert.That(type.IsEnum, Is.True);
        Assert.That(type.IsPublic, Is.True);
        Assert.That(Enum.GetNames(type), Does.Contain("Error"));
        Assert.That(Enum.GetNames(type), Does.Contain("Warning"));
    }

    [Test]
    public void PublicApiSurface_ContainsOnlyExpectedTypes()
    {
        var assembly = typeof(AlderEngine).Assembly;
        var publicTypes = assembly.GetExportedTypes()
            .Select(t => t.FullName!)
            .OrderBy(n => n)
            .ToList();

        var expected = new[]
        {
            "Alder.Aot.AlderBuiltInContext",
            "Alder.Aot.AlderRegisteredAttribute",
            "Alder.Aot.AlderTypeContext",
            "Alder.Aot.GeneratedCodeHelpers",
            "Alder.Aot.TypedDispatch",
            "Alder.Attributes.AlderFunctionAttribute",
            "Alder.Attributes.AlderModuleAttribute",
            "Alder.AlderCompiledExpression`1",
            "Alder.AlderDiagnostic",
            "Alder.AlderEngine",
            "Alder.AlderEval",
            "Alder.AlderEngine+RegisteredModule",
            "Alder.AlderException",
            "Alder.AlderExecutionLimitException",
            "Alder.AlderExpression",
            "Alder.AlderOptions",
            "Alder.AlderStringExtensions",
            "Alder.AlderOptions+AotBuilder",
            "Alder.AlderOptions+FunctionBuilder",
            "Alder.AlderOptions+ModuleBuilder",
            "Alder.AlderOptions+TypeBuilder",
            "Alder.DefaultExpressionCompiler",
            "Alder.DiagnosticSeverity",
            "Alder.ExecutionConstraints",
            "Alder.ExecutionLimitType",
            "Alder.IExpressionCompiler",
            "Alder.LanguageMode",
            "Alder.SandboxOptions",
            "Alder.Security.SecurityPolicy",
            "Alder.Security.SecurityPolicy+Builder",
            "Alder.Diagnostics.DiagnosticCode",
            "Alder.Diagnostics.DiagnosticDescriptor",
            "Alder.Diagnostics.DiagnosticDescriptors",
            "Alder.Text.LinePosition",
            "Alder.Text.SourceText",
            "Alder.Text.TextSpan",
            "Alder.Tracing.EvaluationTraceResult",
            "Alder.Tracing.TraceNode",
        }.OrderBy(n => n).ToList();

        Assert.That(publicTypes, Is.EqualTo(expected),
            $"Unexpected public types: {string.Join(", ", publicTypes.Except(expected))}\n" +
            $"Missing expected types: {string.Join(", ", expected.Except(publicTypes))}");
    }

    [Test]
    public void ParserTypes_AreNotPublic()
    {
        var assembly = typeof(AlderEngine).Assembly;
        var parserTypes = assembly.GetTypes()
            .Where(t => t is { Namespace: "Alder.Parsing", IsPublic: true })
            .Select(t => t.Name)
            .ToList();

        Assert.That(parserTypes, Is.Empty,
            $"Parser types should be internal: {string.Join(", ", parserTypes)}");
    }

    [Test]
    public void RuntimeTypes_AreNotPublic()
    {
        var assembly = typeof(AlderEngine).Assembly;
        var runtimeTypes = assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Alder.Runtime") == true && t.IsPublic)
            .Select(t => t.Name)
            .ToList();

        Assert.That(runtimeTypes, Is.Empty,
            $"Runtime types should be internal: {string.Join(", ", runtimeTypes)}");
    }

    [Test]
    public void StringExtensions_CoverAllStringBasedEngineEvaluateOverloads()
    {
        // Get all Evaluate/TryEvaluate methods on AlderEngine whose first param is string
        var engineMethods = typeof(AlderEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name is "Evaluate" or "TryEvaluate")
            .Where(m => m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType == typeof(string))
            .ToList();

        // Get all extension methods on AlderStringExtensions
        var extensionMethods = typeof(AlderStringExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), inherit: false))
            .ToList();

        var missing = new List<string>();

        foreach (var engineMethod in engineMethods)
        {
            var engineParams = engineMethod.GetParameters();

            // Build expected extension param types: skip IServiceProvider (engine-level concern),
            // keep everything else. First param (string) becomes "this string".
            var expectedParamTypes = engineParams
                .Where(p => p.ParameterType != typeof(IServiceProvider))
                .Select(p => p.ParameterType)
                .ToList();

            // Find matching extension method: same name, same generic arity, same param types
            var match = extensionMethods.FirstOrDefault(ext =>
            {
                if (ext.Name != engineMethod.Name) return false;
                if (ext.IsGenericMethod != engineMethod.IsGenericMethod) return false;
                if (ext.IsGenericMethod && ext.GetGenericArguments().Length != engineMethod.GetGenericArguments().Length)
                    return false;

                var extParams = ext.GetParameters();
                if (extParams.Length != expectedParamTypes.Count) return false;

                for (var i = 0; i < extParams.Length; i++)
                {
                    if (!TypesMatch(extParams[i].ParameterType, expectedParamTypes[i]))
                        return false;
                }

                return true;
            });

            if (match == null)
            {
                var signature = FormatMethodSignature(engineMethod, skipServiceProvider: true);
                missing.Add(signature);
            }
        }

        Assert.That(missing, Is.Empty,
            $"AlderStringExtensions is missing overloads for these AlderEngine methods:\n  " +
            string.Join("\n  ", missing));
    }

    private static bool TypesMatch(Type a, Type b)
    {
        // Unwrap by-ref (out/ref params) before comparing
        if (a.IsByRef) a = a.GetElementType()!;
        if (b.IsByRef) b = b.GetElementType()!;

        if (a == b) return true;

        // Generic type parameters: compare by position
        if (a.IsGenericParameter && b.IsGenericParameter)
            return a.GenericParameterPosition == b.GenericParameterPosition;

        // Generic types like Nullable<T>, IDictionary<K,V>: compare definitions
        if (a.IsGenericType && b.IsGenericType)
            return a.GetGenericTypeDefinition() == b.GetGenericTypeDefinition();

        return false;
    }

    private static string FormatMethodSignature(MethodInfo method, bool skipServiceProvider)
    {
        var generic = method.IsGenericMethod ? $"<{string.Join(", ", method.GetGenericArguments().Select(a => a.Name))}>" : "";
        var parameters = method.GetParameters()
            .Where(p => !skipServiceProvider || p.ParameterType != typeof(IServiceProvider))
            .Select(p => $"{p.ParameterType.Name} {p.Name}");
        return $"{method.Name}{generic}({string.Join(", ", parameters)})";
    }

    [Test]
    public void InterpretationTypes_AreNotPublic()
    {
        var assembly = typeof(AlderEngine).Assembly;
        var interpretationTypes = assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Alder.Interpretation") == true && t.IsPublic)
            .Select(t => t.Name)
            .ToList();

        Assert.That(interpretationTypes, Is.Empty,
            $"Interpretation types should be internal: {string.Join(", ", interpretationTypes)}");
    }
}
