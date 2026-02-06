using System.Text;

namespace CsEval.Test.Stress;

public class StressTestBase
{
    protected readonly Random Random = new(1337); // Fixed seed for reproducibility
    protected CsEvalEngine Engine = null!;

    protected readonly CompilationMode Mode;

    protected StressTestBase(CompilationMode mode)
    {
        Mode = mode;
    }

    [SetUp]
    public void Setup()
    {
        Engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = Mode });
    }

    protected string GenerateDeeplyNestedExpression(int depth, string inner = "1")
    {
        var sb = new StringBuilder();
        for (var i = 0; i < depth; i++) sb.Append('(');
        sb.Append(inner);
        for (var i = 0; i < depth; i++) sb.Append(')');
        return sb.ToString();
    }

    protected string GenerateRandomString(int length, bool includeControlChars = false, bool includeUnicode = true)
    {
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            if (includeUnicode && Random.NextDouble() > 0.7)
            {
                // Random unicode char
                sb.Append((char)Random.Next(0, 0xFFFF));
            }
            else if (includeControlChars && Random.NextDouble() > 0.9)
            {
                // Control char
                sb.Append((char)Random.Next(0, 32));
            }
            else
            {
                // Printable ascii
                sb.Append((char)Random.Next(32, 126));
            }
        }
        return sb.ToString();
    }

    protected string GenerateLongExpression(int operations)
    {
        var sb = new StringBuilder();
        sb.Append(Random.Next(100));
        for (var i = 0; i < operations; i++)
        {
            var op = Random.Next(4) switch
            {
                0 => "+",
                1 => "-",
                2 => "*",
                3 => "/",
                _ => "+"
            };
            sb.Append($" {op} {Random.Next(1, 100)}");
        }
        return sb.ToString();
    }
}
