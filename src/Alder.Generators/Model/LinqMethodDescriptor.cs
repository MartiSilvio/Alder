namespace Alder.Generators.Model;

internal enum LinqMethodShape
{
    Parameterless,
    Filter,
    Projection,
    TwoSequence,
    IntArg,
    ValueArg,
    ScalarAggregate,
    SeedAggregate,
    Generation,
    OrderedProjection,
    SelectMany
}

internal sealed class LinqMethodDescriptor
{
    public string Name { get; }
    public LinqMethodShape Shape { get; }
    public bool AlsoParameterless { get; }
    public bool AlsoFilter { get; }
    public bool NumericOnly { get; }

    public LinqMethodDescriptor(
        string name,
        LinqMethodShape shape,
        bool alsoParameterless = false,
        bool alsoFilter = false,
        bool numericOnly = false)
    {
        Name = name;
        Shape = shape;
        AlsoParameterless = alsoParameterless;
        AlsoFilter = alsoFilter;
        NumericOnly = numericOnly;
    }

    public static readonly LinqMethodDescriptor[] All =
    {
        // Filters
        new("Where", LinqMethodShape.Filter),
        new("SkipWhile", LinqMethodShape.Filter),
        new("TakeWhile", LinqMethodShape.Filter),

        // Projections
        new("Select", LinqMethodShape.Projection),
        new("OrderBy", LinqMethodShape.Projection),
        new("OrderByDescending", LinqMethodShape.Projection),
        new("GroupBy", LinqMethodShape.Projection),
        new("ToDictionary", LinqMethodShape.Projection),

        // Ordered projections
        new("ThenBy", LinqMethodShape.OrderedProjection),
        new("ThenByDescending", LinqMethodShape.OrderedProjection),

        // Parameterless
        new("ToList", LinqMethodShape.Parameterless),
        new("ToArray", LinqMethodShape.Parameterless),
        new("Distinct", LinqMethodShape.Parameterless),
        new("Reverse", LinqMethodShape.Parameterless),
        new("Min", LinqMethodShape.Parameterless),
        new("Max", LinqMethodShape.Parameterless),

        // Parameterless + filter combo
        new("Any", LinqMethodShape.Filter, alsoParameterless: true),
        new("All", LinqMethodShape.Filter),
        new("Count", LinqMethodShape.Parameterless, alsoFilter: true),
        new("First", LinqMethodShape.Parameterless, alsoFilter: true),
        new("Last", LinqMethodShape.Parameterless, alsoFilter: true),
        new("FirstOrDefault", LinqMethodShape.Parameterless, alsoFilter: true),
        new("LastOrDefault", LinqMethodShape.Parameterless, alsoFilter: true),
        new("Single", LinqMethodShape.Parameterless, alsoFilter: true),
        new("SingleOrDefault", LinqMethodShape.Parameterless, alsoFilter: true),

        // Aggregates
        new("Sum", LinqMethodShape.ScalarAggregate, numericOnly: true),
        new("Average", LinqMethodShape.ScalarAggregate, numericOnly: true),
        new("Aggregate", LinqMethodShape.SeedAggregate),

        // Two-sequence
        new("Concat", LinqMethodShape.TwoSequence),

        // Int-arg
        new("Skip", LinqMethodShape.IntArg),
        new("Take", LinqMethodShape.IntArg),
        new("ElementAt", LinqMethodShape.IntArg),

        // Value-arg
        new("Contains", LinqMethodShape.ValueArg),

        // Generation
        new("Repeat", LinqMethodShape.Generation),

        // SelectMany — unique shape
        new("SelectMany", LinqMethodShape.SelectMany),
    };
}
