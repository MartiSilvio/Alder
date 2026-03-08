using System.Reflection;

namespace CsEval.Test.Runtime;

[TestFixture]
public class MethodResolverTests
{
    [Test]
    public void TryResolveMethod_FromCachedCandidates_FindsExactStaticOverload()
    {
        var methods = typeof(Math).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(Math.Max))
            .ToArray();

        var resolved = CsEval.Runtime.MethodResolver.TryResolveMethod(
            methods,
            [typeof(int), typeof(int)]);

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.GetParameters().Select(p => p.ParameterType), Is.EqualTo(new[] { typeof(int), typeof(int) }));
    }

    [Test]
    public void TryResolveMethod_FromCachedCandidates_UsesImplicitNumericConversion()
    {
        var methods = typeof(Math).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(Math.Max))
            .ToArray();

        var resolved = CsEval.Runtime.MethodResolver.TryResolveMethod(
            methods,
            [typeof(int), typeof(long)]);

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.GetParameters().Select(p => p.ParameterType), Is.EqualTo(new[] { typeof(long), typeof(long) }));
    }
}
