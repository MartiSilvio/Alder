using System.Reflection;
using CsEval.Parsing;

namespace CsEval.Test.Core;

/// <summary>
/// Reflection-based API surface inventory for CsEvalEngine.
/// Acts as a living specification: if a public method is added, removed, or
/// renamed without updating this test, the build will catch it immediately.
/// </summary>
[TestFixture]
public class ApiSurfaceTests
{
    // ----------------------------------------------------------------
    // Method inventory
    // ----------------------------------------------------------------

    [Test]
    public void CsEvalEngine_PublicMethodNames_MatchExpectedInventory()
    {
        var methods = typeof(CsEvalEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var expected = new[]
        {
            "AddAssembly",
            "AddUsing",
            "Compile",
            "CompileToFunc",
            "CreateChild",
            "Evaluate",
            "GetRegisteredModules",
            "Parse",
            "ParseAndCompile",
            "RegisterExtensionMethods",
            "RegisterFromAssembly",
            "RegisterFromType",
            "RegisterFunction",
            "RegisterModule",
            "SetVariable",
            "SetVariables",
            "TryEvaluate",
            "TryParse",
            "TryValidate",
        }.OrderBy(n => n).ToList();

        Assert.That(methods, Is.EqualTo(expected));
    }

    // ----------------------------------------------------------------
    // Overload counts
    // ----------------------------------------------------------------

    [Test]
    public void Evaluate_Has4Overloads()
    {
        var overloads = typeof(CsEvalEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Evaluate")
            .ToList();

        Assert.That(overloads, Has.Count.EqualTo(4));
    }

    [Test]
    public void Evaluate_Has2NonGeneric_And2Generic()
    {
        var overloads = typeof(CsEvalEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Evaluate")
            .ToList();

        var nonGeneric = overloads.Where(m => !m.IsGenericMethod).ToList();
        var generic = overloads.Where(m => m.IsGenericMethod).ToList();

        Assert.That(nonGeneric, Has.Count.EqualTo(2));
        Assert.That(generic, Has.Count.EqualTo(2));
    }

    [Test]
    public void RegisterModule_Has3Overloads()
    {
        var overloads = typeof(CsEvalEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "RegisterModule")
            .ToList();

        Assert.That(overloads, Has.Count.EqualTo(3));
    }

    // ----------------------------------------------------------------
    // No EvaluateAsync (deferred to Phase 19.3)
    // ----------------------------------------------------------------

    [Test]
    public void EvaluateAsync_DoesNotExist()
    {
        var asyncMethods = typeof(CsEvalEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "EvaluateAsync")
            .ToList();

        Assert.That(asyncMethods, Is.Empty);
    }

    // ----------------------------------------------------------------
    // CsEvalExpression surface
    // ----------------------------------------------------------------

    [Test]
    public void CsEvalExpression_Ast_IsPublic()
    {
        var astProp = typeof(CsEvalExpression).GetProperty("Ast", BindingFlags.Public | BindingFlags.Instance);
        Assert.That(astProp, Is.Not.Null);
        Assert.That(astProp!.PropertyType, Is.EqualTo(typeof(Expr)));
    }

    [Test]
    public void CsEvalExpression_GetVariables_Exists()
    {
        var method = typeof(CsEvalExpression).GetMethod("GetVariables", BindingFlags.Public | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(IReadOnlyList<string>)));
    }

    [Test]
    public void CsEvalExpression_TryCompile_IsPublic()
    {
        var method = typeof(CsEvalExpression)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "TryCompile" && m.GetParameters().Length == 0)
            .SingleOrDefault();
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void CsEvalExpression_Compile_IsPublic()
    {
        var method = typeof(CsEvalExpression)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Compile" && m.GetParameters().Length == 0)
            .SingleOrDefault();
        Assert.That(method, Is.Not.Null);
    }

    // ----------------------------------------------------------------
    // CsEvalDiagnostic
    // ----------------------------------------------------------------

    [Test]
    public void CsEvalDiagnostic_TypeExists_WithExpectedProperties()
    {
        var type = typeof(CsEvalDiagnostic);
        Assert.That(type, Is.Not.Null);

        Assert.That(type.GetProperty("Line", BindingFlags.Public | BindingFlags.Instance), Is.Not.Null);
        Assert.That(type.GetProperty("Column", BindingFlags.Public | BindingFlags.Instance), Is.Not.Null);
        Assert.That(type.GetProperty("Severity", BindingFlags.Public | BindingFlags.Instance), Is.Not.Null);
        Assert.That(type.GetProperty("Message", BindingFlags.Public | BindingFlags.Instance), Is.Not.Null);
        Assert.That(type.GetProperty("Code", BindingFlags.Public | BindingFlags.Instance), Is.Not.Null);
    }

    // ----------------------------------------------------------------
    // CompiledExpression<T> and CompiledExpressionDelegate
    // ----------------------------------------------------------------

    [Test]
    public void CompiledExpression_GenericType_Exists_WithInvokeMethods()
    {
        var openType = typeof(CompiledExpression<>);
        Assert.That(openType, Is.Not.Null);
        Assert.That(openType.IsGenericTypeDefinition, Is.True);

        var closedType = typeof(CompiledExpression<int>);
        var invokeMethods = closedType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Invoke")
            .ToList();

        Assert.That(invokeMethods, Has.Count.EqualTo(2));
    }

    [Test]
    public void CompiledExpressionDelegate_TypeExists()
    {
        var type = typeof(CompiledExpressionDelegate);
        Assert.That(type, Is.Not.Null);
        Assert.That(typeof(Delegate).IsAssignableFrom(type), Is.True);
    }

    // ----------------------------------------------------------------
    // TryEvaluate and TryValidate presence
    // ----------------------------------------------------------------

    [Test]
    public void TryEvaluate_Has2Overloads()
    {
        var overloads = typeof(CsEvalEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "TryEvaluate")
            .ToList();

        Assert.That(overloads, Has.Count.EqualTo(2));
    }

    [Test]
    public void TryValidate_Exists()
    {
        var method = typeof(CsEvalEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "TryValidate")
            .SingleOrDefault();

        Assert.That(method, Is.Not.Null);
    }

    // ----------------------------------------------------------------
    // Compile / CompileToFunc presence
    // ----------------------------------------------------------------

    [Test]
    public void Compile_Methods_ExistOnEngine()
    {
        var methods = typeof(CsEvalEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Compile")
            .ToList();

        // Compile<T>(string) and Compile(string)
        Assert.That(methods, Has.Count.EqualTo(2));
    }

    [Test]
    public void CompileToFunc_Exists()
    {
        var method = typeof(CsEvalEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "CompileToFunc")
            .SingleOrDefault();

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.IsGenericMethod, Is.True);
    }

    // ----------------------------------------------------------------
    // Parameter order consistency
    // ----------------------------------------------------------------

    [Test]
    public void Evaluate_Overloads_HaveConsistentParameterOrder()
    {
        var overloads = typeof(CsEvalEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Evaluate")
            .ToList();

        foreach (var method in overloads)
        {
            var parameters = method.GetParameters();
            Assert.That(parameters.Length, Is.GreaterThanOrEqualTo(1),
                $"Evaluate must have at least 1 parameter");

            // First parameter is expression (string or CsEvalExpression)
            var firstParam = parameters[0];
            Assert.That(
                firstParam.ParameterType == typeof(string) ||
                firstParam.ParameterType == typeof(CsEvalExpression),
                Is.True,
                $"First parameter of {method} must be expression, was {firstParam.ParameterType.Name}");

            // Last parameter is CancellationToken
            var lastParam = parameters[^1];
            Assert.That(lastParam.ParameterType, Is.EqualTo(typeof(CancellationToken)),
                $"Last parameter of {method} must be CancellationToken");

            // variables comes before serviceProvider
            var variablesIdx = Array.FindIndex(parameters, p => p.Name == "variables");
            var serviceProviderIdx = Array.FindIndex(parameters, p => p.Name == "serviceProvider");
            var cancellationIdx = Array.FindIndex(parameters, p => p.Name == "cancellationToken");

            if (variablesIdx >= 0 && serviceProviderIdx >= 0)
            {
                Assert.That(variablesIdx, Is.LessThan(serviceProviderIdx),
                    $"variables must come before serviceProvider in {method}");
            }

            if (serviceProviderIdx >= 0 && cancellationIdx >= 0)
            {
                Assert.That(serviceProviderIdx, Is.LessThan(cancellationIdx),
                    $"serviceProvider must come before cancellationToken in {method}");
            }
        }
    }

    [Test]
    public void TryEvaluate_Overloads_HaveConsistentParameterOrder()
    {
        var overloads = typeof(CsEvalEngine)
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

            // variables before serviceProvider
            var variablesIdx = Array.FindIndex(parameters, p => p.Name == "variables");
            var serviceProviderIdx = Array.FindIndex(parameters, p => p.Name == "serviceProvider");
            if (variablesIdx >= 0 && serviceProviderIdx >= 0)
            {
                Assert.That(variablesIdx, Is.LessThan(serviceProviderIdx),
                    $"variables must come before serviceProvider in {method}");
            }
        }
    }

    // ----------------------------------------------------------------
    // DiagnosticSeverity enum
    // ----------------------------------------------------------------

    [Test]
    public void DiagnosticSeverity_IsPublicEnum()
    {
        var type = typeof(DiagnosticSeverity);
        Assert.That(type.IsEnum, Is.True);
        Assert.That(type.IsPublic, Is.True);
        Assert.That(Enum.GetNames(type), Does.Contain("Error"));
        Assert.That(Enum.GetNames(type), Does.Contain("Warning"));
    }
}
