using CsEval.Binding.Services;
using CsEval.Runtime;

namespace CsEval.Test;

[TestFixture]
public sealed class CallBinderServiceTests
{
    [Test]
    public void CallBinder_ShouldChooseSameOverload_AsRuntimeInvoker()
    {
        var engine = new CsEvalEngine();
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
            CsEvalOptions.Default,
            CancellationToken.None);

        Assert.That(runtimeResult, Is.EqualTo(2L));
        Assert.That(plan.SelectedMethod.ReturnType, Is.EqualTo(runtimeResult!.GetType()));
    }

    [Test]
    public void MemberBinder_ShouldReturnMethodGroupAndIndexPlan()
    {
        var memberBinder = new MemberBinderService();

        var methodGroup = memberBinder.BindMemberRead(typeof(string), nameof(string.Contains), isStatic: false, isCaseSensitive: true);
        Assert.That(methodGroup.IsMethodGroup, Is.True);

        var indexPlan = memberBinder.BindIndexRead(typeof(List<int>), typeof(int));
        Assert.That(indexPlan.IsDirectCollectionAccess, Is.True);
    }
}
