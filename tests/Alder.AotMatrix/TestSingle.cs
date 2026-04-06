using Alder;

public static class TestSingle
{
    public static async Task<int> Run(string filePath)
    {
        if (filePath == "--diag")
            return RunDiag();
        if (filePath == "--check-tuple")
            return RunCheckTuple();
        if (filePath == "--check-factories")
            return RunCheckFactories();
        if (filePath == "--check-enum")
            return RunCheckEnum();
        if (filePath == "--check-ext")
            return RunCheckExtensionDispatches();

        var expr = File.ReadAllText(filePath).Trim();
        Console.WriteLine($"Expression file: {filePath}");
        Console.WriteLine($"Expression length: {expr.Length} chars");
        Console.WriteLine();

        var engine = new AlderEngine(new AlderOptions
        {
            LanguageMode = LanguageMode.Extended,
            Constraints = new ExecutionConstraints { MaxStatements = 500_000 }
        });

        var exitCode = 0;
        var isAsyncOnly = filePath.Contains("ValidAsyncExpressions");

        if (!isAsyncOnly)
        {
            try
            {
                var result = engine.Evaluate(expr);
                Console.WriteLine($"SYNC:PASS:{result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SYNC:FAIL:{ex.GetType().Name}: {ex.Message}");
                exitCode = 1;
            }
        }
        else
        {
            Console.WriteLine("SYNC:SKIP:async-only expression");
        }

        try
        {
            var result = await engine.EvaluateAsync(expr);
            Console.WriteLine($"ASYNC:PASS:{result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ASYNC:FAIL:{ex.GetType().Name}: {ex.Message}");
            exitCode = 1;
        }

        return exitCode;
    }

    private static int RunDiag()
    {
        Console.WriteLine("=== AOT Reflection Diagnostics ===");
        var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

        void Check(string label, object instance)
        {
            var type = instance.GetType();
            var props = type.GetProperties(flags);
            var methods = type.GetMethods(flags);
            Console.WriteLine($"\n{label} ({type}):");
            Console.WriteLine($"  Properties ({props.Length}): {string.Join(", ", props.Select(p => p.Name))}");
            Console.WriteLine($"  Methods ({methods.Length}): {string.Join(", ", methods.Select(m => m.Name).Distinct().Take(15))}");
        }

        Check("int[]", new[] { 1, 2, 3 });
        Check("string[]", new[] { "a", "b" });
        Check("object[]", new object[] { 1, "x" });
        Check("List<int>", new List<int> { 1, 2 });
        Check("List<string>", new List<string> { "a" });
        Check("List<object>", new List<object> { 1 });
        Check("Dictionary<string,int>", new Dictionary<string, int> { ["a"] = 1 });
        Check("Dictionary<string,object>", new Dictionary<string, object> { ["a"] = 1 });
        Check("HashSet<int>", new HashSet<int> { 1 });
        Check("Queue<int>", new Queue<int>());
        Check("Stack<int>", new Stack<int>());
        Check("string", "hello");
        Check("int (boxed)", (object)42);
        Check("DateTime", DateTime.Now);
        Check("TimeSpan", TimeSpan.FromSeconds(1));
        Check("Guid", Guid.NewGuid());
        Check("ValueTuple<int,int>", (1, 2));
        Check("Nullable<int> (boxed)", (object)(int?)42);
        Check("Exception", new Exception("test"));
        Check("KeyValuePair<string,int>", new KeyValuePair<string, int>("a", 1));
        Check("Tuple<int,string>", Tuple.Create(1, "a"));
        Check("bool[]", new[] { true, false });
        Check("double[]", new[] { 1.0, 2.0 });
        Check("char[]", new[] { 'a', 'b' });

        return 0;
    }

    private static int RunCheckTuple()
    {
        var tuple = (1, 2);
        var type = tuple.GetType();
        var registered = typeof(ValueTuple<int, int>);
        Console.WriteLine($"Runtime type: {type}");
        Console.WriteLine($"Registered type: {registered}");
        Console.WriteLine($"Equal: {type == registered}");
        Console.WriteLine($"Hash match: {type.GetHashCode() == registered.GetHashCode()}");

        var engine = new AlderEngine();
        var ctx = Alder.Aot.AlderBuiltInContext.Default;
        var meta = ctx.GetTypeMetadata();
        Console.WriteLine($"Metadata entries: {meta.Count}");
        foreach (var m in meta)
        {
            if (m.Type.FullName?.Contains("ValueTuple") == true)
                Console.WriteLine($"  {m.Type} (hash={m.Type.GetHashCode()})");
        }

        Console.WriteLine($"runtime type hash: {type.GetHashCode()}");
        return 0;
    }

