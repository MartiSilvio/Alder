using System;
using System.Collections.Generic;
using Alder.Generators.Model;

namespace Alder.Generators.Emitters;

internal static class ContextEmitter
{
    private const string TaskNamespace = "System.Threading.Tasks";
    private const string TaskMetadataName = "Task`1";

    public static string Emit(ContextModel context)
    {
        var w = new SourceWriter();

        if (!string.IsNullOrEmpty(context.Namespace))
        {
            using (w.Block($"namespace {context.Namespace}"))
                EmitClassBody(w, context);
            return w.ToString();
        }

        EmitClassBody(w, context);
        return w.ToString();
    }

    private static void EmitClassBody(SourceWriter w, ContextModel context)
    {
        var taskRoots = GetTaskRoots(context);

        using (w.Block($"partial class {context.ClassName}"))
        {
            w.AppendLine($"public static {context.ClassName} Default {{ get; }} = new();");
            w.AppendLine();
            w.AppendLine("private static readonly global::Alder.Aot.TypedDispatch[] s_metadata =");
            w.AppendLine("[");
            w.Indent();

            foreach (var reg in context.Registrations)
                w.AppendLine($"new {reg.MetadataClassName}(),");

            w.Outdent();
            w.AppendLine("];");
            w.AppendLine();

            if (taskRoots.Count > 0)
            {
                w.AppendLine("private static readonly global::Alder.Aot.GenericStaticDispatch[] s_genericStaticDispatch =");
                w.AppendLine("[");
                w.Indent();
                w.AppendLine("new TaskFromResultDispatch(),");
                w.Outdent();
                w.AppendLine("];");
                w.AppendLine();
            }

            w.AppendLine("public override global::System.Collections.Generic.IReadOnlyList<global::Alder.Aot.TypedDispatch> GetTypeMetadata() => s_metadata;");
            if (taskRoots.Count > 0)
                w.AppendLine("public override global::System.Collections.Generic.IReadOnlyList<global::Alder.Aot.GenericStaticDispatch>? GetGenericStaticDispatch() => s_genericStaticDispatch;");

            EmitTaskFromResultDispatchIfNeeded(w, taskRoots);
        }
    }

    private static List<TypeArgumentModel> GetTaskRoots(ContextModel context)
    {
        var roots = new List<TypeArgumentModel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var reg in context.Registrations)
        {
            if (reg.OriginalDefinitionNamespace != TaskNamespace ||
                reg.OriginalDefinitionMetadataName != TaskMetadataName ||
                reg.TypeArguments.Length != 1)
            {
                continue;
            }

            var taskResultType = reg.TypeArguments[0];
            if (seen.Add(taskResultType.TypeFullName))
                roots.Add(taskResultType);
        }

        return roots;
    }

    private static void EmitTaskFromResultDispatchIfNeeded(SourceWriter w, IReadOnlyList<TypeArgumentModel> taskRoots)
    {
        if (taskRoots.Count == 0)
            return;

        w.AppendLine();
        using (w.Block("private sealed class TaskFromResultDispatch : global::Alder.Aot.GenericStaticDispatch"))
        {
            w.AppendLine("public override global::System.Type DeclaringType => typeof(global::System.Threading.Tasks.Task);");
            w.AppendLine();
            using (w.Block("public override bool TryInvoke(string methodName, global::System.Type? requestedType, object?[] args, out object? result)"))
            {
                w.AppendLine("if (methodName != nameof(global::System.Threading.Tasks.Task.FromResult) || args.Length != 1 || requestedType == null)");
                using (w.Block())
                {
                    w.AppendLine("result = null;");
                    w.AppendLine("return false;");
                }
                w.AppendLine();

                foreach (var taskRoot in taskRoots)
                {
                    using (w.Block($"if ({FormatTaskRootCondition(taskRoot)})"))
                    {
                        w.AppendLine($"result = global::System.Threading.Tasks.Task.FromResult({FormatTaskRootArgument(taskRoot)});");
                        w.AppendLine("return true;");
                    }
                }

                w.AppendLine("result = null;");
                w.AppendLine("return false;");
            }
        }
    }

    private static string FormatTaskRootCondition(TypeArgumentModel taskRoot)
    {
        if (!taskRoot.CanBeNull)
            return $"requestedType == typeof({taskRoot.TypeFullName}) && args[0] is {taskRoot.TypeFullName}";

        return $"requestedType == typeof({taskRoot.TypeFullName}) && (args[0] is {taskRoot.TypeFullName} || args[0] is null)";
    }

    private static string FormatTaskRootArgument(TypeArgumentModel taskRoot)
    {
        return $"({taskRoot.TypeFullName})args[0]!";
    }
}
