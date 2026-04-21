using Alder.Binding.Services;
using Alder.Runtime;
using Alder.Runtime.OverloadResolution;

namespace Alder.Test.Binding;

[TestFixture]
public sealed class CallBinderServiceTests
{
    [Test]
    public void CallBinder_ShouldChooseSameOverload_AsRuntimeInvoker()
    {
        var engine = new AlderEngine();
        var context = engine.GetContextForCompiled();
        var binder = new CallBinderService(context);

        Assert.That(
            binder.TryBindCall(
                typeof(Math),
                "Max",
                ArgumentDescriptor.FromTypes([typeof(int), typeof(long)]),
                isStaticCall: true,
                isCaseSensitive: true,
                typeArgs: null,
                out var plan),
            Is.True);
        Assert.That(plan, Is.Not.Null);
        var parameters = plan!.SelectedMethod.GetParameters();

        Assert.That(parameters.Length, Is.EqualTo(2));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(long)));
        Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(long)));

        var runtimeResult = MethodInvoker.InvokeCall(
            new MethodRef(typeof(Math), "Max"),
            [1, 2L],
            context,
            ct: CancellationToken.None);

        Assert.That(runtimeResult, Is.EqualTo(2L));
        Assert.That(plan.SelectedMethod.ReturnType, Is.EqualTo(runtimeResult!.GetType()));
    }

    [Test]
    public void MemberBinder_ShouldReturnMethodGroupAndIndexPlan()
    {
        var memberBinder = new MemberBinderService(new TypeMetadataProvider());

        var methodGroupResult = memberBinder.BindMemberRead(typeof(string), nameof(string.Contains), isStatic: false, isCaseSensitive: true, out _);
        Assert.That(methodGroupResult, Is.EqualTo(MemberBindResult.MethodGroup));

        var indexPlan = memberBinder.BindIndexRead(typeof(List<int>), typeof(int));
        Assert.That(indexPlan.IsDirectCollectionAccess, Is.True);
    }

    [Test]
    public void CallBinder_ShouldPlanOptionalArgument_WithDefaultBinding()
    {
        var engine = new AlderEngine();
        var context = engine.GetContextForCompiled();
        var binder = new CallBinderService(context);

        Assert.That(
            binder.TryBindCall(
                typeof(InvocationTarget),
                nameof(InvocationTarget.WithOptional),
                ArgumentDescriptor.FromTypes([typeof(int)]),
                isStaticCall: false,
                isCaseSensitive: true,
                typeArgs: null,
                out var plan),
            Is.True);

        var sources = plan!.Resolution.ArgMap.Sources;
        Assert.That(sources.Length, Is.EqualTo(2));
        Assert.That(sources[0].Kind, Is.EqualTo(ParameterSourceKind.Argument));
        Assert.That(sources[0].ArgumentIndex, Is.EqualTo(0));
        Assert.That(sources[1].Kind, Is.EqualTo(ParameterSourceKind.Default));
    }

    [Test]
    public void CallBinder_ShouldPlanParamsExpandedBinding()
    {
        var engine = new AlderEngine();
        var context = engine.GetContextForCompiled();
        var binder = new CallBinderService(context);

        Assert.That(
            binder.TryBindCall(
                typeof(InvocationTarget),
                nameof(InvocationTarget.Sum),
                ArgumentDescriptor.FromTypes([typeof(int), typeof(int), typeof(int), typeof(int)]),
                isStaticCall: false,
                isCaseSensitive: true,
                typeArgs: null,
                out var plan),
            Is.True);

        var sources = plan!.Resolution.ArgMap.Sources;
        Assert.That(sources.Length, Is.EqualTo(1));
        Assert.That(sources[0].Kind, Is.EqualTo(ParameterSourceKind.ParamsRange));
        Assert.That(sources[0].ParamsStartIndex, Is.EqualTo(0));
        Assert.That(sources[0].ParamsCount, Is.EqualTo(4));
    }

    [Test]
    public void CallBinder_ShouldDeferRuntimeResolution_ForObjectTypedArgumentsWithOverloads()
    {
        var engine = new AlderEngine();
        var context = engine.GetContextForCompiled();
        var binder = new CallBinderService(context);

        var result = binder.TryBindCall(
            typeof(OverloadTarget),
            nameof(OverloadTarget.Pick),
            ArgumentDescriptor.FromTypes([typeof(object)]),
            isStaticCall: true,
            isCaseSensitive: true,
            typeArgs: null,
            out _);

        Assert.That(result, Is.False);
    }

    [Test]
    public void ExtensionCallBinder_ShouldResolveLambdaDescriptorThroughUnifiedPath()
    {
        var engine = new AlderEngine();
        var context = engine.GetContextForCompiled();
        var binder = new CallBinderService(context);

        var descriptors = new[]
        {
            ArgumentDescriptor.ForTest(ArgumentKind.Lambda, null, null, lambdaArity: 1)
        };

        var result = binder.TryBindExtensionCall(
            typeof(int[]),
            "Where",
            descriptors,
            isCaseSensitive: true,
            typeArgs: null,
            out var plan);

        Assert.That(result, Is.True);
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan!.SelectedMethod.Name, Is.EqualTo("Where"));
        Assert.That(plan.IsExtensionCall, Is.True);
    }

    private sealed class InvocationTarget
    {
        public int Sum(params int[] values) => values.Sum();
        public int WithOptional(int value, int extra = 10) => value + extra;
    }

    private static class OverloadTarget
    {
        public static string Pick(object value) => value.ToString() ?? string.Empty;
        public static string Pick(IEnumerable<string> values) => string.Join(",", values);
    }
}
