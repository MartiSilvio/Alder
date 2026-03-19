using System.Reflection;

namespace CsEval.Test.Parity;

[TestFixture]
public class ExecutionModeTests
{
    [Test]
    public void InterpretedMode_DoesNotExecute_PrecompiledDelegate()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        var expression = engine.Parse("1 + 1");

        var fakeCompiled = new CompiledExpressionInfo(
            Delegate: (_, _, _) => 999,
            IsCompilable: true,
            FailureReason: null);

        var field = typeof(CsEvalExpression).GetField("CompiledInfo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.That(field, Is.Not.Null);
        field!.SetValue(expression, fakeCompiled);

        var result = engine.Evaluate(expression);
        Assert.That(result, Is.EqualTo(2));
    }
}
