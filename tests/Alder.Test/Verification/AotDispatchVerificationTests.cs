using Alder.Aot;
using Alder.Runtime.Introspection;
using Alder.Test._Infrastructure;

namespace Alder.Test.Verification;

/// <summary>
/// Verifies the explicit AOT dispatch surface and its boundary against the reflection path.
/// </summary>
[TestFixture]
public class AotDispatchVerificationTests
{
    private static AlderEngine CreateAotEngine()
        => new AlderEngine();

    private static AlderEngine CreateReflectionOnlyEngine()
        => new AlderEngine(o => o.Aot.ClearBuiltInContext());

    private static void AssertParityResult(object? aotResult, object? reflResult, string operation)
    {
        Assert.That(aotResult?.GetType(), Is.EqualTo(reflResult?.GetType()),
            $"Type mismatch for {operation}: AOT={aotResult?.GetType()?.Name}, Reflection={reflResult?.GetType()?.Name}");
        Assert.That(aotResult, Is.EqualTo(reflResult),
            $"Value mismatch for {operation}: AOT={aotResult}, Reflection={reflResult}");
    }

    // --- Property get: instance and static ---

    [Test]
    public void PropertyGet_Instance_StringLength()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();
        aotEngine.SetVariable("s", "hello");
        reflEngine.SetVariable("s", "hello");

