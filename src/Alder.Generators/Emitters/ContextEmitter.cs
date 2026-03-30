using System.Linq;
using Alder.Generators.Model;

namespace Alder.Generators.Emitters;

internal static class ContextEmitter
{
    public static string Emit(ContextModel context, bool hasDelegateFactories, bool hasExtensionDispatch)
    {
        var w = new SourceWriter();

        if (!string.IsNullOrEmpty(context.Namespace))
        {
            using (w.Block($"namespace {context.Namespace}"))
                EmitClassBody(w, context, hasDelegateFactories, hasExtensionDispatch);
            return w.ToString();
        }

        EmitClassBody(w, context, hasDelegateFactories, hasExtensionDispatch);
        return w.ToString();
    }

    private static void EmitClassBody(SourceWriter w, ContextModel context, bool hasDelegateFactories, bool hasExtensionDispatch)
    {
        using (w.Block($"partial class {context.ClassName}"))
        {
            w.AppendLine($"public static {context.ClassName} Default {{ get; }} = new();");
            w.AppendLine();
            w.AppendLine("private static readonly global::Alder.Aot.ITypedDispatch[] s_metadata =");
            w.AppendLine("[");
            w.Indent();

            foreach (var reg in context.Registrations)
                w.AppendLine($"new {reg.MetadataClassName}(),");

            if (hasExtensionDispatch)
                w.AppendLine("new EnumerableDispatch(),");

            w.Outdent();
            w.AppendLine("];");
            w.AppendLine();
            w.AppendLine("public override global::System.Collections.Generic.IReadOnlyList<global::Alder.Aot.ITypedDispatch> GetTypeMetadata() => s_metadata;");

            if (hasDelegateFactories)
            {
                w.AppendLine();
                w.AppendLine("public override global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Func<object, global::System.Delegate>>? GetDelegateFactories() => AotDelegateFactories.Create();");
            }
        }
    }
}
