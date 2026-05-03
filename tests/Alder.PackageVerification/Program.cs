using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: Alder.PackageVerification <path-to-nupkg-or-package-directory>");
    return 2;
}

try
{
    var packagePath = ResolvePackagePath(args[0]);
    var package = VerifyPackageContents(packagePath);
    VerifySymbolPackage(packagePath, package);
    VerifyLocalInstall(packagePath, package);
    Console.WriteLine($"Verified NuGet package {package.Id} {package.Version}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static string ResolvePackagePath(string input)
{
    var path = Path.GetFullPath(input);
    if (File.Exists(path))
        return path;

    if (!Directory.Exists(path))
        throw new InvalidOperationException($"Package path not found: {path}");

    var packages = Directory.GetFiles(path, "*.nupkg")
        .Where(static candidate => !candidate.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))
        .Order(StringComparer.Ordinal)
        .ToArray();

    return packages.Length switch
    {
        1 => packages[0],
        0 => throw new InvalidOperationException($"No .nupkg files found in package directory: {path}"),
        _ => throw new InvalidOperationException($"Expected one .nupkg file in package directory '{path}', found {packages.Length}."),
    };
}

static PackageInfo VerifyPackageContents(string packagePath)
{
    using var archive = ZipFile.OpenRead(packagePath);
    var entries = archive.Entries.Select(static entry => entry.FullName).ToHashSet(StringComparer.Ordinal);

    string[] requiredEntries =
    [
        "lib/net8.0/Alder.dll",
        "lib/netstandard2.0/Alder.dll",
        "lib/net8.0/Alder.Compiled.dll",
        "analyzers/dotnet/cs/Alder.Generators.dll",
        "README.md",
        "alder-icon.png",
    ];

    foreach (var entry in requiredEntries)
        Require(entries.Contains(entry), $"Package is missing required entry: {entry}");

    var nuspecEntry = archive.Entries.SingleOrDefault(static entry => entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
    Require(nuspecEntry is not null, "Package is missing a .nuspec file.");

    using var nuspecStream = nuspecEntry!.Open();
    var nuspec = XDocument.Load(nuspecStream);
    XNamespace ns = nuspec.Root?.Name.Namespace ?? XNamespace.None;
    var metadata = nuspec.Root?.Element(ns + "metadata")
        ?? throw new InvalidOperationException("Package nuspec is missing metadata.");

    var id = RequiredElementValue(metadata, ns, "id");
    var version = RequiredElementValue(metadata, ns, "version");

    Require(id == "Alder", $"Expected package id 'Alder' but found '{id}'.");
    Require(RequiredElementValue(metadata, ns, "authors") == "Silvio Martignetti", "Package authors metadata is incorrect.");
    Require(RequiredElementValue(metadata, ns, "title") == "Alder", "Package title metadata is incorrect.");
    var description = RequiredElementValue(metadata, ns, "description");
    RequireDescriptionContains(description, "C# runtime engine");
    RequireDescriptionContains(description, "compiler-style parsing");
    RequireDescriptionContains(description, "semantic binding");
    RequireDescriptionContains(description, "Dynamic LINQ");
    RequireDescriptionContains(description, "NativeAOT generated dispatch");
    Require(RequiredElementValue(metadata, ns, "readme") == "README.md", "Package readme metadata must point to README.md.");
    Require(RequiredElementValue(metadata, ns, "icon") == "alder-icon.png", "Package icon metadata must point to alder-icon.png.");
    Require(RequiredElementValue(metadata, ns, "releaseNotes") == "Initial 1.0.0 release.", "Package release notes metadata is incorrect.");
    Require(RequiredElementValue(metadata, ns, "copyright") == "Copyright © Silvio Martignetti", "Package copyright metadata is incorrect.");

    var tags = RequiredElementValue(metadata, ns, "tags")
        .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    string[] requiredTags =
    [
        "csharp",
        "c-sharp",
        "expressions",
        "expression-evaluator",
        "expression-parser",
        "scripting",
        "script-engine",
        "rules-engine",
        "dynamic-linq",
        "linq",
        "sandbox",
        "safe-evaluation",
        "nativeaot",
        "source-generator",
        "dotnet",
    ];

    foreach (var tag in requiredTags)
        Require(tags.Contains(tag), $"Package tags metadata is missing search tag: {tag}");

    var license = metadata.Element(ns + "license");
    Require(license is not null, "Package license metadata is missing.");
    Require((string?)license!.Attribute("type") == "expression" && license.Value == "MIT", "Package license must be the MIT SPDX expression.");

    var repository = metadata.Element(ns + "repository");
    Require(repository is not null, "Package repository metadata is missing.");
    Require((string?)repository!.Attribute("type") == "git", "Package repository type must be git.");
    Require(!string.IsNullOrWhiteSpace((string?)repository.Attribute("url")), "Package repository URL is missing.");

    return new PackageInfo(id, version);
}

static void RequireDescriptionContains(string description, string text)
    => Require(description.Contains(text, StringComparison.OrdinalIgnoreCase), $"Package description is missing search phrase: {text}");

static void VerifySymbolPackage(string packagePath, PackageInfo package)
{
    var packageDirectory = Path.GetDirectoryName(packagePath) ?? Directory.GetCurrentDirectory();
    var symbolPackagePath = Path.Combine(packageDirectory, $"{package.Id}.{package.Version}.snupkg");
    Require(File.Exists(symbolPackagePath), $"Symbol package not found: {symbolPackagePath}");

    using var archive = ZipFile.OpenRead(symbolPackagePath);
    var entries = archive.Entries.Select(static entry => entry.FullName).ToHashSet(StringComparer.Ordinal);

    string[] requiredEntries =
    [
        "lib/net8.0/Alder.pdb",
        "lib/netstandard2.0/Alder.pdb",
        "lib/net8.0/Alder.Compiled.pdb",
    ];

    foreach (var entry in requiredEntries)
        Require(entries.Contains(entry), $"Symbol package is missing required entry: {entry}");
}

static void VerifyLocalInstall(string packagePath, PackageInfo package)
{
    var packageDirectory = Path.GetDirectoryName(packagePath)
        ?? throw new InvalidOperationException("Package path has no parent directory.");
    var tempRoot = Path.Combine(Path.GetTempPath(), $"alder-package-verify-{Guid.NewGuid():N}");
    var appDirectory = Path.Combine(tempRoot, "Consumer");
    var packagesDirectory = Path.Combine(tempRoot, "packages");

    try
    {
        Directory.CreateDirectory(tempRoot);
        Run("dotnet", $"new console --framework net8.0 --output {Quote(appDirectory)} --no-restore", tempRoot);
        File.WriteAllText(Path.Combine(appDirectory, "NuGet.config"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="local" value="{EscapeXml(packageDirectory)}" />
              </packageSources>
            </configuration>
            """);

        File.WriteAllText(Path.Combine(appDirectory, "Program.cs"), """
            using Alder;
            using Alder.Aot;
            using Alder.Compiled;

            var engine = new AlderEngine();
            if (engine.Evaluate<int>("1 + 2") != 3)
                throw new InvalidOperationException("Interpreter smoke test failed.");

            var compiledEngine = new AlderEngine(options => options.UseCompiler());
            var compiled = compiledEngine.Compile<int>("1 + 2");
            if (compiled.Invoke() != 3)
                throw new InvalidOperationException("Compiled backend smoke test failed.");

            if (PackageSmokeContext.Default.GetTypeMetadata().Count == 0)
                throw new InvalidOperationException("AOT source generator smoke test failed.");

            Console.WriteLine("Alder package smoke test passed.");

            public sealed class PackageSmokeModel
            {
                public int Value { get; set; }
            }

            [AlderRegistered(typeof(PackageSmokeModel))]
            public partial class PackageSmokeContext : AlderTypeContext
            {
            }
            """);

        var environment = new Dictionary<string, string?>
        {
            ["NUGET_PACKAGES"] = packagesDirectory,
        };

        Run("dotnet", $"add package {package.Id} --version {package.Version} --source {Quote(packageDirectory)}", appDirectory, environment);
        Run("dotnet", "run --configuration Release --no-restore", appDirectory, environment);
    }
    finally
    {
        if (Directory.Exists(tempRoot))
            Directory.Delete(tempRoot, recursive: true);
    }
}

static string RequiredElementValue(XElement metadata, XNamespace ns, string name)
{
    var value = metadata.Element(ns + name)?.Value;
    Require(!string.IsNullOrWhiteSpace(value), $"Package nuspec is missing {name} metadata.");
    return value!;
}

static void Run(string fileName, string arguments, string workingDirectory, IReadOnlyDictionary<string, string?>? environment = null)
{
    var startInfo = new ProcessStartInfo(fileName, arguments)
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    if (environment is not null)
    {
        foreach (var (key, value) in environment)
            startInfo.Environment[key] = value;
    }

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Failed to start process: {fileName}");

    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode == 0)
        return;

    throw new InvalidOperationException($"""
        Command failed with exit code {process.ExitCode}: {fileName} {arguments}
        Working directory: {workingDirectory}
        stdout:
        {output}
        stderr:
        {error}
        """);
}

static string Quote(string value) => "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

static string EscapeXml(string value)
    => value.Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

internal sealed record PackageInfo(string Id, string Version);
