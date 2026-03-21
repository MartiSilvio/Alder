using Alder.Binding.Services;
using Alder.Runtime;

namespace Alder.Test.Runtime;

[TestFixture]
public sealed class PlannedCallInvokerTests
{
    [Test]
    public void InvokePlannedMethod_OptionalDefault_AppliesDefaultValue()
    {
        var context = new AlderContext(AlderConfig.Empty);
        var binder = new CallBinderService(context);
        var plan = binder.BindInstanceCall(
            typeof(PlannedInvocationTarget),
            nameof(PlannedInvocationTarget.WithOptional),
            [typeof(int)],
            isCaseSensitive: true);

        Assert.That(plan.IsDirectArgumentMapping, Is.False);

        var target = new PlannedInvocationTarget();
        var result = MethodInvoker.InvokePlannedMethod(plan, target, [7], CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(17));
    }

    [Test]
    public void InvokePlannedMethod_ParamsExpansion_PacksTrailingArguments()
    {
        var context = new AlderContext(AlderConfig.Empty);
        var binder = new CallBinderService(context);
        var plan = binder.BindInstanceCall(
            typeof(PlannedInvocationTarget),
            nameof(PlannedInvocationTarget.Sum),
            [typeof(int), typeof(int), typeof(int), typeof(int)],
            isCaseSensitive: true);

        Assert.That(plan.IsDirectArgumentMapping, Is.False);

        var target = new PlannedInvocationTarget();
        var result = MethodInvoker.InvokePlannedMethod(plan, target, [1, 2, 3, 4], CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(10));
    }

    [Test]
    public void InvokePlannedMethod_DirectIdentityMapping_UsesDirectPath()
    {
        var context = new AlderContext(AlderConfig.Empty);
        var binder = new CallBinderService(context);
        var plan = binder.BindInstanceCall(
            typeof(PlannedInvocationTarget),
            nameof(PlannedInvocationTarget.Echo),
            [typeof(int)],
            isCaseSensitive: true);

        Assert.That(plan.IsDirectArgumentMapping, Is.True);

        var target = new PlannedInvocationTarget();
        var result = MethodInvoker.InvokePlannedMethod(plan, target, [7], CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(7));
    }

    private sealed class PlannedInvocationTarget
    {
        public int Sum(params int[] values)
        {
            var total = 0;
            for (var i = 0; i < values.Length; i++)
                total += values[i];
            return total;
        }

        public int WithOptional(int value, int extra = 10) => value + extra;

        public int Echo(int value) => value;
    }
}
