using Alder.Generators.Model;

namespace Alder.Generators.Emitters;

internal static class ContextEmitter
{
    public static string Emit(ContextModel context)
    {
        var w = new SourceWriter();

        if (!string.IsNullOrEmpty(context.Namespace))
            return EmitInNamespace(w, context);

        EmitClassBody(w, context);
        return w.ToString();
    }

    private static string EmitInNamespace(SourceWriter w, ContextModel context)
    {
        using (w.Block($"namespace {context.Namespace}"))
            EmitClassBody(w, context);
        return w.ToString();
    }

    private static void EmitClassBody(SourceWriter w, ContextModel context)
    {
        using (w.Block($"partial class {context.ClassName}"))
        {
            w.AppendLine($"public static {context.ClassName} Default {{ get; }} = new();");
            w.AppendLine();
            w.AppendLine("private static readonly global::Alder.Aot.IAotTypeMetadata[] s_metadata =");
            w.AppendLine("[");
            w.Indent();

            foreach (var reg in context.Registrations)
                w.AppendLine($"new {reg.MetadataClassName}(),");

            w.Outdent();
            w.AppendLine("];");
            w.AppendLine();
            w.AppendLine("public override global::System.Collections.Generic.IReadOnlyList<global::Alder.Aot.IAotTypeMetadata> GetTypeMetadata() => s_metadata;");
        }
    }
}
