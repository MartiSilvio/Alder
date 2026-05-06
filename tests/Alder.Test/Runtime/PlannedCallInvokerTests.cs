using Alder.Binding.Services;
using Alder.Runtime;
using Alder.Runtime.OverloadResolution;

namespace Alder.Test.Runtime;

[TestFixture]
public sealed class PlannedCallInvokerTests
{
    [Test]
    public void PreparedCall_OptionalDefault_AppliesDefaultValue()
    {
        var context = new AlderContext(AlderConfig.Empty);
        var binder = new CallBinderService(context);
        binder.TryBindCall(
            typeof(PlannedInvocationTarget),
            nameof(PlannedInvocationTarget.WithOptional),
            ArgumentDescriptor.FromTypes([typeof(int)]),
            isStaticCall: false,
            isCaseSensitive: true,
            typeArgs: null,
            out var plan);

        var resolved = plan!.Resolution;
        Assert.That(ArgumentPreparer.IsDirectMapping(resolved, [7], MethodDispatchCache.GetParameters(resolved.Method)), Is.False);

        var target = new PlannedInvocationTarget();
        var parameters = MethodDispatchCache.GetParameters(resolved.Method);
        var prepared = ArgumentPreparer.Prepare(resolved, [7], parameters, CancellationToken.None);
        var result = MethodInvoker.InvokeMethodCore(resolved.Method, target, prepared);

        Assert.That(result, Is.EqualTo(17));
    }

    [Test]
    public void PreparedCall_ParamsExpansion_PacksTrailingArguments()
    {
        var context = new AlderContext(AlderConfig.Empty);
        var binder = new CallBinderService(context);
        binder.TryBindCall(
            typeof(PlannedInvocationTarget),
            nameof(PlannedInvocationTarget.Sum),
            ArgumentDescriptor.FromTypes([typeof(int), typeof(int), typeof(int), typeof(int)]),
            isStaticCall: false,
            isCaseSensitive: true,
            typeArgs: null,
            out var plan);

        var resolved = plan!.Resolution;
        object?[] args = [1, 2, 3, 4];
        Assert.That(ArgumentPreparer.IsDirectMapping(resolved, args, MethodDispatchCache.GetParameters(resolved.Method)), Is.False);

        var target = new PlannedInvocationTarget();
        var parameters = MethodDispatchCache.GetParameters(resolved.Method);
        var prepared = ArgumentPreparer.Prepare(resolved, args, parameters, CancellationToken.None);
        var result = MethodInvoker.InvokeMethodCore(resolved.Method, target, prepared);

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void PreparedCall_DirectIdentityMapping_UsesDirectPath()
    {
        var context = new AlderContext(AlderConfig.Empty);
        var binder = new CallBinderService(context);
        binder.TryBindCall(
            typeof(PlannedInvocationTarget),
            nameof(PlannedInvocationTarget.Echo),
            ArgumentDescriptor.FromTypes([typeof(int)]),
            isStaticCall: false,
            isCaseSensitive: true,
            typeArgs: null,
            out var plan);

        var resolved = plan!.Resolution;
        object?[] args = [7];
        Assert.That(ArgumentPreparer.IsDirectMapping(resolved, args, MethodDispatchCache.GetParameters(resolved.Method)), Is.True);

        var target = new PlannedInvocationTarget();
        var parameters = MethodDispatchCache.GetParameters(resolved.Method);
        var prepared = ArgumentPreparer.Prepare(resolved, args, parameters, CancellationToken.None);
        var result = MethodInvoker.InvokeMethodCore(resolved.Method, target, prepared);

        Assert.That(result, Is.EqualTo(7));
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