    private static int RunCheckEnum()
    {
        Console.WriteLine("=== Enum AOT Debug ===");
        Console.Out.Flush();

        // Step 1: Direct reflection — does DayOfWeek.Monday exist?
        Console.WriteLine("Step 1: Direct reflection on DayOfWeek...");
        Console.Out.Flush();
        try
        {
            var type = typeof(DayOfWeek);
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Console.WriteLine($"  Fields ({fields.Length}): {string.Join(", ", fields.Select(f => f.Name))}");
            var monday = type.GetField("Monday", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Console.WriteLine($"  Monday field: {monday}");
            if (monday != null)
                Console.WriteLine($"  Monday value: {monday.GetValue(null)}");
            Console.Out.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL: {ex.GetType().Name}: {ex.Message}");
            Console.Out.Flush();
        }

        // Step 2: Full eval
        Console.WriteLine("Step 2: Evaluate 'System.DayOfWeek.Monday'...");
        Console.Out.Flush();
        try
        {
            var engine = new AlderEngine(new AlderOptions { LanguageMode = LanguageMode.Extended });
            var result = engine.Evaluate("System.DayOfWeek.Monday");
            Console.WriteLine($"  Result: {result}");
            Console.Out.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL: {ex.GetType().Name}: {ex.Message}");
            Console.Out.Flush();
        }

        return 0;
    }

    private static int RunCheckExtensionDispatches()
    {
        Console.WriteLine("=== Extension Dispatch Diagnostics ===");
        Console.Out.Flush();

        var ctx = Alder.Aot.AlderBuiltInContext.Default;
        var extDispatches = ctx.GetExtensionDispatches();
        Console.WriteLine($"GetExtensionDispatches() returned: {(extDispatches == null ? "null" : $"count={extDispatches.Count}")}");
        Console.Out.Flush();

        if (extDispatches != null)
        {
            foreach (var d in extDispatches)
            {
                Console.WriteLine($"  Dispatch type: {d.GetType().Name}, Target: {d.Type}");
                // Try a simple Where on int[]
                var testArgs = new object?[] { new[] { 1, 2, 3 }, (Func<int, bool>)(x => x > 1) };
                var ok = d.TryInvokeStatic("Where", testArgs, out var result);
                Console.WriteLine($"  TryInvokeStatic('Where', int[], Func<int,bool>) = {ok}, result type: {result?.GetType().Name ?? "null"}");
                // Try Sum
                var sumArgs = new object?[] { new[] { 1, 2, 3 } };
                var sumOk = d.TryInvokeStatic("Sum", sumArgs, out var sumResult);
                Console.WriteLine($"  TryInvokeStatic('Sum', int[]) = {sumOk}, result: {sumResult}");
                Console.Out.Flush();
            }
        }

        // Now check via the engine's config
        Console.WriteLine("\nChecking engine config path...");
        Console.Out.Flush();
        try
        {
            var engine = new AlderEngine(new AlderOptions { LanguageMode = LanguageMode.Extended });
            // Test a LINQ expression on int[]
            var result = engine.Evaluate("new[] { 1, 2, 3 }.Where(x => x > 1).Sum()");
            Console.WriteLine($"LINQ eval result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LINQ eval FAILED: {ex.GetType().Name}: {ex.Message}");
        }
        Console.Out.Flush();

        return 0;
    }

    private static int RunCheckFactories()
    {
        var ctx = Alder.Aot.AlderBuiltInContext.Default;
        var factories = ctx.GetDelegateFactories();
        Console.WriteLine($"Delegate factories from context: {(factories == null ? "null" : factories.Count.ToString())}");

        var engine = new AlderEngine();
        Console.WriteLine($"Engine created. Testing delegate conversion...");
        try
        {
            var result = engine.Evaluate("Func<int, bool> f = x => x > 5; f(10)");
            Console.WriteLine($"Func<int,bool> result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Func<int,bool> FAILED: {ex.GetType().Name}: {ex.Message}");
        }
        return 0;
    }
}
