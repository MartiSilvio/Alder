using System.Reflection;

namespace Alder.Test.Core;

/// <summary>
/// Stable public contract checks for a few core surface types.
/// </summary>
[TestFixture]
public class ApiSurfaceTests
{
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
    public void DiagnosticSeverity_IsPublicEnum()
    {
        var type = typeof(DiagnosticSeverity);
        Assert.That(type.IsEnum, Is.True);
        Assert.That(type.IsPublic, Is.True);
        Assert.That(Enum.GetNames(type), Does.Contain("Error"));
        Assert.That(Enum.GetNames(type), Does.Contain("Warning"));
    }
}
