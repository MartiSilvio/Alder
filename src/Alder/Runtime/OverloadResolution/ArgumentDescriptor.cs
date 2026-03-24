namespace Alder.Runtime;

internal enum ArgumentKind : byte
{
    Value,
    Out,
    Lambda,
    Null
}

internal readonly struct ArgumentDescriptor
{
    public Type? StaticType { get; }
    public string? Name { get; }
    public ArgumentKind Kind { get; }
    public int LambdaArity { get; }
    public object? RuntimeValue { get; }

    private ArgumentDescriptor(Type? staticType, string? name, ArgumentKind kind, int lambdaArity, object? runtimeValue)
    {
        StaticType = staticType;
        Name = name;
        Kind = kind;
        LambdaArity = lambdaArity;
        RuntimeValue = runtimeValue;
    }

    public static ArgumentDescriptor[] FromArgs(object?[] args)
    {
        var result = new ArgumentDescriptor[args.Length];
        for (var i = 0; i < args.Length; i++)
            result[i] = FromSingleArg(args[i]);
        return result;
    }

    public static ArgumentDescriptor[] FromTypes(Type[] types)
    {
        var result = new ArgumentDescriptor[types.Length];
        for (var i = 0; i < types.Length; i++)
            result[i] = new ArgumentDescriptor(types[i], null, ArgumentKind.Value, -1, null);
        return result;
    }

    internal static ArgumentDescriptor ForTest(ArgumentKind kind, Type? staticType, string? name, int lambdaArity) =>
        new(staticType, name, kind, lambdaArity, null);

    private static ArgumentDescriptor FromSingleArg(object? arg)
    {
        switch (arg)
        {
            case null:
                return new ArgumentDescriptor(null, null, ArgumentKind.Null, -1, null);

            case NamedArg n:
                var inner = FromSingleArg(n.Value);
                return new ArgumentDescriptor(inner.StaticType, n.Name, inner.Kind, inner.LambdaArity, n.Value);

            case OutArgMarker:
                return new ArgumentDescriptor(null, null, ArgumentKind.Out, -1, arg);

            case LambdaValue lv:
                return new ArgumentDescriptor(null, null, ArgumentKind.Lambda, lv.Parameters.Count, lv);

            case CompiledLambdaValue cv:
                return new ArgumentDescriptor(null, null, ArgumentKind.Lambda, cv.Parameters.Count, cv);

            default:
                return new ArgumentDescriptor(arg.GetType(), null, ArgumentKind.Value, -1, arg);
        }
    }
}
