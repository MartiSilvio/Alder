using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Alder.Generators.Emitters;
using Alder.Generators.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alder.Generators;

[Generator]
public sealed class BoundNodeGenerator : IIncrementalGenerator
{
    private const string BoundNodeAttributeName = "Alder.Binding.BoundNodeAttribute";
    private const string BoundContainerAttributeName = "Alder.Binding.BoundContainerAttribute";
    private const string BoundExprMetadataName = "Alder.Binding.BoundExpr";
    private const string ImmutableArrayMetadataName = "System.Collections.Immutable.ImmutableArray`1";

    private static readonly DiagnosticDescriptor DuplicateKindValue = new(
        "ALDR9010", "Duplicate BoundNodeKind integer value",
        "BoundNodeKind value {0} is claimed by both '{1}' and '{2}'",
        "Alder.Binding", DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor DuplicateKindName = new(
        "ALDR9011", "Duplicate BoundNodeKind name",
        "BoundNodeKind name '{0}' is claimed by both '{1}' and '{2}'",
        "Alder.Binding", DiagnosticSeverity.Error, true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var nodeEntries = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BoundNodeAttributeName,
                predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                transform: static (ctx, _) => ExtractNodeEntry(ctx))
            .Where(static e => e != null);

        var containerEntries = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BoundContainerAttributeName,
                predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                transform: static (ctx, _) => ExtractContainerEntry(ctx))
            .Where(static e => e != null);

        var allNodes = nodeEntries.Collect();
        var allContainers = containerEntries.Collect();
        var combined = allNodes.Combine(allContainers);

