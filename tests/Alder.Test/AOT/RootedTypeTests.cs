using System.Diagnostics.CodeAnalysis;
using Alder.Aot;

namespace Alder.Test.AOT;

[TestFixture]
[Category("AOT")]
public sealed class RootedTypeTests
{
    [Test]
    public void RootedType_CarriesPublicConstructorAccessAnnotation()
    {
        var property = typeof(RootedType).GetProperty(nameof(RootedType.Type))!;
        var attribute = property.GetCustomAttributes(typeof(DynamicallyAccessedMembersAttribute), inherit: false)
            .Cast<DynamicallyAccessedMembersAttribute>()
            .Single();

        Assert.That(attribute.MemberTypes, Is.EqualTo(DynamicallyAccessedMemberTypes.PublicConstructors));
    }

    [Test]
    public void RootedType_EqualityUsesUnderlyingType()
    {
        var first = new RootedType(typeof(Func<int, int>));
        var second = new RootedType(typeof(Func<int, int>));

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first == second, Is.True);
        Assert.That(first != second, Is.False);
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }
}
