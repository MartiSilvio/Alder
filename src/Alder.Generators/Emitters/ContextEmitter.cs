using System.Linq;
using Alder.Generators.Model;

namespace Alder.Generators.Emitters;

internal static class ContextEmitter
{
    public static string Emit(ContextModel context, bool hasDelegateFactories)
    {
        var w = new SourceWriter();

        if (!string.IsNullOrEmpty(context.Namespace))
            return EmitInNamespace(w, context, hasDelegateFactories);

        EmitClassBody(w, context, hasDelegateFactories);
        return w.ToString();
    }

    private static string EmitInNamespace(SourceWriter w, ContextModel context, bool hasDelegateFactories)
    {
        using (w.Block($"namespace {context.Namespace}"))
            EmitClassBody(w, context, hasDelegateFactories);
        return w.ToString();
    }

    private static void EmitClassBody(SourceWriter w, ContextModel context, bool hasDelegateFactories)
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

            if (hasDelegateFactories)
            {
                w.AppendLine();
                w.AppendLine("public override global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Func<object, global::System.Delegate>>? GetDelegateFactories() => AotDelegateFactories.Create();");
            }
        }
    }
}
