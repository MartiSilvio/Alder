using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CsEval.Generators.Tests;

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

        var csEvalRef = MetadataReference.CreateFromFile(typeof(CsEvalRegisteredAttribute).Assembly.Location);
        if (!references.Any(r => r.Display == csEvalRef.Display))
            references.Add(csEvalRef);

        var compilation = CSharpCompilation.Create(
            "GeneratorTestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new CsEvalSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees;

        return (diagnostics, outputCompilation, generatedTrees);
    }
}
