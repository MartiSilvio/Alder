using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Alder.Generators.Tests;

[TestFixture]
public class DynamicLinqOperatorCatalogGenerationTests
{
    [Test]
    public void DynamicLinqGenerator_UsesStructuralIndentation()
    {
        var source = File.ReadAllText(FindRepoFile("src/Alder.Generators/DynamicLinqOperatorCatalogGenerator.cs"));

        Assert.That(source, Does.Not.Contain("\"    "));
    }

    [Test]
    public void DispatcherBackedJoinWrappers_UseOuterSourceTypeParameter()
    {
        var source = """
            using System;

            namespace Alder.Compiled.DynamicLinq;

            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            internal sealed class DynamicLinqOperatorAttribute : Attribute
            {
                internal DynamicLinqOperatorAttribute(string extensionName) { }
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
                    string firstExpressionParameter) { }

                public string SecondarySourceType { get; set; } = "";
                public string SecondarySourceName { get; set; } = "inner";
                public string SecondExpressionParameter { get; set; } = "";
                public string ThirdExpressionParameter { get; set; } = "";
                public bool IncludeEngineOverload { get; set; }
                public bool IncludeTypedResultOverload { get; set; }
                public int GenericArity { get; set; } = 1;
                public string SortDirection { get; set; } = "";
            }

            [DynamicLinqOperator("Join", Sources = "Enumerable|Queryable", UntypedResults = "Sequence", DispatcherOperator = "Join", ProbeType = "String")]
            [DynamicLinqOperator("GroupJoin", Sources = "Enumerable|Queryable", UntypedResults = "Sequence", DispatcherOperator = "GroupJoin", ProbeType = "String")]
            [DynamicLinqDispatcherExtension("JoinDynamic", "Join", "IEnumerable", "IEnumerableOfT", "outerKeySelector", SecondarySourceType = "IEnumerableOfTSecond", SecondExpressionParameter = "innerKeySelector", ThirdExpressionParameter = "resultSelector", IncludeEngineOverload = true, GenericArity = 2)]
            [DynamicLinqDispatcherExtension("JoinDynamic", "Join", "IQueryable", "IQueryableOfT", "outerKeySelector", SecondarySourceType = "IEnumerableOfTSecond", SecondExpressionParameter = "innerKeySelector", ThirdExpressionParameter = "resultSelector", IncludeEngineOverload = true, GenericArity = 2)]
            [DynamicLinqDispatcherExtension("GroupJoinDynamic", "GroupJoin", "IEnumerable", "IEnumerableOfT", "outerKeySelector", SecondarySourceType = "IEnumerableOfTSecond", SecondExpressionParameter = "innerKeySelector", ThirdExpressionParameter = "resultSelector", IncludeEngineOverload = true, GenericArity = 2)]
            [DynamicLinqDispatcherExtension("GroupJoinDynamic", "GroupJoin", "IQueryable", "IQueryableOfT", "outerKeySelector", SecondarySourceType = "IEnumerableOfTSecond", SecondExpressionParameter = "innerKeySelector", ThirdExpressionParameter = "resultSelector", IncludeEngineOverload = true, GenericArity = 2)]
            internal static class DynamicLinqOperatorDefinitions
            {
            }
            """;

        var generated = RunGenerator(source);

        Assert.That(generated, Does.Contain("JoinDynamic<TOuter, TInner>(this IEnumerable<TOuter> source"));
        Assert.That(generated, Does.Contain("JoinDynamic<TOuter, TInner>(this IQueryable<TOuter> source"));
        Assert.That(generated, Does.Contain("GroupJoinDynamic<TOuter, TInner>(this IEnumerable<TOuter> source"));
        Assert.That(generated, Does.Contain("GroupJoinDynamic<TOuter, TInner>(this IQueryable<TOuter> source"));
        Assert.That(generated, Does.Not.Contain("this IEnumerable<T> source"));
        Assert.That(generated, Does.Not.Contain("this IQueryable<T> source"));
    }

