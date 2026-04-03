using Alder.Attributes;
using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class FunctionsAndModulesDocTests(CompilationMode mode)
{
    [Test]
    public void Functions_Register()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Functions.Register("clamp", args =>
            {
                var value = Convert.ToDouble(args![0]);
                var min = Convert.ToDouble(args[1]);
                var max = Convert.ToDouble(args[2]);
                return Math.Min(Math.Max(value, min), max);
            });
        });

        Assert.That(engine.Evaluate<double>("clamp(150, 0, 100)"), Is.EqualTo(100.0));
        Assert.That(engine.Evaluate<double>("clamp(-5, 0, 100)"), Is.EqualTo(0.0));
        Assert.That(engine.Evaluate<double>("clamp(50, 0, 100)"), Is.EqualTo(50.0));
    }

    public class MathUtils
    {
        public double CircleArea(double radius) => Math.PI * radius * radius;
        public double Hypotenuse(double a, double b) => Math.Sqrt(a * a + b * b);
        public double Pi => Math.PI;
    }

    [Test]
    public void Modules_Register()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.Register<MathUtils>("utils");
        });

        Assert.That(engine.Evaluate<double>("utils.CircleArea(5)"), Is.EqualTo(Math.PI * 25).Within(0.001));
        Assert.That(engine.Evaluate<double>("utils.Pi"), Is.EqualTo(Math.PI));
    }

    [AlderModule("secure", ExplicitOnly = true)]
    public class SecureModule
    {
        [AlderFunction]
        public string GetValue() => "exposed";

        public string InternalMethod() => "hidden";
    }

    [Test]
    public void Modules_ExplicitOnly()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.RegisterFromType<SecureModule>();
        });

        Assert.That(engine.Evaluate<string>("secure.GetValue()"), Is.EqualTo("exposed"));
        Assert.Throws<AlderException>(() => engine.Evaluate("secure.InternalMethod()"));
    }

    public class GlobalHelpers
    {
        [AlderFunction("greet")]
        public string Greet(string name) => $"Hello, {name}!";
    }

    [Test]
    public void Functions_GlobalFromType()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.RegisterFromType<GlobalHelpers>();
        });

        Assert.That(engine.Evaluate<string>("""greet("Alice")"""), Is.EqualTo("Hello, Alice!"));
    }

    public class AsyncService
    {
        public Task<int> FetchValueAsync() => Task.FromResult(42);
        public Task<string> FetchNameAsync(string prefix) => Task.FromResult($"{prefix}-result");
        public async Task<int> ComputeAsync(int a, int b)
        {
            await Task.Delay(1);
            return a + b;
        }
        public Task DelayAsync() => Task.Delay(1);
    }

    [Test]
    public async Task Modules_AsyncMethod_TaskOfT()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.Register<AsyncService>("svc");
        });

        var result = await engine.EvaluateAsync("await svc.FetchValueAsync()");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task Modules_AsyncMethod_WithArgs()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.Register<AsyncService>("svc");
        });

        var result = await engine.EvaluateAsync("""await svc.FetchNameAsync("test")""");
        Assert.That(result, Is.EqualTo("test-result"));
    }

    [Test]
    public async Task Modules_AsyncMethod_VoidTask()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.Register<AsyncService>("svc");
        });

        var result = await engine.EvaluateAsync("await svc.DelayAsync()");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Modules_AsyncMethod_ReturnsTask()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.Register<AsyncService>("svc");
        });

        var raw = await engine.EvaluateAsync("svc.ComputeAsync(10, 20)");
        Assert.That(raw, Is.InstanceOf<Task<int>>(), $"Expected Task<int> but got {raw?.GetType()?.Name ?? "null"}");

        // Manually await to prove the Task is valid
        var manual = await (Task<int>)raw!;
        Assert.That(manual, Is.EqualTo(30));
    }

    [Test]
    public async Task Modules_AsyncMethod_RealAsync_InjectedTask()
    {
        var engine = TestEngineFactory.Create(mode);
        var task = DelayAndReturn(30, 200);
        var result = await engine.EvaluateAsync(
            "await t",
            new Dictionary<string, object?> { ["t"] = task });
        Assert.That(result, Is.EqualTo(30));
    }

    private static async Task<int> DelayAndReturn(int value, int delayMs)
    {
        await Task.Delay(delayMs);
        return value;
    }

    [Test]
    public async Task Modules_AsyncMethod_RealAsync()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.Register<AsyncService>("svc");
        });

        var result = await engine.EvaluateAsync("await svc.ComputeAsync(10, 20)");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public async Task Modules_AsyncMethod_InControlFlow()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.Register<AsyncService>("svc");
        });

        var result = await engine.EvaluateAsync("""
            var sum = 0;
            for (var i = 0; i < 3; i++)
            {
                sum += await svc.ComputeAsync(i, 1);
            }
            return sum;
            """);
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public async Task SetVariable_AsyncFunc()
    {
        var engine = TestEngineFactory.Create(mode);
        Func<int, int, Task<int>> addAsync = async (a, b) =>
        {
            await Task.Delay(1);
            return a + b;
        };
        engine.SetVariable("addAsync", addAsync);

        var result = await engine.EvaluateAsync("await addAsync(10, 20)");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public async Task SetVariable_AsyncFunc_InLoop()
    {
        var engine = TestEngineFactory.Create(mode);
        Func<int, Task<int>> doubleAsync = async n =>
        {
            await Task.Delay(1);
            return n * 2;
        };
        engine.SetVariable("doubleAsync", doubleAsync);

        var result = await engine.EvaluateAsync("""
            var sum = 0;
            for (var i = 1; i <= 3; i++)
            {
                sum += await doubleAsync(i);
            }
            return sum;
            """);
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public async Task SetVariable_AsyncFunc_CompletedTask()
    {
        var engine = TestEngineFactory.Create(mode);
        Func<string, Task<string>> upperAsync = s => Task.FromResult(s.ToUpper());
        engine.SetVariable("upperAsync", upperAsync);

        var result = await engine.EvaluateAsync("""await upperAsync("hello")""");
        Assert.That(result, Is.EqualTo("HELLO"));
    }
}
