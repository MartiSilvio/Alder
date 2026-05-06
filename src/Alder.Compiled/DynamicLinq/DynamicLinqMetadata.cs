using System;

namespace Alder.Compiled.DynamicLinq;

internal enum DynamicLinqProbeType
{
    None = 0,
    Boolean,
    Int32,
    Int64,
    Decimal,
    String,
    Object
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal sealed class DynamicLinqOperatorAttribute : Attribute
{
    internal DynamicLinqOperatorAttribute(string extensionName) => ExtensionName = extensionName;

    public string ExtensionName { get; }

    public string Sources { get; set; } = "";

    public string UntypedResults { get; set; } = "";

    public string DispatcherOperator { get; set; } = "";

    public string ProbeType { get; set; } = "";
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal sealed class DynamicLinqDispatcherExtensionAttribute : Attribute
{
    internal DynamicLinqDispatcherExtensionAttribute(
        string extensionMethodName,
        string dispatcherMethodName,
        string returnType,
        string sourceType,
        string firstExpressionParameter)
    {
        ExtensionMethodName = extensionMethodName;
        DispatcherMethodName = dispatcherMethodName;
        ReturnType = returnType;
        SourceType = sourceType;
        FirstExpressionParameter = firstExpressionParameter;
    }

    public string ExtensionMethodName { get; }

    public string DispatcherMethodName { get; }

    public string ReturnType { get; }

    public string SourceType { get; }

    public string FirstExpressionParameter { get; }

    public string SecondarySourceType { get; set; } = "";

    public string SecondarySourceName { get; set; } = "inner";

    public string SecondExpressionParameter { get; set; } = "";

    public string ThirdExpressionParameter { get; set; } = "";

    public bool IncludeEngineOverload { get; set; }

    public bool IncludeTypedResultOverload { get; set; }

    public int GenericArity { get; set; } = 1;

    public string SortDirection { get; set; } = "";

    public string Sources { get; set; } = "";
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal sealed class DynamicLinqTypedStringExtensionAttribute : Attribute
{
    internal DynamicLinqTypedStringExtensionAttribute(
        string extensionMethodName,
        string linqMethodName,
        string returnType,
        string sourceType,
        string lambdaKind,
        string firstExpressionParameter)
    {
        ExtensionMethodName = extensionMethodName;
        LinqMethodName = linqMethodName;
        ReturnType = returnType;
        SourceType = sourceType;
        LambdaKind = lambdaKind;
        FirstExpressionParameter = firstExpressionParameter;
    }

    public string ExtensionMethodName { get; }

    public string LinqMethodName { get; }

    public string ReturnType { get; }

    public string SourceType { get; }

    public string LambdaKind { get; }

    public string FirstExpressionParameter { get; }

    public string SecondarySourceType { get; set; } = "";

    public string SecondarySourceName { get; set; } = "inner";

    public string SecondExpressionParameter { get; set; } = "";

    public string ThirdExpressionParameter { get; set; } = "";

    public bool IncludeEngineOverload { get; set; }

    public int GenericArity { get; set; } = 1;

    public string SortDirection { get; set; } = "";

    public string Sources { get; set; } = "";
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal sealed class DynamicLinqForwardingExtensionAttribute : Attribute
{
    internal DynamicLinqForwardingExtensionAttribute(
        string extensionMethodName,
        string linqMethodName,
        string returnType,
        string sourceType,
        string genericParameters)
    {
        ExtensionMethodName = extensionMethodName;
        LinqMethodName = linqMethodName;
        ReturnType = returnType;
        SourceType = sourceType;
        GenericParameters = genericParameters;
    }

    public string ExtensionMethodName { get; }

    public string LinqMethodName { get; }

    public string ReturnType { get; }

    public string SourceType { get; }

    public string GenericParameters { get; }

    public string Sources { get; set; } = "";

    public string SecondarySourceType { get; set; } = "";

    public string SecondarySourceName { get; set; } = "second";

    public string ValueParameterType { get; set; } = "";

    public string ValueParameterName { get; set; } = "";

    public bool NullForgivingResult { get; set; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal sealed class DynamicLinqLambdaForwardingExtensionAttribute : Attribute
{
    internal DynamicLinqLambdaForwardingExtensionAttribute(
        string extensionMethodName,
        string linqMethodName,
        string returnType,
        string sourceType,
        string genericParameters,
        string lambdaKind,
        string lambdaParameterName)
    {
        ExtensionMethodName = extensionMethodName;
        LinqMethodName = linqMethodName;
        ReturnType = returnType;
        SourceType = sourceType;
        GenericParameters = genericParameters;
        LambdaKind = lambdaKind;
        LambdaParameterName = lambdaParameterName;
    }

    public string ExtensionMethodName { get; }

    public string LinqMethodName { get; }

    public string ReturnType { get; }

    public string SourceType { get; }

    public string GenericParameters { get; }

    public string LambdaKind { get; }

    public string LambdaParameterName { get; }

    public string Sources { get; set; } = "";
}
