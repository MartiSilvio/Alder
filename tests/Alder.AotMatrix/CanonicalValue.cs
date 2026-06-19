using System.Collections;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Alder.Parity;

/// <summary>
/// Deterministic, culture-invariant text rendering of an evaluation result.
///
/// The Alder NativeAOT value can only cross a process boundary as text, so all
/// three engines (Roslyn, Alder-JIT, Alder-AOT) are reduced to a canonical
/// (type, value) string pair and compared as strings on the AOT axis. This file
/// is compiled into both Alder.AotMatrix (the AOT worker that emits canonical
/// records) and Alder.ParityHarness (the orchestrator that parses them), so the
/// two sides render byte-for-byte identically.
///
/// Rendering is interface-based — no arbitrary property reflection — so it stays
/// NativeAOT-safe: primitives, strings, decimals, floats, enums, dictionaries,
/// sequences, and tuples render exactly; anything else falls back to ToString().
/// Object-shaped results (anonymous types vs. dictionaries) may therefore render
/// differently across engines; the orchestrator handles those on the Roslyn axis
/// with live-object structural comparison rather than these strings.
/// </summary>
public static class CanonicalValue
{
    private const int MaxDepth = 32;

    public static string TypeOf(object? value) =>
        value is null ? "null"
        // A Type value (the result of `typeof(...)`) has a different *runtime* type
        // across engines — `System.RuntimeType` under JIT vs an AOT-specific
        // `NativeFormat...TypeInfo` under NativeAOT — even though the value itself is
        // identical. Normalize to a stable label so that difference isn't a spurious
        // type mismatch; the value (rendered full name) is still compared.
        : value is Type ? "System.Type"
        : NormalizeTypeName(value.GetType());

    public static string Render(object? value)
    {
        var sb = new StringBuilder();
        Append(sb, value, depth: 0);
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, object? value, int depth)
    {
        switch (value)
        {
            case null: sb.Append("null"); return;
            case string s: sb.Append('"').Append(Escape(s)).Append('"'); return;
            case char c: sb.Append('\'').Append(Escape(c.ToString())).Append('\''); return;
            case bool b: sb.Append(b ? "true" : "false"); return;
            case float f: sb.Append(RenderFloat(f)); return;
            case double d: sb.Append(RenderDouble(d)); return;
            case decimal m: sb.Append(m.ToString(CultureInfo.InvariantCulture)); return;
            case Enum e: sb.Append(e.GetType().Name).Append('.').Append(e.ToString()); return;
        }

        if (depth >= MaxDepth) { sb.Append("..."); return; }

        if (value is ITuple tuple)
        {
            sb.Append('(');
            for (var i = 0; i < tuple.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                Append(sb, tuple[i], depth + 1);
            }
            sb.Append(')');
            return;
        }

        if (value is IDictionary dict)
        {
            var entries = new List<(string Key, object? Val)>();
            foreach (DictionaryEntry entry in dict)
                entries.Add((Render(entry.Key), entry.Value));
            entries.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));
            sb.Append('{');
            for (var i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(entries[i].Key).Append(" = ");
                Append(sb, entries[i].Val, depth + 1);
            }
            sb.Append('}');
            return;
        }

        if (value is IEnumerable seq)
        {
            sb.Append('[');
            var i = 0;
            foreach (var item in seq)
            {
                if (i++ > 0) sb.Append(", ");
                Append(sb, item, depth + 1);
            }
            sb.Append(']');
            return;
        }

        // Numeric and other IFormattable primitives render invariantly.
        if (value is IFormattable formattable)
        {
            sb.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
            return;
        }

        sb.Append(value.ToString() ?? "null");
    }

    public static string NormalizeTypeName(Type type)
    {
        if (IsAnonymous(type)) return "<anonymous>";
        if (type.IsArray) return NormalizeTypeName(type.GetElementType()!) + "[]";
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            var name = def.FullName ?? def.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0) name = name[..tick];
            var args = string.Join(", ", type.GetGenericArguments().Select(NormalizeTypeName));
            return $"{name}<{args}>";
        }
        return type.FullName ?? type.Name;
    }

    private static bool IsAnonymous(Type type) =>
        Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) &&
        type.Name.Contains("AnonymousType", StringComparison.Ordinal);

    private static string RenderDouble(double d) =>
        double.IsNaN(d) ? "NaN"
        : double.IsPositiveInfinity(d) ? "Infinity"
        : double.IsNegativeInfinity(d) ? "-Infinity"
        : d.ToString("G17", CultureInfo.InvariantCulture);

    private static string RenderFloat(float f) =>
        float.IsNaN(f) ? "NaN"
        : float.IsPositiveInfinity(f) ? "Infinity"
        : float.IsNegativeInfinity(f) ? "-Infinity"
        : f.ToString("G9", CultureInfo.InvariantCulture);

    private static string Escape(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 32) sb.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    else sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    // ── Single-line transport field encoding (for the CANON\t...\t... record) ──

    public static string EscapeField(string s) =>
        s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");

    public static string UnescapeField(string s)
    {
        if (s.IndexOf('\\') < 0) return s;
        var sb = new StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                switch (s[++i])
                {
                    case '\\': sb.Append('\\'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'n': sb.Append('\n'); break;
                    default: sb.Append('\\').Append(s[i]); break;
                }
            }
            else sb.Append(s[i]);
        }
        return sb.ToString();
    }
}
