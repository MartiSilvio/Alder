using System.Reflection;
using Alder.Runtime;
using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

[TestFixture]
public class TypeInferenceTests
{
    private static MethodInfo GetGenericMethod(Type type, string name, int genericArity, int paramCount)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == name
                        && m.IsGenericMethodDefinition
                        && m.GetGenericArguments().Length == genericArity
                        && m.GetParameters().Length == paramCount);
    }

    [Test]
    public void SingleTypeParam_BothSameType_InfersExactType()
    {
        // Enumerable.Max<T>(IEnumerable<T>) — single T from IEnumerable<int>
        var method = GetGenericMethod(typeof(Enumerable), "Max", 1, 1);
        var result = TypeInference.Infer(method, [typeof(List<int>)], null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.EqualTo(typeof(int)));
    }

    [Test]
    public void MultipleIndependentTypeParams_InfersBoth()
    {
        // Enumerable.Zip<TFirst, TSecond>(IEnumerable<TFirst>, IEnumerable<TSecond>)
        var method = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m is { Name: "Zip", IsGenericMethodDefinition: true }
                        && m.GetGenericArguments().Length == 2
                        && m.GetParameters().Length == 2);

        var result = TypeInference.Infer(method, [typeof(List<string>), typeof(int[])], null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.EqualTo(typeof(string)));
        Assert.That(result[1], Is.EqualTo(typeof(int)));
    }

    [Test]
    public void GenericInterfaceInference_ListToIEnumerable_InfersElementType()
    {
        var method = GetGenericMethod(typeof(Enumerable), "ToList", 1, 1);
        var result = TypeInference.Infer(method, [typeof(List<string>)], null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.EqualTo(typeof(string)));
    }

    [Test]
    public void CovariantLowerBound_IEnumerableOfDerived()
    {
        // IEnumerable<T> is covariant, so IEnumerable<string> produces lower bound T=string
        var method = GetGenericMethod(typeof(Enumerable), "Count", 1, 1);
        var result = TypeInference.Infer(method, [typeof(IEnumerable<string>)], null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.EqualTo(typeof(string)));
    }

    [Test]
    public void InvariantExactBound_ListOfInt()
    {
        // List<T> is invariant, so List<int> produces exact bound T=int
        // Use a method that takes List<T> directly — we can use Enumerable.ToArray which takes IEnumerable<T>
        // but List<int> implements IEnumerable<int>, so let's verify with ToList input
        var method = GetGenericMethod(typeof(Enumerable), "ToArray", 1, 1);
        var result = TypeInference.Infer(method, [typeof(List<int>)], null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.EqualTo(typeof(int)));
    }

    [Test]
    public void MultipleBoundsMerging_BothProduceSameType()
    {
        // Enumerable.Concat<T>(IEnumerable<T>, IEnumerable<T>) — both args contribute lower bounds
        var method = GetGenericMethod(typeof(Enumerable), "Concat", 1, 2);
        var result = TypeInference.Infer(method, [typeof(List<string>), typeof(string[])], null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.EqualTo(typeof(string)));
    }

    [Test]
    public void InferenceFailure_ConflictingExactBounds_ReturnsNull()
    {
        // Create a scenario with conflicting types — pass Dictionary<int, string> to a method expecting IEnumerable<T>
        // Dictionary<int, string> implements IEnumerable<KeyValuePair<int, string>>
        // If we also provide int[] which implements IEnumerable<int>, conflicting lower bounds
        var method = GetGenericMethod(typeof(Enumerable), "Concat", 1, 2);
        var result = TypeInference.Infer(method, [typeof(List<int>), typeof(List<string>)], null, null);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NestedGenerics_DictionaryWithListValue()
    {
        // Enumerable.ToList on a Dictionary<string, List<int>> yields T=KeyValuePair<string, List<int>>
        var method = GetGenericMethod(typeof(Enumerable), "ToList", 1, 1);
        var result = TypeInference.Infer(method, [typeof(Dictionary<string, List<int>>)], null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.EqualTo(typeof(KeyValuePair<string, List<int>>)));
    }

    [Test]
    public void FailsGracefully_NoMatchingInterface_ReturnsNull()
    {
        // Pass a type that doesn't implement the expected interface
        var method = GetGenericMethod(typeof(Enumerable), "ToList", 1, 1);
        var result = TypeInference.Infer(method, [typeof(int)], null, null);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NoArgs_ReturnsNull()
    {
        var method = GetGenericMethod(typeof(Enumerable), "ToList", 1, 1);
        var result = TypeInference.Infer(method, [], null, null);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullArgType_SkippedDuringInference()
    {
        var method = GetGenericMethod(typeof(Enumerable), "Concat", 1, 2);
        var result = TypeInference.Infer(method, [typeof(List<int>), null], null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.EqualTo(typeof(int)));
    }

    [Test]
    public void ArrayInference_StringArray()
    {
        var method = GetGenericMethod(typeof(Enumerable), "Count", 1, 1);
        var result = TypeInference.Infer(method, [typeof(string[])], null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.EqualTo(typeof(string)));
    }

    [TestFixture(CompilationMode.Interpreted)]
    [TestFixture(CompilationMode.Compiled)]
    public class LambdaOutputTypeInference(CompilationMode mode)
    {
        [Test]
        public void Select_WithLambda_InfersReturnType()
        {
            var result = TestEngineFactory.Create(mode).Evaluate("new[] { 1, 2, 3 }.Select(x => x.ToString()).ToList()");
            Assert.That(result, Is.TypeOf<List<string>>());
            var list = (List<string>)result!;
            Assert.That(list, Is.EqualTo(new List<string> { "1", "2", "3" }));
        }

        [Test]
        public void Where_WithLambda_InfersSourceType()
        {
            var result = TestEngineFactory.Create(mode).Evaluate("new[] { 1, 2, 3, 4 }.Where(x => x > 2).ToList()");
            Assert.That(result, Is.TypeOf<List<int>>());
            var list = (List<int>)result!;
            Assert.That(list, Is.EqualTo(new List<int> { 3, 4 }));
        }
    }
}
