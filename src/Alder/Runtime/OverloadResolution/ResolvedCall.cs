using System.Collections.Immutable;
using System.Reflection;

namespace Alder.Runtime;

internal enum ArgumentConversionKind : byte
{
    Identity,
    ImplicitReference,
    ImplicitNumeric,
    UserDefined,
    LambdaToDelegate,
    Boxing
}

internal readonly struct ArgumentConversion
{
    public ArgumentConversionKind Kind { get; }
    public Type TargetType { get; }

    private ArgumentConversion(ArgumentConversionKind kind, Type targetType)
    {
        Kind = kind;
        TargetType = targetType;
    }

    public bool IsIdentity => Kind == ArgumentConversionKind.Identity;

    public static ArgumentConversion ForIdentity(Type type) =>
        new(ArgumentConversionKind.Identity, type);

    public static ArgumentConversion ForImplicitReference(Type targetType) =>
        new(ArgumentConversionKind.ImplicitReference, targetType);

    public static ArgumentConversion ForImplicitNumeric(Type targetType) =>
        new(ArgumentConversionKind.ImplicitNumeric, targetType);

    public static ArgumentConversion ForUserDefined(Type targetType) =>
        new(ArgumentConversionKind.UserDefined, targetType);

    public static ArgumentConversion ForLambdaToDelegate(Type delegateType) =>
        new(ArgumentConversionKind.LambdaToDelegate, delegateType);

    public static ArgumentConversion ForBoxing(Type targetType) =>
        new(ArgumentConversionKind.Boxing, targetType);
}

internal readonly struct ResolvedCall
{
    public MethodInfo Method { get; }
    public ArgumentParameterMap ArgMap { get; }
    public ApplicableForm Form { get; }
    public ImmutableArray<ArgumentConversion> Conversions { get; }

    public ResolvedCall(
        MethodInfo method,
        ArgumentParameterMap argMap,
        ApplicableForm form,
        ImmutableArray<ArgumentConversion> conversions)
    {
        Method = method;
        ArgMap = argMap;
        Form = form;
        Conversions = conversions;
    }
}

internal readonly struct ResolvedConstructorCall
{
    public ConstructorInfo Constructor { get; }
    public ArgumentParameterMap ArgMap { get; }
    public ApplicableForm Form { get; }
    public ImmutableArray<ArgumentConversion> Conversions { get; }

    public ResolvedConstructorCall(
        ConstructorInfo constructor,
        ArgumentParameterMap argMap,
        ApplicableForm form,
        ImmutableArray<ArgumentConversion> conversions)
    {
        Constructor = constructor;
        ArgMap = argMap;
        Form = form;
        Conversions = conversions;
    }
}
