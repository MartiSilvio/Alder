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

    /// <summary>
    /// Walks a dynamic member access chain to reconstruct the qualified name.
    /// For <c>System.IO.File.ReadAllText(...)</c>, the callee is a chain ending at
    /// an identifier root. This extracts <c>System.IO.File</c> (the target path,
    /// excluding the final method-name segment which is the callee itself).
    /// </summary>
    private static bool TryExtractQualifiedName(BoundExpr callee, out string qualifiedName)
    {
        if (callee is not BoundDynamicMemberAccessExpr methodAccess)
        {
            qualifiedName = "";
            return false;
        }

        var parts = new List<string>();
        BoundExpr current = methodAccess.Target;
        while (current is BoundDynamicMemberAccessExpr dma)
        {
            parts.Add(dma.MemberName);
            current = dma.Target;
        }

        if (current is BoundIdentifierExpr id)
            parts.Add(id.Name);
        else
        {
            qualifiedName = "";
            return false;
        }

        parts.Reverse();
        qualifiedName = string.Join(".", parts);
        return parts.Count >= 2;
    }

    /// <summary>
    /// Returns true only when the target chain root is a resolved variable (identifier
    /// with known type). Unresolved identifiers (modules, namespaces) and non-identifier
    /// roots (constructions, nested calls) return false because those are handled by other
    /// security checks or at runtime where full type info is available.
    /// </summary>
    private static bool IsResolvedTargetChain(BoundExpr target)
    {
        var current = target;
        while (current is BoundDynamicMemberAccessExpr chain)
            current = chain.Target;
        return current is BoundIdentifierExpr { StaticType: not BoundUnknownType };
    }

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

            case BoundDynamicCallExpr dynamicCall:
                if (dynamicCall.Callee is BoundMethodGroupExpr mg && !policy.IsTypeAllowed(mg.DeclaringType))
                    throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, mg.DeclaringType.Name);
                if (TryExtractQualifiedName(dynamicCall.Callee, out var qualifiedName) &&
                    policy.IsQualifiedNameBlocked(qualifiedName))
                    throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, qualifiedName);
                if (!policy.AllowMethodCalls &&
                    dynamicCall.Callee is BoundDynamicMemberAccessExpr dma &&
                    IsResolvedTargetChain(dma.Target))
                    throw new AlderException(DiagnosticDescriptors.SandboxMethodCallBlocked, dma.MemberName);
                break;

            case BoundCastExpr cast:
                if (!policy.IsTypeAllowed(cast.TargetType))
                    throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, cast.TargetType.Name);
                break;

            case BoundAsExpr asExpr:
                if (!policy.IsTypeAllowed(asExpr.TargetType))
                    throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, asExpr.TargetType.Name);
                break;

            case BoundLiteralExpr { Value: Type typeValue }:
                if (!policy.IsTypeAllowed(typeValue))
                    throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, typeValue.Name);
                break;

            case BoundTypeRefExpr typeRef:
                if (!policy.IsTypeAllowed(typeRef.TargetType))
                    throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, typeRef.TargetType.Name);
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