    [Test]
    public void TypedStringWrappers_AreGeneratedFromOperatorMetadata()
    {
        var source = """
            using System;

            namespace Alder.Compiled.DynamicLinq;

            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            internal sealed class DynamicLinqOperatorAttribute : Attribute
            {
                internal DynamicLinqOperatorAttribute(string extensionName) { }
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
                    string firstExpressionParameter) { }

                public string SecondarySourceType { get; set; } = "";
                public string SecondarySourceName { get; set; } = "inner";
                public string SecondExpressionParameter { get; set; } = "";
                public string ThirdExpressionParameter { get; set; } = "";
                public bool IncludeEngineOverload { get; set; }
                public bool IncludeTypedResultOverload { get; set; }
                public int GenericArity { get; set; } = 1;
                public string SortDirection { get; set; } = "";
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
                    string firstExpressionParameter) { }

                public string SecondarySourceType { get; set; } = "";
                public string SecondarySourceName { get; set; } = "inner";
                public string SecondExpressionParameter { get; set; } = "";
                public string ThirdExpressionParameter { get; set; } = "";
                public bool IncludeEngineOverload { get; set; }
                public int GenericArity { get; set; } = 1;
                public string SortDirection { get; set; } = "";
                public string Sources { get; set; } = "";
            }

            [DynamicLinqOperator("Where", Sources = "Enumerable|Queryable")]
            [DynamicLinqOperator("Select", Sources = "Enumerable|Queryable", UntypedResults = "Sequence", DispatcherOperator = "Select", ProbeType = "String")]
            [DynamicLinqOperator("OrderBy", Sources = "Enumerable|Queryable", DispatcherOperator = "OrderBy", ProbeType = "String")]
            [DynamicLinqOperator("GroupBy", Sources = "Enumerable|Queryable", DispatcherOperator = "GroupBy", ProbeType = "String")]
            [DynamicLinqOperator("Join", Sources = "Enumerable|Queryable", UntypedResults = "Sequence", DispatcherOperator = "Join", ProbeType = "String")]
            [DynamicLinqTypedStringExtension("WhereDynamic", "Where", "SequenceOfT", "SequenceOfT", "Predicate", "predicate", IncludeEngineOverload = true, Sources = "Enumerable|Queryable")]
            [DynamicLinqTypedStringExtension("SelectDynamic", "Select", "SequenceOfTResult", "SequenceOfT", "MaterializingSelector", "selector", IncludeEngineOverload = true, Sources = "Enumerable|Queryable")]
            [DynamicLinqTypedStringExtension("OrderByDynamic", "OrderBy", "OrderedSequenceOfT", "SequenceOfT", "Selector", "keySelector", IncludeEngineOverload = true, SortDirection = "Ascending", Sources = "Enumerable|Queryable")]
            [DynamicLinqTypedStringExtension("GroupByDynamic", "GroupBy", "SequenceOfGrouping", "SequenceOfT", "Grouping", "keySelector", IncludeEngineOverload = true, Sources = "Enumerable|Queryable")]
            [DynamicLinqTypedStringExtension("JoinDynamic", "Join", "SequenceOfTResult", "SequenceOfT", "Join", "outerKeySelector", SecondarySourceType = "IEnumerableOfTSecond", SecondExpressionParameter = "innerKeySelector", ThirdExpressionParameter = "resultSelector", IncludeEngineOverload = true, GenericArity = 2, Sources = "Enumerable|Queryable")]
            internal static class DynamicLinqOperatorDefinitions
            {
            }
            """;

        var generated = RunGenerator(source);

        Assert.That(generated, Does.Contain("public static IEnumerable<T> WhereDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)"));
        Assert.That(generated, Does.Contain("public static IQueryable<T> WhereDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)"));
        Assert.That(generated, Does.Contain("public static IEnumerable<TResult> SelectDynamic<T, TResult>(this IEnumerable<T> source, string selector, params object?[] variables)"));
        Assert.That(generated, Does.Contain("public static IOrderedEnumerable<T> OrderByDynamic<T, TKey>(this IEnumerable<T> source, string keySelector, params object?[] variables)"));
        Assert.That(generated, Does.Contain("public static IEnumerable<IGrouping<TKey, T>> GroupByDynamic<T, TKey>(this IEnumerable<T> source, string keySelector, params object?[] variables)"));
        Assert.That(generated, Does.Contain("public static IQueryable<IGrouping<TKey, T>> GroupByDynamic<T, TKey>(this IQueryable<T> source, AlderEngine engine, string keySelector, params object?[] variables)"));
        Assert.That(generated, Does.Contain("public static IEnumerable<TResult> JoinDynamic<TOuter, TInner, TKey, TResult>("));
        Assert.That(generated, Does.Contain("this IQueryable<TOuter> outer,"));
        Assert.That(generated, Does.Contain(".ParsePredicate<T>(predicate"));
        Assert.That(generated, Does.Contain(".ParseSelector<T, TResult>(selector"));
        Assert.That(generated, Does.Contain(".ParseSelector<T, TKey>(keySelector"));
        Assert.That(generated, Does.Contain(".ParseLambda("));
        Assert.That(generated, Does.Not.Contain("private static Expression<Func<T, bool>> ParsePredicate"));
        Assert.That(generated, Does.Not.Contain("CompilePredicate<T>("));
        Assert.That(generated, Does.Not.Contain("private static Expression<Func<T, TResult>> ParseSelector"));
        Assert.That(generated, Does.Not.Contain("CompileSelector<T,"));
        Assert.That(generated, Does.Not.Contain("ParseBinaryLambda"));
        Assert.That(generated, Does.Not.Contain("CompileBinaryLambda"));
    }

