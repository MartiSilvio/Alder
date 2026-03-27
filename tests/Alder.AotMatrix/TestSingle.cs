// Standalone entry point for testing a single expression under AOT
// Usage: Alder.AotMatrix --single <path-to-csx-file>

using Alder;

public static class TestSingle
{
    public static int Run(string filePath)
    {
        if (filePath == "--diag")
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

        if (filePath == "--check-factories")
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

        var expr = File.ReadAllText(filePath).Trim();
        Console.WriteLine($"Expression file: {filePath}");
        Console.WriteLine($"Expression length: {expr.Length} chars");
        Console.WriteLine();

        try
        {
            var engine = new AlderEngine(new AlderOptions
            {
                LanguageMode = LanguageMode.Extended,
                Constraints = new ExecutionConstraints { MaxStatements = 100_000 }
            });
            var result = engine.Evaluate(expr);
            Console.WriteLine($"Result: {result}");
            Console.WriteLine($"Type: {result?.GetType().Name ?? "null"}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION: {ex.GetType().FullName}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"StackTrace:\n{ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"\nInner: {ex.InnerException.GetType().FullName}");
                Console.WriteLine($"Inner Message: {ex.InnerException.Message}");
            }
            return 1;
        }
    }
}
