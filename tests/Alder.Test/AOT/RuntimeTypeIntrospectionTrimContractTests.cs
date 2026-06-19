using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Alder;
using Alder.Runtime;
using Alder.Runtime.Introspection;

namespace Alder.Test.AOT;

[TestFixture]
[Category("AOT")]
public sealed class RuntimeTypeIntrospectionTrimContractTests
{
    [Test]
    public void GetInterfaces_RequiresInterfaceMetadataOnInputType()
    {
        var parameter = GetInterfacesMethod().GetParameters().Single();
        var attribute = parameter
            .GetCustomAttributes(typeof(DynamicallyAccessedMembersAttribute), inherit: false)
            .Cast<DynamicallyAccessedMembersAttribute>()
            .Single();

        Assert.That(attribute.MemberTypes, Is.EqualTo(DynamicallyAccessedMemberTypes.Interfaces));
    }

    [Test]
    public void GetInterfaces_DoesNotSuppressMissingInterfaceMetadataWarning()
    {
        var suppressions = GetInterfacesMethod()
            .GetCustomAttributes(typeof(UnconditionalSuppressMessageAttribute), inherit: false)
            .Cast<UnconditionalSuppressMessageAttribute>()
            .Select(attribute => attribute.CheckId);

        Assert.That(suppressions, Does.Not.Contain("IL2070"));
    }

    [Test]
    public void RuntimeIntrospection_KeepsSuppressedInterfaceFallbackNonPublic()
    {
        // The interface-member fallback suppresses IL2067 (its input type is not annotated for
        // interface metadata) and routes back through the DAM-annotated GetInterfaces. It must stay
        // private: only GetInterfaces, which roots interface metadata via its annotation, is part of
        // the trim-safe surface callers may reach.
        var fallback = typeof(RuntimeTypeIntrospection).GetMethod(
            "GetInterfacesForInterfaceMemberFallback",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(fallback, Is.Not.Null, "expected the interface-member fallback to exist");
        Assert.That(fallback!.IsPublic, Is.False, "the suppressed interface fallback must not be public");
    }

    [Test]
    public void ModuleRegistration_RootsInterfaceMetadata()
    {
        var registerTypeParameter = typeof(AlderOptions.ModuleBuilder)
            .GetMethods()
            .Single(method =>
                method.Name == nameof(AlderOptions.ModuleBuilder.Register) &&
                method.GetParameters() is
                [
                    { ParameterType: var first },
                    { ParameterType: var second },
                    { ParameterType: var third },
                    { ParameterType: var fourth }
                ] &&
                first == typeof(string) &&
                second == typeof(Type) &&
                third == typeof(bool) &&
                fourth == typeof(object))
            .GetParameters()[1];

        AssertDynamicallyAccessedMembers(registerTypeParameter, DynamicallyAccessedMemberTypes.Interfaces);
    }

    [Test]
    public void RegisteredModuleMetadata_RootsInterfaceMetadata()
    {
        var moduleTypeProperty = typeof(ModuleInfo).GetProperty(nameof(ModuleInfo.Type))!;
        AssertDynamicallyAccessedMembers(moduleTypeProperty, DynamicallyAccessedMemberTypes.Interfaces);

        var registeredType = typeof(AlderOptions).GetNestedType("RegisteredType", BindingFlags.NonPublic)!;
        var registeredTypeProperty = registeredType.GetProperty("Type")!;
        AssertDynamicallyAccessedMembers(registeredTypeProperty, DynamicallyAccessedMemberTypes.Interfaces);
    }

    private static MethodInfo GetInterfacesMethod() =>
        typeof(RuntimeTypeIntrospection).GetMethod(
            nameof(RuntimeTypeIntrospection.GetInterfaces),
            BindingFlags.Public | BindingFlags.Static)!;

    private static void AssertDynamicallyAccessedMembers(MemberInfo member, DynamicallyAccessedMemberTypes expected)
    {
        var attribute = member
            .GetCustomAttributes(typeof(DynamicallyAccessedMembersAttribute), inherit: false)
            .Cast<DynamicallyAccessedMembersAttribute>()
            .Single();

        Assert.That(attribute.MemberTypes & expected, Is.EqualTo(expected));
    }

    private static void AssertDynamicallyAccessedMembers(ParameterInfo parameter, DynamicallyAccessedMemberTypes expected)
    {
        var attribute = parameter
            .GetCustomAttributes(typeof(DynamicallyAccessedMembersAttribute), inherit: false)
            .Cast<DynamicallyAccessedMembersAttribute>()
            .Single();

        Assert.That(attribute.MemberTypes & expected, Is.EqualTo(expected));
    }
}
