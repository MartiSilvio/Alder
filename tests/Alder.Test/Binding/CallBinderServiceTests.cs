using Alder.Binding.Plans;
using Alder.Binding.Services;
using Alder.Runtime;

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

        var plan = binder.BindStaticCall(typeof(Math), "Max", [1, 2L], isCaseSensitive: true);
        var parameters = plan.SelectedMethod.GetParameters();

        Assert.That(parameters.Length, Is.EqualTo(2));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(long)));
        Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(long)));

        var runtimeResult = MethodInvoker.InvokeCall(
            new StaticMethodRef(typeof(Math), "Max"),
            [1, 2L],
            context,
            AlderOptions.Default,
            ct: CancellationToken.None);

        Assert.That(runtimeResult, Is.EqualTo(2L));
        Assert.That(plan.SelectedMethod.ReturnType, Is.EqualTo(runtimeResult!.GetType()));
    }

    [Test]
    public void MemberBinder_ShouldReturnMethodGroupAndIndexPlan()
    {
        var memberBinder = new MemberBinderService(new TypeMetadataProvider());

        var methodGroup = memberBinder.BindMemberRead(typeof(string), nameof(string.Contains), isStatic: false, isCaseSensitive: true);
        Assert.That(methodGroup.IsMethodGroup, Is.True);

        var indexPlan = memberBinder.BindIndexRead(typeof(List<int>), typeof(int));
        Assert.That(indexPlan.IsDirectCollectionAccess, Is.True);
    }

    [Test]
    public void CallBinder_ShouldPlanOptionalArgument_WithDefaultBinding()
    {
        var engine = new AlderEngine();
        var context = engine.GetContextForCompiled();
        var binder = new CallBinderService(context);

        var plan = binder.BindInstanceCall(
            typeof(InvocationTarget),
            nameof(InvocationTarget.WithOptional),
            [typeof(int)],
            isCaseSensitive: true);

        Assert.That(plan.ParameterBindings.Length, Is.EqualTo(2));
        Assert.That(plan.ParameterBindings[0].Kind, Is.EqualTo(BoundParameterBindingKind.Argument));
        Assert.That(plan.ParameterBindings[0].SourceArgumentIndex, Is.EqualTo(0));
        Assert.That(plan.ParameterBindings[1].Kind, Is.EqualTo(BoundParameterBindingKind.DefaultValue));
    }

    [Test]
    public void CallBinder_ShouldPlanParamsExpandedBinding()
    {
        var engine = new AlderEngine();
        var context = engine.GetContextForCompiled();
        var binder = new CallBinderService(context);

        var plan = binder.BindInstanceCall(
            typeof(InvocationTarget),
            nameof(InvocationTarget.Sum),
            [typeof(int), typeof(int), typeof(int), typeof(int)],
            isCaseSensitive: true);

        Assert.That(plan.ParameterBindings.Length, Is.EqualTo(1));
        Assert.That(plan.ParameterBindings[0].Kind, Is.EqualTo(BoundParameterBindingKind.ParamsArray));
        Assert.That(plan.ParameterBindings[0].SourceArgumentIndex, Is.EqualTo(0));
        Assert.That(plan.ParameterBindings[0].SourceArgumentCount, Is.EqualTo(4));
    }

    [Test]
    public void CallBinder_ShouldDeferRuntimeResolution_ForObjectTypedArgumentsWithOverloads()
    {
        var engine = new AlderEngine();
        var context = engine.GetContextForCompiled();
        var binder = new CallBinderService(context);

        var ex = Assert.Throws<AlderException>(() =>
            binder.BindStaticCall(
                typeof(OverloadTarget),
                nameof(OverloadTarget.Pick),
                [typeof(object)],
                isCaseSensitive: true));

        Assert.That(ex!.ErrorCode, Is.EqualTo(Alder.Diagnostics.DiagnosticCode.ALDR0003));
        Assert.That(ex.Message, Does.Contain("runtime overload resolution"));
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