        context.RegisterSourceOutput(combined, static (spc, data) => Emit(spc, data.Left, data.Right));
    }

    private static BoundNodeModel? ExtractNodeEntry(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol nodeClass)
            return null;

        var attr = ctx.Attributes.FirstOrDefault();
        if (attr == null)
            return null;
        if (attr.ConstructorArguments.Length != 2)
            return null;

        var kindConstant = attr.ConstructorArguments[0];
        if (kindConstant.Value is not int kindValue)
            return null;
        var kindName = FindEnumFieldName(kindConstant);
        if (kindName == null)
            return null;
        if (attr.ConstructorArguments[1].Value is not string visitorName)
            return null;

        var chainFlatten = GetNamedBool(attr, "ChainFlatten");
        var hasRevisitHook = GetNamedBool(attr, "HasRevisitHook");
        var manualRewrite = GetNamedBool(attr, "ManualRewrite");

        var compilation = ctx.SemanticModel.Compilation;
        var boundExprSymbol = compilation.GetTypeByMetadataName(BoundExprMetadataName);
        if (boundExprSymbol == null)
            return null;

        var children = ClassifyChildren(nodeClass, boundExprSymbol, compilation);

        return new BoundNodeModel
        {
            KindValue = kindValue,
            KindName = kindName,
            VisitorName = visitorName,
            TypeFullName = nodeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            TypeShortName = nodeClass.Name,
            ChainFlatten = chainFlatten,
            HasRevisitHook = hasRevisitHook,
            ManualRewrite = manualRewrite,
            Children = children
        };
    }

    private static BoundContainerModel? ExtractContainerEntry(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol containerClass)
            return null;

        var compilation = ctx.SemanticModel.Compilation;
        var boundExprSymbol = compilation.GetTypeByMetadataName(BoundExprMetadataName);
        if (boundExprSymbol == null)
            return null;

        var children = ClassifyContainerChildren(containerClass, boundExprSymbol, compilation);

        return new BoundContainerModel
        {
            TypeFullName = containerClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            TypeShortName = containerClass.Name,
            BaseTypeFullName = containerClass.BaseType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "",
            IsAbstract = containerClass.IsAbstract,
            Children = children
        };
    }

    private static string? FindEnumFieldName(TypedConstant constant)
    {
        if (constant.Type == null) return null;
        var value = constant.Value;
        foreach (var member in constant.Type.GetMembers())
        {
            if (member is IFieldSymbol { HasConstantValue: true } field && Equals(field.ConstantValue, value))
                return field.Name;
        }
        return null;
    }

    private static bool GetNamedBool(AttributeData attr, string name)
    {
        foreach (var kvp in attr.NamedArguments)
        {
            if (kvp.Key == name && kvp.Value.Value is bool b)
                return b;
        }
        return false;
    }

    private static ImmutableArray<ChildInfo> ClassifyChildren(
        INamedTypeSymbol nodeClass, INamedTypeSymbol boundExprSymbol, Compilation compilation)
    {
        var immutableArraySymbol = compilation.GetTypeByMetadataName(ImmutableArrayMetadataName);
        var builder = ImmutableArray.CreateBuilder<ChildInfo>();

        foreach (var param in GetRecordPrimaryConstructorParameters(nodeClass))
        {
            var paramType = param.Type;

            if (IsBoundExpr(paramType, boundExprSymbol))
            {
                if (IsNullableParameter(param))
                {
                    builder.Add(new ChildInfo { ParameterName = param.Name, Kind = ChildKind.Nullable });
                }
                else
                {
                    builder.Add(new ChildInfo { ParameterName = param.Name, Kind = ChildKind.Direct });
                }
                continue;
            }

            if (IsNullableImmutableArrayOfBoundExpr(paramType, boundExprSymbol, immutableArraySymbol))
            {
                builder.Add(new ChildInfo { ParameterName = param.Name, Kind = ChildKind.NullableArray });
                continue;
            }

            if (IsImmutableArrayOf(paramType, boundExprSymbol, immutableArraySymbol))
            {
                builder.Add(new ChildInfo { ParameterName = param.Name, Kind = ChildKind.Array });
                continue;
            }

            if (immutableArraySymbol != null &&
                paramType is INamedTypeSymbol { IsGenericType: true } namedArr &&
                SymbolEqualityComparer.Default.Equals(namedArr.OriginalDefinition, immutableArraySymbol))
            {
                var elementType = namedArr.TypeArguments[0];
                if (elementType is INamedTypeSymbol elementNamed)
                {
                    var containerChild = TryClassifyContainerArray(elementNamed, boundExprSymbol, compilation, immutableArraySymbol);
                    if (containerChild != null)
                    {
                        builder.Add(containerChild.Value with { ParameterName = param.Name });
                        continue;
                    }
                }
            }
        }

        return builder.ToImmutable();
    }

    private static ChildInfo? TryClassifyContainerArray(
        INamedTypeSymbol elementType, INamedTypeSymbol boundExprSymbol,
        Compilation compilation, INamedTypeSymbol immutableArraySymbol)
    {
        if (HasAttribute(elementType, BoundContainerAttributeName))
        {
            var containerChildren = ClassifyContainerChildren(elementType, boundExprSymbol, compilation, immutableArraySymbol);
            if (containerChildren.Length > 0)
            {
                return new ChildInfo
                {
                    ParameterName = null!,
                    Kind = ChildKind.ContainerArray,
                    ContainerTypeFullName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    ContainerChildren = containerChildren,
                    Subtypes = ImmutableArray<PolymorphicSubtype>.Empty
                };
            }
        }

        if (elementType.IsAbstract)
        {
            var subtypes = FindContainerSubtypes(elementType, boundExprSymbol, compilation);
            if (subtypes.Length > 0)
            {
                return new ChildInfo
                {
                    ParameterName = null!,
                    Kind = ChildKind.PolymorphicContainerArray,
                    ContainerTypeFullName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    ContainerChildren = ImmutableArray<ContainerChildInfo>.Empty,
                    Subtypes = subtypes
                };
            }
        }

        return null;
    }

    private static ImmutableArray<PolymorphicSubtype> FindContainerSubtypes(
        INamedTypeSymbol baseType, INamedTypeSymbol boundExprSymbol, Compilation compilation)
    {
        var builder = ImmutableArray.CreateBuilder<PolymorphicSubtype>();
        var ns = baseType.ContainingNamespace;

        foreach (var member in ns.GetTypeMembers())
        {
            if (member.IsAbstract) continue;
            if (!DerivesFrom(member, baseType)) continue;
            if (!HasAttribute(member, BoundContainerAttributeName)) continue;

            var children = ClassifyContainerChildren(member, boundExprSymbol, compilation);
            if (children.Length > 0)
            {
                builder.Add(new PolymorphicSubtype
                {
                    TypeFullName = member.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Children = children
                });
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<ContainerChildInfo> ClassifyContainerChildren(
        INamedTypeSymbol containerClass, INamedTypeSymbol boundExprSymbol, Compilation compilation,
        INamedTypeSymbol immutableArraySymbol = null)
    {
        immutableArraySymbol ??= compilation.GetTypeByMetadataName(ImmutableArrayMetadataName);
        var builder = ImmutableArray.CreateBuilder<ContainerChildInfo>();

        foreach (var param in GetRecordPrimaryConstructorParameters(containerClass))
        {
            var paramType = param.Type;

            if (IsBoundExpr(paramType, boundExprSymbol))
            {
                var kind = IsNullableParameter(param)
                    ? ContainerChildKind.Nullable
                    : ContainerChildKind.Direct;
                builder.Add(new ContainerChildInfo { ParameterName = param.Name, Kind = kind });
                continue;
            }

            if (IsNullableImmutableArrayOfBoundExpr(paramType, boundExprSymbol, immutableArraySymbol))
            {
                builder.Add(new ContainerChildInfo { ParameterName = param.Name, Kind = ContainerChildKind.NullableArray });
                continue;
            }

            if (IsImmutableArrayOf(paramType, boundExprSymbol, immutableArraySymbol))
            {
                builder.Add(new ContainerChildInfo { ParameterName = param.Name, Kind = ContainerChildKind.Array });
                continue;
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<IParameterSymbol> GetRecordPrimaryConstructorParameters(INamedTypeSymbol type)
    {
        foreach (var ctor in type.InstanceConstructors)
        {
            if (ctor.DeclaringSyntaxReferences.Length > 0)
            {
                var syntax = ctor.DeclaringSyntaxReferences[0].GetSyntax();
                if (syntax is RecordDeclarationSyntax)
                    return ctor.Parameters;
            }
        }

        foreach (var ctor in type.InstanceConstructors)
        {
            if (ctor.Parameters.Length > 0)
                return ctor.Parameters;
        }

        return ImmutableArray<IParameterSymbol>.Empty;
    }

    private static bool IsBoundExpr(ITypeSymbol type, INamedTypeSymbol boundExprSymbol)
    {
        var unwrapped = type;
        if (unwrapped is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            unwrapped = nullable.TypeArguments[0];

        return unwrapped is INamedTypeSymbol named && DerivesFrom(named, boundExprSymbol);
    }

    private static bool IsNullableParameter(IParameterSymbol param) =>
        param.NullableAnnotation == NullableAnnotation.Annotated ||
        param.Type.NullableAnnotation == NullableAnnotation.Annotated;

    private static bool IsImmutableArrayOf(ITypeSymbol type, INamedTypeSymbol elementBase, INamedTypeSymbol immutableArraySymbol)
    {
        if (immutableArraySymbol == null) return false;
        if (type is not INamedTypeSymbol { IsGenericType: true } named) return false;
        if (!SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, immutableArraySymbol)) return false;
        return named.TypeArguments[0] is INamedTypeSymbol argNamed && DerivesFrom(argNamed, elementBase);
    }

    private static bool IsNullableImmutableArrayOfBoundExpr(
        ITypeSymbol type, INamedTypeSymbol boundExprSymbol, INamedTypeSymbol immutableArraySymbol)
    {
        if (immutableArraySymbol == null) return false;
        if (type is not INamedTypeSymbol { IsGenericType: true, OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            return false;
        var inner = nullable.TypeArguments[0];
        return IsImmutableArrayOf(inner, boundExprSymbol, immutableArraySymbol);
    }

    private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        var current = type;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static bool HasAttribute(INamedTypeSymbol type, string attributeMetadataName)
    {
        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == attributeMetadataName)
                return true;
        }
        return false;
    }

    private static void Emit(
        SourceProductionContext spc,
        ImmutableArray<BoundNodeModel?> nodeEntries,
        ImmutableArray<BoundContainerModel?> containerEntries)
    {
        if (nodeEntries.IsDefaultOrEmpty)
            return;

        var nodes = new List<BoundNodeModel>();
        foreach (var e in nodeEntries)
        {
            if (e == null) continue;
            nodes.Add(e.Value);
        }

        var containers = new List<BoundContainerModel>();
        if (!containerEntries.IsDefaultOrEmpty)
        {
            foreach (var c in containerEntries)
            {
                if (c == null) continue;
                containers.Add(c.Value);
            }
        }

        if (!ValidateUniqueness(spc, nodes))
            return;

        nodes.Sort((a, b) => a.KindValue.CompareTo(b.KindValue));

        EmitBoundNodesGenerated(spc, nodes);
        EmitBoundExprVisitor(spc, nodes);
        EmitBoundExprRewriter(spc, nodes, containers);
    }

    private static bool ValidateUniqueness(SourceProductionContext spc, List<BoundNodeModel> nodes)
    {
        var byValue = new Dictionary<int, BoundNodeModel>();
        var byName = new Dictionary<string, BoundNodeModel>();
        var valid = true;

        foreach (var node in nodes)
        {
            if (byValue.TryGetValue(node.KindValue, out var existing))
            {
                spc.ReportDiagnostic(Diagnostic.Create(DuplicateKindValue, Location.None,
                    node.KindValue, existing.TypeShortName, node.TypeShortName));
                valid = false;
            }
            else
            {
                byValue[node.KindValue] = node;
            }

            if (byName.TryGetValue(node.KindName, out var existingName))
            {
                spc.ReportDiagnostic(Diagnostic.Create(DuplicateKindName, Location.None,
                    node.KindName, existingName.TypeShortName, node.TypeShortName));
                valid = false;
            }
            else
            {
                byName[node.KindName] = node;
            }
        }

        return valid;
    }

    private static void EmitBoundNodesGenerated(SourceProductionContext spc, List<BoundNodeModel> nodes)
    {
        var w = new SourceWriter();
        w.AppendLine("// <auto-generated/>");
        w.AppendLine("#nullable enable");
        w.AppendLine();
        w.AppendLine("using Alder.Binding;");
        w.AppendLine();

        using (w.Block("namespace Alder.Binding.BoundNodes"))
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (i > 0) w.AppendLine();
                var n = nodes[i];

                using (w.Block($"partial record {n.TypeShortName}"))
                {
                    w.AppendLine($"internal override BoundNodeKind Kind => BoundNodeKind.{n.KindName};");

                    w.AppendLine("internal override void EnumerateChildren(System.Action<BoundExpr> visit)");
                    w.AppendLine("{");
                    w.Indent();
                    EmitEnumerateChildrenBody(w, n.Children);
                    w.Outdent();
                    w.AppendLine("}");
                }
            }
        }

        spc.AddSource("BoundNodes.Generated.g.cs", w.ToString());
    }

    private static void EmitEnumerateChildrenBody(SourceWriter w, ImmutableArray<ChildInfo> children)
    {
        foreach (var child in children)
        {
            switch (child.Kind)
            {
                case ChildKind.Direct:
                    w.AppendLine($"visit({child.ParameterName});");
                    break;
                case ChildKind.Nullable:
                    w.AppendLine($"if ({child.ParameterName} != null) visit({child.ParameterName});");
                    break;
                case ChildKind.Array:
                    w.AppendLine($"foreach (var item in {child.ParameterName}) visit(item);");
                    break;
                case ChildKind.NullableArray:
                    w.AppendLine($"if ({child.ParameterName} != null) foreach (var item in {child.ParameterName}.Value) visit(item);");
                    break;
                case ChildKind.ContainerArray:
                    EmitContainerEnumeration(w, child.ParameterName, child.ContainerChildren);
                    break;
                case ChildKind.PolymorphicContainerArray:
                    EmitPolymorphicContainerEnumeration(w, child.ParameterName, child.Subtypes);
                    break;
            }
        }
    }

    private static void EmitContainerEnumeration(SourceWriter w, string paramName, ImmutableArray<ContainerChildInfo> containerChildren)
    {
        using (w.Block($"foreach (var item in {paramName})"))
        {
            foreach (var cc in containerChildren)
            {
                switch (cc.Kind)
                {
                    case ContainerChildKind.Direct:
                        w.AppendLine($"visit(item.{cc.ParameterName});");
                        break;
                    case ContainerChildKind.Nullable:
                        w.AppendLine($"if (item.{cc.ParameterName} != null) visit(item.{cc.ParameterName});");
                        break;
                    case ContainerChildKind.Array:
                        w.AppendLine($"if (!item.{cc.ParameterName}.IsDefaultOrEmpty) foreach (var sub in item.{cc.ParameterName}) visit(sub);");
                        break;
                    case ContainerChildKind.NullableArray:
                        w.AppendLine($"if (item.{cc.ParameterName} != null) foreach (var sub in item.{cc.ParameterName}.Value) visit(sub);");
                        break;
                }
            }
        }
    }

    private static void EmitPolymorphicContainerEnumeration(
        SourceWriter w, string paramName, ImmutableArray<PolymorphicSubtype> subtypes)
    {
        using (w.Block($"foreach (var item in {paramName})"))
        {
            foreach (var sub in subtypes)
            {
                var shortName = GetShortName(sub.TypeFullName);
                using (w.Block($"if (item is {sub.TypeFullName} {ToLocalName(shortName)})"))
                {
                    foreach (var cc in sub.Children)
                    {
                        var accessor = $"{ToLocalName(shortName)}.{cc.ParameterName}";
                        switch (cc.Kind)
                        {
                            case ContainerChildKind.Direct:
                                w.AppendLine($"visit({accessor});");
                                break;
                            case ContainerChildKind.Nullable:
                                w.AppendLine($"if ({accessor} != null) visit({accessor});");
                                break;
                            case ContainerChildKind.Array:
                                w.AppendLine($"if (!{accessor}.IsDefaultOrEmpty) foreach (var sub in {accessor}) visit(sub);");
                                break;
                            case ContainerChildKind.NullableArray:
                                w.AppendLine($"if ({accessor} != null) foreach (var sub in {accessor}.Value) visit(sub);");
                                break;
                        }
                    }
                }
            }
        }
    }

    private static void EmitBoundExprVisitor(SourceProductionContext spc, List<BoundNodeModel> nodes)
    {
        var w = new SourceWriter();
        w.AppendLine("// <auto-generated/>");
        w.AppendLine("#nullable enable");
        w.AppendLine();
        w.AppendLine("using Alder.Binding.BoundNodes;");
        w.AppendLine();

        using (w.Block("namespace Alder.Binding"))
        {
            // ReSharper disable comment matches existing file
            w.AppendLine("// ReSharper disable VirtualMemberNeverOverridden.Global");
            w.AppendLine();
            using (w.Block("internal abstract class BoundExprVisitor<T>"))
            {
                w.AppendLine("public T Visit(BoundExpr expr) => expr.Kind switch");
                w.AppendLine("{");
                w.Indent();
                foreach (var n in nodes)
                {
                    w.AppendLine($"BoundNodeKind.{n.KindName} => Visit{n.VisitorName}(({n.TypeShortName})expr),");
                }
                w.AppendLine("_ => DefaultVisit(expr)");
                w.Outdent();
                w.AppendLine("};");

                w.AppendLine();
                w.AppendLine("protected virtual T DefaultVisit(BoundExpr node) => default!;");

                w.AppendLine();
                foreach (var n in nodes)
                {
                    w.AppendLine($"protected virtual T Visit{n.VisitorName}({n.TypeShortName} node) => DefaultVisit(node);");
                }
            }
        }

        spc.AddSource("BoundExprVisitor.g.cs", w.ToString());
    }

    private static void EmitBoundExprRewriter(
        SourceProductionContext spc, List<BoundNodeModel> nodes, List<BoundContainerModel> containers)
    {
        var w = new SourceWriter();
        w.AppendLine("// <auto-generated/>");
        w.AppendLine("#nullable enable");
        w.AppendLine();
        w.AppendLine("using System.Collections.Generic;");
        w.AppendLine("using System.Collections.Immutable;");
        w.AppendLine("using Alder.Binding.BoundNodes;");
        w.AppendLine();

        using (w.Block("namespace Alder.Binding"))
        {
            using (w.Block("internal abstract partial class BoundExprRewriter"))
            {
                foreach (var n in nodes)
                {
                    if (n.ManualRewrite) continue;

                    w.AppendLine();

                    if (n.ChainFlatten)
                    {
                        EmitChainFlattenRewriter(w, n);
                    }
                    else if (n.Children.Length == 0)
                    {
                        w.AppendLine($"protected override BoundExpr Visit{n.VisitorName}({n.TypeShortName} node) => node;");
                    }
                    else
                    {
                        EmitStandardRewriter(w, n, containers);
                    }
                }

                var emittedContainers = new HashSet<string>();
                foreach (var n in nodes)
                {
                    if (n.ManualRewrite) continue;
                    foreach (var child in n.Children)
                    {
                        if (child.Kind == ChildKind.ContainerArray && emittedContainers.Add(child.ContainerTypeFullName))
                        {
                            w.AppendLine();
                            EmitContainerRewriteHelper(w, child, containers);
                        }
                        else if (child.Kind == ChildKind.PolymorphicContainerArray && emittedContainers.Add(child.ContainerTypeFullName))
                        {
                            w.AppendLine();
                            EmitPolymorphicContainerRewriteHelper(w, child);
                        }
                    }
                }
            }
        }

        spc.AddSource("BoundExprRewriter.g.cs", w.ToString());
    }

    private static void EmitChainFlattenRewriter(SourceWriter w, BoundNodeModel n)
    {
        string leftName = null, rightName = null;
        foreach (var child in n.Children)
        {
            if (child.Kind == ChildKind.Direct)
            {
                if (leftName == null) leftName = child.ParameterName;
                else if (rightName == null) rightName = child.ParameterName;
            }
        }

        if (leftName == null || rightName == null) return;

        using (w.Block($"protected override BoundExpr Visit{n.VisitorName}({n.TypeShortName} node)"))
        {
            w.AppendLine($"var chain = new List<{n.TypeShortName}>();");
            w.AppendLine("BoundExpr leftmost = node;");
            using (w.Block($"while (leftmost is {n.TypeShortName} b)"))
            {
                w.AppendLine("chain.Add(b);");
                w.AppendLine($"leftmost = b.{leftName};");
            }
            w.AppendLine();
            w.AppendLine("var current = Visit(leftmost);");
            w.AppendLine();
            using (w.Block("for (var i = chain.Count - 1; i >= 0; i--)"))
            {
                w.AppendLine("var original = chain[i];");
                w.AppendLine($"var newRight = Visit(original.{rightName});");
                w.AppendLine();
                using (w.Block($"if (!ReferenceEquals(current, original.{leftName}) || !ReferenceEquals(newRight, original.{rightName}))"))
                {
                    w.AppendLine($"var rewritten = original with {{ {leftName} = current, {rightName} = newRight }};");
                    if (n.HasRevisitHook)
                        w.AppendLine($"current = Revisit{n.VisitorName}(CopyMetadata(original, rewritten));");
                    else
                        w.AppendLine("current = CopyMetadata(original, rewritten);");
                }
                using (w.Block("else"))
                {
                    if (n.HasRevisitHook)
                        w.AppendLine($"current = Revisit{n.VisitorName}(original);");
                    else
                        w.AppendLine("current = original;");
                }
            }
            w.AppendLine();
            w.AppendLine("return current;");
        }

        if (n.HasRevisitHook)
        {
            w.AppendLine();
            w.AppendLine($"protected virtual BoundExpr Revisit{n.VisitorName}(BoundExpr node) => node;");
        }
    }

    private static void EmitStandardRewriter(SourceWriter w, BoundNodeModel n, List<BoundContainerModel> containers)
    {
        using (w.Block($"protected override BoundExpr Visit{n.VisitorName}({n.TypeShortName} node)"))
        {
            var locals = new List<(string localName, string paramName, string kind)>();

            foreach (var child in n.Children)
            {
                var localName = "new" + child.ParameterName;

                switch (child.Kind)
                {
                    case ChildKind.Direct:
                        w.AppendLine($"var {localName} = Visit(node.{child.ParameterName});");
                        locals.Add((localName, child.ParameterName, "ref"));
                        break;
                    case ChildKind.Nullable:
                        w.AppendLine($"var {localName} = node.{child.ParameterName} != null ? Visit(node.{child.ParameterName}) : null;");
                        locals.Add((localName, child.ParameterName, "ref"));
                        break;
                    case ChildKind.Array:
                        w.AppendLine($"var {localName} = RewriteImmutableArray(node.{child.ParameterName}, out var {child.ParameterName}Changed);");
                        locals.Add((localName, child.ParameterName, "changed"));
                        break;
                    case ChildKind.NullableArray:
                    {
                        var changedVar = child.ParameterName + "Changed";
                        w.AppendLine($"var {changedVar} = false;");
                        w.AppendLine($"var {localName} = node.{child.ParameterName};");
                        using (w.Block($"if (node.{child.ParameterName} != null)"))
                        {
                            w.AppendLine($"{localName} = RewriteImmutableArray(node.{child.ParameterName}.Value, out {changedVar});");
                        }
                        locals.Add((localName, child.ParameterName, "changed"));
                        break;
                    }
                    case ChildKind.ContainerArray:
                    {
                        var helperName = "Rewrite" + GetShortName(child.ContainerTypeFullName) + "s";
                        w.AppendLine($"var {localName} = {helperName}(node.{child.ParameterName}, out var {child.ParameterName}Changed);");
                        locals.Add((localName, child.ParameterName, "changed"));
                        break;
                    }
                    case ChildKind.PolymorphicContainerArray:
                    {
                        var helperName = "Rewrite" + GetShortName(child.ContainerTypeFullName) + "s";
                        w.AppendLine($"var {localName} = {helperName}(node.{child.ParameterName}, out var {child.ParameterName}Changed);");
                        locals.Add((localName, child.ParameterName, "changed"));
                        break;
                    }
                }
            }

            var conditions = new List<string>();
            foreach (var (localName, paramName, kind) in locals)
            {
                if (kind == "ref")
                    conditions.Add($"ReferenceEquals({localName}, node.{paramName})");
                else
                    conditions.Add($"!{paramName}Changed");
            }

            var check = string.Join(" && ", conditions);
            w.AppendLine($"if ({check}) return node;");

            var assignments = string.Join(", ", locals.Select(l => $"{l.paramName} = {l.localName}"));
            w.AppendLine($"return CopyMetadata(node, node with {{ {assignments} }});");
        }
    }

    private static void EmitContainerRewriteHelper(
        SourceWriter w, ChildInfo child, List<BoundContainerModel> containers)
    {
        var containerShort = GetShortName(child.ContainerTypeFullName);
        var helperName = "Rewrite" + containerShort + "s";

        using (w.Block($"private ImmutableArray<{child.ContainerTypeFullName}> {helperName}(ImmutableArray<{child.ContainerTypeFullName}> items, out bool changed)"))
        {
            w.AppendLine("changed = false;");
            w.AppendLine($"var builder = ImmutableArray.CreateBuilder<{child.ContainerTypeFullName}>(items.Length);");

            using (w.Block("foreach (var c in items)"))
            {
                var localNames = new List<(string localName, string paramName, string kind)>();

                foreach (var cc in child.ContainerChildren)
                {
                    var localName = "new" + cc.ParameterName;
                    switch (cc.Kind)
                    {
                        case ContainerChildKind.Direct:
                            w.AppendLine($"var {localName} = Visit(c.{cc.ParameterName});");
                            localNames.Add((localName, cc.ParameterName, "ref"));
                            break;
                        case ContainerChildKind.Nullable:
                            w.AppendLine($"var {localName} = c.{cc.ParameterName} != null ? Visit(c.{cc.ParameterName}) : null;");
                            localNames.Add((localName, cc.ParameterName, "ref"));
                            break;
                        case ContainerChildKind.Array:
                        {
                            var changedVar = cc.ParameterName + "Changed";
                            w.AppendLine($"var {changedVar} = false;");
                            w.AppendLine($"var {localName} = c.{cc.ParameterName};");
                            using (w.Block($"if (!c.{cc.ParameterName}.IsDefaultOrEmpty)"))
                            {
                                w.AppendLine($"{localName} = RewriteImmutableArray(c.{cc.ParameterName}, out {changedVar});");
                            }
                            localNames.Add((localName, cc.ParameterName, "changed"));
                            break;
                        }
                        case ContainerChildKind.NullableArray:
                        {
                            var changedVar = cc.ParameterName + "Changed";
                            w.AppendLine($"var {changedVar} = false;");
                            w.AppendLine($"var {localName} = c.{cc.ParameterName};");
                            using (w.Block($"if (c.{cc.ParameterName} != null)"))
                            {
                                w.AppendLine($"{localName} = RewriteImmutableArray(c.{cc.ParameterName}.Value, out {changedVar});");
                            }
                            localNames.Add((localName, cc.ParameterName, "changed"));
                            break;
                        }
                    }
                }

                // Build change condition
                var conditions = new List<string>();
                foreach (var (localName, paramName, kind) in localNames)
                {
                    if (kind == "ref")
                        conditions.Add($"!ReferenceEquals({localName}, c.{paramName})");
                    else
                        conditions.Add($"{paramName}Changed");
                }

                var anyChanged = string.Join(" || ", conditions);
                using (w.Block($"if ({anyChanged})"))
                {
                    EmitContainerReconstruction(w, child.ContainerTypeFullName, child.ContainerChildren, localNames);
                    w.AppendLine("changed = true;");
                }
                using (w.Block("else"))
                {
                    w.AppendLine("builder.Add(c);");
                }
            }

            w.AppendLine("return changed ? builder.MoveToImmutable() : items;");
        }
    }

    private static void EmitContainerReconstruction(
        SourceWriter w, string containerTypeFull,
        ImmutableArray<ContainerChildInfo> containerChildren,
        List<(string localName, string paramName, string kind)> localNames)
    {
        var assignments = string.Join(", ", localNames.Select(l => $"{l.paramName} = {l.localName}"));
        w.AppendLine($"builder.Add(c with {{ {assignments} }});");
    }

    private static void EmitPolymorphicContainerRewriteHelper(SourceWriter w, ChildInfo child)
    {
        var baseShort = GetShortName(child.ContainerTypeFullName);
        var helperName = "Rewrite" + baseShort + "s";

        using (w.Block($"private ImmutableArray<{child.ContainerTypeFullName}> {helperName}(ImmutableArray<{child.ContainerTypeFullName}> items, out bool changed)"))
        {
            w.AppendLine("changed = false;");
            w.AppendLine($"var builder = ImmutableArray.CreateBuilder<{child.ContainerTypeFullName}>(items.Length);");

            using (w.Block("foreach (var item in items)"))
            {
                foreach (var sub in child.Subtypes)
                {
                    var subShort = GetShortName(sub.TypeFullName);
                    var localVar = ToLocalName(subShort);

                    using (w.Block($"if (item is {sub.TypeFullName} {localVar})"))
                    {
                        var localNames = new List<(string localName, string paramName, string kind)>();

                        foreach (var cc in sub.Children)
                        {
                            var localName = "new" + cc.ParameterName;
                            switch (cc.Kind)
                            {
                                case ContainerChildKind.Direct:
                                    w.AppendLine($"var {localName} = Visit({localVar}.{cc.ParameterName});");
                                    localNames.Add((localName, cc.ParameterName, "ref"));
                                    break;
                                case ContainerChildKind.Nullable:
                                    w.AppendLine($"var {localName} = {localVar}.{cc.ParameterName} != null ? Visit({localVar}.{cc.ParameterName}) : null;");
                                    localNames.Add((localName, cc.ParameterName, "ref"));
                                    break;
                                case ContainerChildKind.Array:
                                    w.AppendLine($"var {localName} = RewriteImmutableArray({localVar}.{cc.ParameterName}, out var {cc.ParameterName}Changed);");
                                    localNames.Add((localName, cc.ParameterName, "changed"));
                                    break;
                            }
                        }

                        var conditions = new List<string>();
                        foreach (var (localName, paramName, kind) in localNames)
                        {
                            if (kind == "ref")
                                conditions.Add($"!ReferenceEquals({localName}, {localVar}.{paramName})");
                            else
                                conditions.Add($"{paramName}Changed");
                        }

                        var anyChanged = string.Join(" || ", conditions);
                        using (w.Block($"if ({anyChanged})"))
                        {
                            var assignments = string.Join(", ", localNames.Select(l => $"{l.paramName} = {l.localName}"));
                            w.AppendLine($"builder.Add({localVar} with {{ {assignments} }});");
                            w.AppendLine("changed = true;");
                        }
                        using (w.Block("else"))
                        {
                            w.AppendLine("builder.Add(item);");
                        }
                        w.AppendLine("continue;");
                    }
                }

                w.AppendLine("builder.Add(item);");
            }

            w.AppendLine("return changed ? builder.MoveToImmutable() : items;");
        }
    }

    private static string GetShortName(string fullName)
    {
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;
    }

    private static string ToLocalName(string typeName)
    {
        if (typeName.Length == 0) return typeName;
        return char.ToLowerInvariant(typeName[0]) + typeName.Substring(1);
    }
}
