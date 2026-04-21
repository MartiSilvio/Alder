using System.Collections.Immutable;
using Alder.Runtime;

namespace Alder.Test.Runtime;

[TestFixture]
public class TypeAssemblyIndexTests
{
    [Test]
    public void ResolveInNamespace_FindsGenericTypeByMetadataName()
    {
        var index = CreateIndex(StringComparer.Ordinal);

        var found = index.TryResolveInNamespace("System.Collections.Generic", "List`1", out var type);

        Assert.That(found, Is.True);
        Assert.That(type, Is.EqualTo(typeof(List<>)));
    }

    [Test]
    public void ResolveImplicitImport_FindsFriendlyGenericName()
    {
        var index = CreateIndex(StringComparer.Ordinal);

        var cachedPath = index.TryResolveImplicitImport("List", out var cachedType);

        Assert.That(cachedPath, Is.True);
        Assert.That(cachedType, Is.EqualTo(typeof(List<>)));
    }

    [Test]
    public void ResolveFullyQualifiedName_FindsNestedType()
    {
        var index = CreateIndex(StringComparer.Ordinal);

        var type = index.TryResolveFullyQualifiedName("System.Environment.SpecialFolder");

        Assert.That(type, Is.EqualTo(typeof(Environment.SpecialFolder)));
    }

    [Test]
    public void IsNamespaceOrPrefix_RecognizesIntermediateNamespaceSegments()
    {
        var index = CreateIndex(StringComparer.Ordinal);

        Assert.That(index.IsNamespaceOrPrefix("System.Collections"), Is.True);
        Assert.That(index.IsNamespaceOrPrefix("System.Collections.Generic"), Is.True);
        Assert.That(index.IsNamespaceOrPrefix("System.Collections.Generic.NonExistent"), Is.False);
    }

    [Test]
    public void CaseInsensitiveIndex_ResolvesLowerCaseNames()
    {
        var index = CreateIndex(StringComparer.OrdinalIgnoreCase);

        var inNamespace = index.TryResolveInNamespace("system.collections.generic", "list`1", out var namespacedType);
        var fullyQualified = index.TryResolveFullyQualifiedName("system.environment.specialfolder");
        var implicitFound = index.TryResolveImplicitImport("list", out var implicitType);

        Assert.That(inNamespace, Is.True);
        Assert.That(namespacedType, Is.EqualTo(typeof(List<>)));
        Assert.That(fullyQualified, Is.EqualTo(typeof(Environment.SpecialFolder)));
        Assert.That(implicitFound, Is.True);
        Assert.That(implicitType, Is.EqualTo(typeof(List<>)));
    }

    private static TypeAssemblyIndex CreateIndex(StringComparer comparer)
    {
        var assemblies = ImmutableArray.Create(
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(Stack<>).Assembly,
            typeof(Environment).Assembly,
            typeof(Uri).Assembly,
            typeof(System.Text.RegularExpressions.Regex).Assembly);

        return new TypeAssemblyIndex(assemblies, implicitBclImports: true, comparer);
    }
}
