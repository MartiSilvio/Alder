using System.Collections.Concurrent;

namespace CsEval.Test.Stress;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ConcurrencyHammerTests(CompilationMode mode) : StressTestBase(mode)
{
    [Test]
    public void ParallelChildren_ShouldBeSafe_IfParentIsReadOnly()
    {
        // Correct usage: Parent configured once, then children spawn and run in parallel.
        Engine.SetVariable("globalConfig", 123);
        
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 };
        var exceptions = new ConcurrentBag<Exception>();
        
        Parallel.For(0, 1000, parallelOptions, i => 
        {
            try
            {
                var child = Engine.CreateChild();
                child.SetVariable("local", i);
                var result = child.Evaluate("globalConfig + local");
                Assert.That(result, Is.EqualTo(123 + i));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });
        
        Assert.That(exceptions, Is.Empty);
    }

    [Test]
    public void ParallelParsing_SameExpression_ShouldHitCacheSafe()
    {
        // Multiple threads parsing/evaluating the SAME expression should hit the shared ExpressionCache.
        // If cache is not thread-safe, this will crash.
        var expr = "1 + 1";
        
        Parallel.For(0, 10000, i => 
        {
            Engine.Evaluate(expr);
        });
    }

    [Test]
    public void ParallelParsing_DifferentExpressions_ShouldStressCacheWraps()
    {
        // Generate unique expressions to flood the cache
        Parallel.For(0, 5000, i => 
        {
            Engine.Evaluate($"{i} + {i}");
        });
    }

    [Test]
    [Explicit("Demonstrates expected unsafe behavior - Modifying parent while children read is NOT thread safe")]
    public void ModifyingParent_WhileChildrenRead_ShouldCrashOrCorrupt()
    {
        // This test exposes the lack of thread safety if the user violates the contract.
        // Children share the _functions dictionary reference from parent.
        // Dictionary<T,K> is not thread safe for read/write.
        
        var running = true;
        
        // Writer thread
        var writerTask = Task.Run(() => 
        {
            var i = 0;
            while(running)
            {
                Engine.RegisterFunction($"func{i}", args => i);
                i++;
                // Add some jitter
                if (i % 100 == 0) Thread.Sleep(1);
            }
        });
        
        // Reader threads (children)
        var readerTask = Task.Run(() => 
        {
            Parallel.For(0, 1000, i => 
            {
                try
                {
                    var child = Engine.CreateChild();
                    // Just resolving a function might trigger dictionary read
                    // Or evaluating a text that calls a function
                    // "Math.Abs(1)" calls a module, let's try calling a function we just registered?
                    // Or just any evaluation that triggers lookup.
                    child.Evaluate("1+1"); 
                }
                catch
                {
                    // Ignore transient errors, we want to see if process crashes or hangs
                }
            });
        });
        
        readerTask.Wait(2000);
        running = false;
        writerTask.Wait(1000);
    }
    
    [Test]
    public void ConcurrentRecursiveEvaluation_ShouldNotDeadlock()
    {
        // If there are any locks in evaluation, recursion + concurrency might deadlock.
        var expr = "1 + 1";
        
        Parallel.For(0, 100, i => 
        {
            var child = Engine.CreateChild();
            // Nested children?
            var grandChild = child.CreateChild();
            grandChild.Evaluate(expr);
        });
    }
}
