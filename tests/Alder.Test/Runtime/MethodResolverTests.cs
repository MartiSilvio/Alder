using System.Reflection;
using Alder.Runtime;

namespace Alder.Test.Runtime;

[TestFixture]
public class OverloadResolverTests
{
    [Test]
    public void Resolve_FindsExactStaticOverload()
    {
        var methods = typeof(Math).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(Math.Max))
            .ToArray();

        var descriptors = ArgumentDescriptor.FromTypes([typeof(int), typeof(int)]);
        var found = OverloadResolver.TryResolve(methods, descriptors, context: null, out var resolved, out _);

        Assert.That(found, Is.True);
        Assert.That(resolved.Method.GetParameters().Select(p => p.ParameterType),
            Is.EqualTo(new[] { typeof(int), typeof(int) }));
    }

    [Test]
    public void Resolve_UsesImplicitNumericConversion()
    {
        var methods = typeof(Math).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(Math.Max))
            .ToArray();

        var descriptors = ArgumentDescriptor.FromTypes([typeof(int), typeof(long)]);
        var found = OverloadResolver.TryResolve(methods, descriptors, context: null, out var resolved, out _);

        Assert.That(found, Is.True);
        Assert.That(resolved.Method.GetParameters().Select(p => p.ParameterType),
            Is.EqualTo(new[] { typeof(long), typeof(long) }));
    }
}
