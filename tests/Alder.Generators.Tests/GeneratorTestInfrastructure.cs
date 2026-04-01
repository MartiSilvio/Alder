using System.Collections.Immutable;
using Alder.Aot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Alder.Generators.Tests;

public static class GeneratorTestHelper
{
    public static (ImmutableArray<Diagnostic> Diagnostics, Microsoft.CodeAnalysis.Compilation OutputCompilation, ImmutableArray<SyntaxTree> GeneratedTrees)
        RunGenerator(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var references = System.AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var alderRef = MetadataReference.CreateFromFile(typeof(AlderRegisteredAttribute).Assembly.Location);
        if (!references.Any(r => r.Display == alderRef.Display))
            references.Add(alderRef);

        var compilation = CSharpCompilation.Create(
            "GeneratorTestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new AlderSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees;

        return (diagnostics, outputCompilation, generatedTrees);
    }
}