    [Test]
    public void ForwardingWrappers_AreGeneratedFromSourceMetadata()
    {
        var source = """
            using System;

            namespace Alder.Compiled.DynamicLinq;

            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            internal sealed class DynamicLinqOperatorAttribute : Attribute
            {
                internal DynamicLinqOperatorAttribute(string extensionName) { }
                public string Sources { get; set; } = "";
                public string UntypedResults { get; set; } = "";
                public string DispatcherOperator { get; set; } = "";
                public string ProbeType { get; set; } = "";
            }

            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            internal sealed class DynamicLinqForwardingExtensionAttribute : Attribute
            {
                internal DynamicLinqForwardingExtensionAttribute(
                    string extensionMethodName,
                    string linqMethodName,
                    string returnType,
                    string sourceType,
                    string genericParameters) { }

                public string Sources { get; set; } = "";
                public string SecondarySourceType { get; set; } = "";
                public string SecondarySourceName { get; set; } = "second";
                public string ValueParameterType { get; set; } = "";
                public string ValueParameterName { get; set; } = "";
            }

            [DynamicLinqOperator("Skip", Sources = "Enumerable|Queryable")]
            [DynamicLinqOperator("Concat", Sources = "Enumerable|Queryable")]
            [DynamicLinqOperator("Contains", Sources = "Enumerable|Queryable")]
            [DynamicLinqForwardingExtension("SkipDynamic", "Skip", "SequenceOfT", "SequenceOfT", "T", Sources = "Enumerable|Queryable", ValueParameterType = "Int32", ValueParameterName = "count")]
            [DynamicLinqForwardingExtension("ConcatDynamic", "Concat", "SequenceOfT", "SequenceOfT", "T", Sources = "Enumerable|Queryable", SecondarySourceType = "SequenceOfT")]
            [DynamicLinqForwardingExtension("ContainsDynamic", "Contains", "Boolean", "SequenceOfT", "T", Sources = "Enumerable|Queryable", ValueParameterType = "T", ValueParameterName = "value")]
            internal static class DynamicLinqOperatorDefinitions
            {
            }
            """;

        var generated = RunGenerator(source);

        Assert.That(generated, Does.Contain("public static IEnumerable<T> SkipDynamic<T>(this IEnumerable<T> source, int count)"));
        Assert.That(generated, Does.Contain("return source.Skip<T>(count);"));
        Assert.That(generated, Does.Contain("public static IQueryable<T> SkipDynamic<T>(this IQueryable<T> source, int count)"));
        Assert.That(generated, Does.Contain("public static IEnumerable<T> ConcatDynamic<T>(this IEnumerable<T> source, IEnumerable<T> second)"));
        Assert.That(generated, Does.Contain("public static IQueryable<T> ConcatDynamic<T>(this IQueryable<T> source, IQueryable<T> second)"));
        Assert.That(generated, Does.Contain("public static bool ContainsDynamic<T>(this IEnumerable<T> source, T value)"));
        Assert.That(generated, Does.Contain("public static bool ContainsDynamic<T>(this IQueryable<T> source, T value)"));
    }

