using System.Runtime.CompilerServices;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Pipeline;

namespace Alder.Security;

internal sealed class SecurityValidationPass : IBoundTreePass
{
    internal static readonly SecurityValidationPass Instance = new();

    public BoundExpr Execute(BoundExpr tree, PipelineContext context)
    {
        Walk(tree, context.Policy);
        return tree;
    }

    private static void Walk(BoundExpr root, SecurityPolicy policy)
    {
        var stack = new Stack<BoundExpr>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var expr = stack.Pop();
            Validate(expr, policy);
            expr.EnumerateChildren(child => stack.Push(child));
        }
    }

    private static bool IsExtensionMethod(MethodInfo method) =>
        method.IsDefined(typeof(ExtensionAttribute), false);

    private static void ValidateMemberRead(Type memberType, bool isStatic, string memberName, SecurityPolicy policy)
    {
        if (!policy.IsTypeAllowed(memberType))
            throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, memberType.Name);
        if (isStatic && !policy.AllowStaticPropertyRead)
            throw new AlderException(DiagnosticDescriptors.SandboxStaticMemberAccessBlocked, memberType.Name, memberName);
        if (!policy.AllowPropertyRead)
            throw new AlderException(DiagnosticDescriptors.SandboxPropertyAccessBlocked, memberName);
    }

    private static void Validate(BoundExpr expr, SecurityPolicy policy)
    {
        switch (expr)
        {
            case BoundObjectCreationExpr creation:
                if (!policy.AllowConstruction)
                    throw new AlderException(DiagnosticDescriptors.SandboxConstructionBlocked, creation.TypeName);
                if (!policy.IsTypeAllowed(creation.StaticType.ClrType))
                    throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, creation.StaticType.ClrType.Name);
                break;

            case BoundResolvedCallExpr call:
                if (!call.IsModuleCall && !policy.AllowMethodCalls && !IsExtensionMethod(call.SelectedMethod))
                    throw new AlderException(DiagnosticDescriptors.SandboxMethodCallBlocked, call.SelectedMethod.Name);
                var declaringType = call.SelectedMethod.DeclaringType;
                if (declaringType != null && !policy.IsTypeAllowed(declaringType))
                    throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, declaringType.Name);
                break;

            case BoundPropertyAccessExpr prop:
                ValidateMemberRead(prop.Property.ReflectedType ?? prop.Property.DeclaringType!, prop.IsStatic, prop.MemberName, policy);
                break;
            case BoundFieldAccessExpr field:
                ValidateMemberRead(field.Field.ReflectedType ?? field.Field.DeclaringType!, field.IsStatic, field.MemberName, policy);
                break;
            case BoundDynamicMemberAccessExpr dyn:
                if (!policy.AllowPropertyRead)
                    throw new AlderException(DiagnosticDescriptors.SandboxPropertyAccessBlocked, dyn.MemberName);
                break;

            case BoundAssignExpr or BoundCompoundAssignExpr or BoundNullCoalesceAssignExpr
                or BoundIncrementDecrementExpr:
                if (!policy.AllowAssignment)
                    throw new AlderException(DiagnosticDescriptors.SandboxAssignmentBlocked, "variable");
                break;

            case BoundMemberAssignExpr or BoundMemberCompoundAssignExpr
                or BoundMemberNullCoalesceAssignExpr or BoundMemberIncrementExpr:
                if (!policy.AllowPropertySet)
                    throw new AlderException(DiagnosticDescriptors.SandboxPropertyAssignmentBlocked, "property");
                break;

            case BoundIndexAssignExpr or BoundIndexCompoundAssignExpr
                or BoundIndexNullCoalesceAssignExpr or BoundIndexIncrementExpr
                or BoundMultiDimIndexAssignExpr:
                if (!policy.AllowIndexSet)
                    throw new AlderException(DiagnosticDescriptors.SandboxIndexAssignmentBlocked, "index");
                break;
        }
    }
}
