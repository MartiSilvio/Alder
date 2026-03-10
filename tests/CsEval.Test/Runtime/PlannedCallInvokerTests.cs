using CsEval.Binding.Services;
using CsEval.Runtime;

namespace CsEval.Test.Runtime;

[TestFixture]
public sealed class PlannedCallInvokerTests
{
    [Test]
    public void InvokePlannedMethod_OptionalDefault_AppliesDefaultValue()
    {
        var context = new CsEvalContext(CsEvalConfig.Empty);
        var binder = new CallBinderService(context);
        var plan = binder.BindInstanceCall(
            typeof(PlannedInvocationTarget),
            nameof(PlannedInvocationTarget.WithOptional),
            [typeof(int)],
            isCaseSensitive: true);

        var target = new PlannedInvocationTarget();
        var result = MethodInvoker.InvokePlannedMethod(plan, target, [7], CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(17));
    }

    [Test]
    public void InvokePlannedMethod_ParamsExpansion_PacksTrailingArguments()
    {
        var context = new CsEvalContext(CsEvalConfig.Empty);
        var binder = new CallBinderService(context);
        var plan = binder.BindInstanceCall(
            typeof(PlannedInvocationTarget),
            nameof(PlannedInvocationTarget.Sum),
            [typeof(int), typeof(int), typeof(int), typeof(int)],
            isCaseSensitive: true);

        var target = new PlannedInvocationTarget();
        var result = MethodInvoker.InvokePlannedMethod(plan, target, [1, 2, 3, 4], CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(10));
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
    }
}