    [Test]
    public void LambdaForwardingWrappers_AreGeneratedFromSourceMetadata()
    {
        var source = """
            using System;

            namespace Alder.Compiled.DynamicLinq;

            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            internal sealed class DynamicLinqOperatorAttribute : Attribute
            {
                internal DynamicLinqOperatorAttribute(string extensionName) { }
                public string Sources { get; set; } = "";
                public string UntypedResults { get; set; } = "";
                public string DispatcherOperator { get; set; } = "";
                public string ProbeType { get; set; } = "";
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
                    string lambdaParameterName) { }

                public string Sources { get; set; } = "";
            }

            [DynamicLinqOperator("Where", Sources = "Enumerable|Queryable")]
            [DynamicLinqOperator("Select", Sources = "Enumerable|Queryable")]
            [DynamicLinqLambdaForwardingExtension("WhereDynamic", "Where", "SequenceOfT", "SequenceOfT", "T", "ExpressionPredicate", "predicateExpr", Sources = "Enumerable|Queryable")]
            [DynamicLinqLambdaForwardingExtension("WhereDynamic", "Where", "IEnumerableOfT", "IEnumerableOfT", "T", "FuncPredicate", "predicate", Sources = "Enumerable")]
            [DynamicLinqLambdaForwardingExtension("SelectDynamic", "Select", "SequenceOfTResult", "SequenceOfT", "T, TResult", "ExpressionSelector", "selectorExpr", Sources = "Enumerable|Queryable")]
            [DynamicLinqLambdaForwardingExtension("SelectDynamic", "Select", "IEnumerableOfTResult", "IEnumerableOfT", "T, TResult", "FuncSelector", "selector", Sources = "Enumerable")]
            internal static class DynamicLinqOperatorDefinitions
            {
            }
            """;

        var generated = RunGenerator(source);

        Assert.That(generated, Does.Contain("public static IEnumerable<T> WhereDynamic<T>(this IEnumerable<T> source, Expression<Func<T, bool>> predicateExpr)"));
        Assert.That(generated, Does.Contain("return source.Where<T>(CompilePredicate(predicateExpr));"));
        Assert.That(generated, Does.Contain("public static IEnumerable<T> WhereDynamic<T>(this IEnumerable<T> source, DynamicQueryPlan plan)"));
        Assert.That(generated, Does.Contain("return source.Where<T>(plan.Compile<Func<T, bool>>());"));
        Assert.That(generated, Does.Contain("public static IQueryable<T> WhereDynamic<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicateExpr)"));
        Assert.That(generated, Does.Contain("public static IQueryable<T> WhereDynamic<T>(this IQueryable<T> source, DynamicQueryPlan plan)"));
        Assert.That(generated, Does.Contain("return source.Where<T>(plan.ToExpression<Func<T, bool>>());"));
        Assert.That(generated, Does.Contain("ArgumentNullException.ThrowIfNull(predicateExpr);"));
        Assert.That(generated, Does.Contain("public static IEnumerable<T> WhereDynamic<T>(this IEnumerable<T> source, Func<T, bool> predicate)"));
        Assert.That(generated, Does.Contain("return source.Where<T>(predicate);"));
        Assert.That(generated, Does.Contain("public static IEnumerable<TResult> SelectDynamic<T, TResult>(this IEnumerable<T> source, Expression<Func<T, TResult>> selectorExpr)"));
        Assert.That(generated, Does.Contain("return source.Select<T, TResult>(CompileSelector(selectorExpr));"));
        Assert.That(generated, Does.Contain("public static IEnumerable<TResult> SelectDynamic<T, TResult>(this IEnumerable<T> source, DynamicQueryPlan plan)"));
        Assert.That(generated, Does.Contain("return source.Select<T, TResult>(plan.Compile<Func<T, TResult>>());"));
        Assert.That(generated, Does.Contain("public static IQueryable<TResult> SelectDynamic<T, TResult>(this IQueryable<T> source, Expression<Func<T, TResult>> selectorExpr)"));
    }

    [Test]
    public void AsyncWrappers_AreGeneratedFromSourceMetadata()
    {
        var source = """
            using System;

            namespace Alder.Compiled.DynamicLinq;

            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            internal sealed class DynamicLinqOperatorAttribute : Attribute
            {
                internal DynamicLinqOperatorAttribute(string extensionName) { }
                public string Sources { get; set; } = "";
                public string UntypedResults { get; set; } = "";
                public string DispatcherOperator { get; set; } = "";
                public string ProbeType { get; set; } = "";
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
                    string firstExpressionParameter) { }

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
                    string genericParameters) { }

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
                    string lambdaParameterName) { }

                public string Sources { get; set; } = "";
            }

            [DynamicLinqOperator("Where", Sources = "Async")]
            [DynamicLinqTypedStringExtension("WhereDynamic", "Where", "SequenceOfT", "SequenceOfT", "Predicate", "predicate", IncludeEngineOverload = true, Sources = "Async")]
            [DynamicLinqTypedStringExtension("SelectDynamic", "Select", "SequenceOfTResult", "SequenceOfT", "MaterializingSelector", "selector", IncludeEngineOverload = true, Sources = "Async")]
            [DynamicLinqTypedStringExtension("CountDynamic", "Count", "Int32", "SequenceOfT", "Predicate", "predicate", IncludeEngineOverload = true, Sources = "Async")]
            [DynamicLinqForwardingExtension("SkipDynamic", "Skip", "SequenceOfT", "SequenceOfT", "T", Sources = "Async", ValueParameterType = "Int32", ValueParameterName = "count")]
            [DynamicLinqLambdaForwardingExtension("WhereDynamic", "Where", "SequenceOfT", "SequenceOfT", "T", "FuncPredicate", "predicate", Sources = "Async")]
            internal static class DynamicLinqOperatorDefinitions
            {
            }
            """;

        var generated = RunGenerator(source);

        Assert.That(generated, Does.Contain("public static IAsyncEnumerable<T> WhereDynamic<T>(this IAsyncEnumerable<T> source, string predicate, params object?[] variables)"));
        Assert.That(generated, Does.Contain("public static IAsyncEnumerable<T> WhereDynamic<T>(this IAsyncEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)"));
        Assert.That(generated, Does.Contain("public static IAsyncEnumerable<TResult> SelectDynamic<T, TResult>(this IAsyncEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)"));
        Assert.That(generated, Does.Contain("public static ValueTask<int> CountDynamic<T>(this IAsyncEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)"));
        Assert.That(generated, Does.Contain("public static IAsyncEnumerable<T> SkipDynamic<T>(this IAsyncEnumerable<T> source, int count)"));
        Assert.That(generated, Does.Contain("public static IAsyncEnumerable<T> WhereDynamic<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)"));
        Assert.That(generated, Does.Contain("return AsyncWhereCore(source, ValidateEngine(engine).ParsePredicate<T>(predicate, BuildOrderedValues(variables)).Compile<Func<T, bool>>());"));
    }