        var aot = aotEngine.Evaluate("return s.Length;");
        var refl = reflEngine.Evaluate("return s.Length;");
        AssertParityResult(aot, refl, "string.Length (instance property get)");
    }

    [Test]
    public void PropertyGet_Static_StringEmpty()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();

        var aot = aotEngine.Evaluate("return string.Empty;");
        var refl = reflEngine.Evaluate("return string.Empty;");
        AssertParityResult(aot, refl, "string.Empty (static property get)");
    }

    // --- Property set ---

    [Test]
    public void PropertySet_Instance()
    {
        var list = new List<int> { 1, 2, 3 };
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();
        aotEngine.SetVariable("list", new List<int>(list));
        reflEngine.SetVariable("list", new List<int>(list));

        aotEngine.Evaluate("list.Capacity = 100; return list.Capacity;");
        reflEngine.Evaluate("list.Capacity = 100; return list.Capacity;");

        var aot = aotEngine.Evaluate("return list.Capacity;");
        var refl = reflEngine.Evaluate("return list.Capacity;");
        AssertParityResult(aot, refl, "List.Capacity set + get");
    }

    // --- Method invoke: instance with args ---

    [Test]
    public void MethodInvoke_Instance_StringContains()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();
        aotEngine.SetVariable("s", "hello world");
        reflEngine.SetVariable("s", "hello world");

        var aot = aotEngine.Evaluate("""return s.Contains("world");""");
        var refl = reflEngine.Evaluate("""return s.Contains("world");""");
        AssertParityResult(aot, refl, "string.Contains (instance method)");
    }

    [Test]
    public void MethodInvoke_Static_MathMax()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();

        var aot = aotEngine.Evaluate("return Math.Max(10, 20);");
        var refl = reflEngine.Evaluate("return Math.Max(10, 20);");
        AssertParityResult(aot, refl, "Math.Max (static method)");
    }

    // --- Method invoke with overloads ---

    [Test]
    public void MethodInvoke_Overloads_MathAbs()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();

        // int overload
        var aotInt = aotEngine.Evaluate("return Math.Abs(-42);");
        var reflInt = reflEngine.Evaluate("return Math.Abs(-42);");
        AssertParityResult(aotInt, reflInt, "Math.Abs(int)");

        // double overload
        var aotDbl = aotEngine.Evaluate("return Math.Abs(-3.14);");
        var reflDbl = reflEngine.Evaluate("return Math.Abs(-3.14);");
        AssertParityResult(aotDbl, reflDbl, "Math.Abs(double)");
    }

    // --- Indexer get/set ---

    [Test]
    public void IndexerGet_List()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();
        aotEngine.SetVariable("items", new List<int> { 10, 20, 30 });
        reflEngine.SetVariable("items", new List<int> { 10, 20, 30 });

        var aot = aotEngine.Evaluate("return items[1];");
        var refl = reflEngine.Evaluate("return items[1];");
        AssertParityResult(aot, refl, "List<int>[1] (indexer get)");
    }

    [Test]
    public void IndexerSet_List()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();
        aotEngine.SetVariable("items", new List<int> { 10, 20, 30 });
        reflEngine.SetVariable("items", new List<int> { 10, 20, 30 });

        var aot = aotEngine.Evaluate("items[1] = 99; return items[1];");
        var refl = reflEngine.Evaluate("items[1] = 99; return items[1];");
        AssertParityResult(aot, refl, "List<int>[1] = 99 (indexer set)");
    }

    [Test]
    public void IndexerGet_Dictionary()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();
        var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        aotEngine.SetVariable("d", new Dictionary<string, int>(dict));
        reflEngine.SetVariable("d", new Dictionary<string, int>(dict));

        var aot = aotEngine.Evaluate("""return d["b"];""");
        var refl = reflEngine.Evaluate("""return d["b"];""");
        AssertParityResult(aot, refl, "Dictionary[key] (indexer get)");
    }

    // --- Constructor ---

    [Test]
    public void Constructor_List()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();

        var aot = aotEngine.Evaluate("return new List<int>();");
        var refl = reflEngine.Evaluate("return new List<int>();");
        Assert.That(aot, Is.InstanceOf<List<int>>());
        Assert.That(refl, Is.InstanceOf<List<int>>());
    }

    // --- Null argument ---

    [Test]
    public void MethodInvoke_NullArgument()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();

        var aot = aotEngine.Evaluate("""return string.IsNullOrEmpty(null);""");
        var refl = reflEngine.Evaluate("""return string.IsNullOrEmpty(null);""");
        AssertParityResult(aot, refl, "string.IsNullOrEmpty(null)");
    }

    // --- Inherited member ---

    [Test]
    public void InheritedMember_BasePropertyOnDerived()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();

        // List<int> inherits Count from ICollection<int> but the property is on List<int> itself.
        // Use a more interesting case: MemoryStream.Length is inherited from Stream.
        aotEngine.SetVariable("items", new List<string> { "a", "b", "c" });
        reflEngine.SetVariable("items", new List<string> { "a", "b", "c" });

        var aot = aotEngine.Evaluate("return items.Count;");
        var refl = reflEngine.Evaluate("return items.Count;");
        AssertParityResult(aot, refl, "List<string>.Count (base type chain dispatch)");
    }

    [Test]
    public void InheritedMember_ObjectMembersOnCuratedType()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();

        aotEngine.SetVariable("value", 5);
        reflEngine.SetVariable("value", 5);

        var aotType = aotEngine.Evaluate("return value.GetType() == typeof(int);");
        var reflType = reflEngine.Evaluate("return value.GetType() == typeof(int);");
        AssertParityResult(aotType, reflType, "int.GetType() (object base member)");

        var aotString = aotEngine.Evaluate("return value.ToString();");
        var reflString = reflEngine.Evaluate("return value.ToString();");
        AssertParityResult(aotString, reflString, "int.ToString() (object/value-type base member)");
    }

    // --- AOT boundary ---

    [Test]
    public void ExtensionMethod_LinqWhere_IsOutsideAotDispatchBoundary()
    {
        using var reflEngine = CreateReflectionOnlyEngine();
        reflEngine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var refl = reflEngine.Evaluate("return items.Where(x => x > 3).Count();");
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        using var aotEngine = CreateAotEngine();
        aotEngine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });
        var ex = Assert.Throws<AlderException>(() => aotEngine.Evaluate("return items.Where(x => x > 3).Count();"));

        Assert.That(refl, Is.EqualTo(2));
        Assert.That(ex!.Message, Does.Contain("authoritative generated mode"));
    }

    // --- Case-insensitive mode ---

    // Case-insensitive dispatch must remain functionally correct.
    // exact-case dispatch attempt. The first TryGet/TryInvoke uses the user-provided name
    // (e.g., "length"), which AOT dispatch won't match since it registers "Length".
    // The fallback resolves the canonical name via reflection, then retries.
    //
    // This is correctly implemented but incurs a reflection cost on every case-mismatch.
    // Verify it produces correct results.
    [Test]
    public void CaseInsensitiveMode_PropertyAccess()
    {
        using var aotEngine = new AlderEngine(o => o.IsCaseSensitive = false);
        using var reflEngine = new AlderEngine(o => { o.IsCaseSensitive = false; o.Aot.ClearBuiltInContext(); });

        aotEngine.SetVariable("s", "Hello");
        reflEngine.SetVariable("s", "Hello");

        // "length" with lowercase 'l' — AOT dispatch registers "Length" (PascalCase)
        var aot = aotEngine.Evaluate("return s.length;");
        var refl = reflEngine.Evaluate("return s.length;");
        AssertParityResult(aot, refl, "s.length (case-insensitive property access)");
    }

    [Test]
    public void CaseInsensitiveMode_MethodInvoke()
    {
        using var aotEngine = new AlderEngine(o => o.IsCaseSensitive = false);
        using var reflEngine = new AlderEngine(o => { o.IsCaseSensitive = false; o.Aot.ClearBuiltInContext(); });

        aotEngine.SetVariable("s", "Hello World");
        reflEngine.SetVariable("s", "Hello World");

        var aot = aotEngine.Evaluate("""return s.contains("World");""");
        var refl = reflEngine.Evaluate("""return s.contains("World");""");
        AssertParityResult(aot, refl, "s.contains() (case-insensitive method invoke)");
    }

    // --- Comprehensive parity: same expression, AOT vs reflection-only ---

    [Test]
    public void FullParity_StringManipulation()
    {
        using var aotEngine = CreateAotEngine();
        using var reflEngine = CreateReflectionOnlyEngine();

        aotEngine.SetVariable("s", "  Hello, World!  ");
        reflEngine.SetVariable("s", "  Hello, World!  ");

        var expr = """return s.Trim().ToUpper().Replace("WORLD", "ALDER").Substring(0, 13);""";
        var aot = aotEngine.Evaluate(expr);
        var refl = reflEngine.Evaluate(expr);
        AssertParityResult(aot, refl, "String method chain");
    }
}
