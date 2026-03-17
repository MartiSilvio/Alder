using System.Text;
using CsEval.Generators.Model;

namespace CsEval.Generators.Emitters;

internal static class ContextEmitter
{
    public static string Emit(ContextModel context)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(context.Namespace))
        {
            sb.AppendLine($"namespace {context.Namespace}");
            sb.AppendLine("{");
        }

        var indent = string.IsNullOrEmpty(context.Namespace) ? "" : "    ";

        sb.AppendLine($"{indent}partial class {context.ClassName}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    public static {context.ClassName} Default {{ get; }} = new();");
        sb.AppendLine();
        sb.AppendLine($"{indent}    private static readonly global::CsEval.Aot.IAotTypeMetadata[] s_metadata =");
        sb.AppendLine($"{indent}    [");

        foreach (var reg in context.Registrations)
            sb.AppendLine($"{indent}        new {reg.MetadataClassName}(),");

        sb.AppendLine($"{indent}    ];");
        sb.AppendLine();
        sb.AppendLine($"{indent}    public override global::System.Collections.Generic.IReadOnlyList<global::CsEval.Aot.IAotTypeMetadata> GetTypeMetadata() => s_metadata;");
        sb.AppendLine($"{indent}}}");

        if (!string.IsNullOrEmpty(context.Namespace))
            sb.AppendLine("}");

        return sb.ToString();
    }
}