    [Test]
    public void DispatcherFacades_AreGeneratedFromDispatcherMetadata()
    {
        var source = """
            using System;

            namespace Alder.Compiled.DynamicLinq;

            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            internal sealed class DynamicLinqOperatorAttribute : Attribute
            {
                internal DynamicLinqOperatorAttribute(string extensionName) { }
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
                    string firstExpressionParameter) { }

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

            [DynamicLinqOperator("Select", Sources = "Enumerable", UntypedResults = "Sequence", DispatcherOperator = "Select", ProbeType = "String")]
            [DynamicLinqOperator("Sum", Sources = "Enumerable", UntypedResults = "Scalar", DispatcherOperator = "Sum", ProbeType = "Decimal")]
            [DynamicLinqDispatcherExtension("SelectDynamic", "Select", "IEnumerable", "IEnumerableOfT", "selector")]
            [DynamicLinqDispatcherExtension("SumDynamic", "Sum", "Object", "IEnumerableOfT", "selector")]
            internal static class DynamicLinqOperatorDefinitions
            {
            }
            """;

        var generated = RunGenerator(source);

        Assert.That(generated, Does.Contain("internal static object Select<T>("));
        Assert.That(generated, Does.Contain("ApplySelectOperator("));
        Assert.That(generated, Does.Contain("internal static object Sum<T>("));
        Assert.That(generated, Does.Contain("DynamicQueryOperatorKind.Sum"));
        Assert.That(generated, Does.Contain("DynamicQueryLambdaKind.AggregateSelector"));
    }

    [Test]
    public void UnsupportedAsyncDispatcherMetadata_ReportsDiagnostic()
    {
        var source = """
            using System;

            namespace Alder.Compiled.DynamicLinq;

            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            internal sealed class DynamicLinqOperatorAttribute : Attribute
            {
                internal DynamicLinqOperatorAttribute(string extensionName) { }
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
                    string firstExpressionParameter) { }

                public string Sources { get; set; } = "";
            }

            [DynamicLinqOperator("OrderBy", Sources = "Async", DispatcherOperator = "OrderBy", ProbeType = "String")]
            [DynamicLinqDispatcherExtension("OrderByDynamic", "OrderBy", "Object", "SequenceOfT", "keySelector", Sources = "Async")]
            internal static class DynamicLinqOperatorDefinitions
            {
            }
            """;

        var result = RunGeneratorResult(source);

        Assert.That(
            result.Diagnostics.Select(static diagnostic => diagnostic.Id),
            Does.Contain("ALDRDL001"));
        Assert.That(
            result.Diagnostics.Select(static diagnostic => diagnostic.GetMessage()),
            Has.Some.Contains("Unsupported async dispatcher method 'OrderBy'."));
    }

    private static string RunGenerator(string source)
        => string.Join(
            "\n",
            RunGeneratorResult(source).GeneratedTrees.Select(static tree => tree.GetText().ToString()));

    private static GeneratorDriverRunResult RunGeneratorResult(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>()
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "DynamicLinqGeneratorTest",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new DynamicLinqOperatorCatalogGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult();
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }

        Assert.Fail("Could not find repository file '" + relativePath + "'.");
        return "";
    }
}
