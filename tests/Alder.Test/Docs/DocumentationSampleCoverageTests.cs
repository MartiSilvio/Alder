using System.Text.RegularExpressions;
using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

public class DocumentationSampleCoverageTests
{
    private static readonly Regex FenceStartPattern = new(@"^```(?<language>[A-Za-z0-9_+-]*)\s*$", RegexOptions.Compiled);
    private static readonly Regex TestMarkerPattern = new(@"^<!--\s*test:\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*-->$", RegexOptions.Compiled);
    private static readonly Regex TestMethodPattern = new(@"\bpublic\s+(?:async\s+Task|void)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\(", RegexOptions.Compiled);

    [Test]
    public void PublicCSharpDocSamples_HaveMatchingDocTests()
    {
        var root = FindRepositoryRoot();
        var docsRoot = Path.Combine(root, "docs");
        var testMethods = GetDocTestMethodNames(root);
        var missingMarkers = new List<string>();
        var missingTests = new List<string>();

        foreach (var path in Directory.EnumerateFiles(docsRoot, "*.md", SearchOption.AllDirectories)
                     .Where(path => IsPublicDocPage(docsRoot, path))
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var relativePath = TestHelpers.GetRelativePath(root, path);
            var lines = File.ReadAllLines(path);

            for (var i = 0; i < lines.Length; i++)
            {
                var fence = FenceStartPattern.Match(lines[i]);
                if (!fence.Success || fence.Groups["language"].Value != "csharp")
                {
                    continue;
                }

                var marker = FindAdjacentTestMarker(lines, i);
                if (marker is null)
                {
                    missingMarkers.Add($"{relativePath}:{i + 1}");
                    continue;
                }

                if (!testMethods.Contains(marker))
                {
                    missingTests.Add($"{relativePath}:{i + 1} -> {marker}");
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(missingMarkers, Is.Empty, "C# doc samples without adjacent test markers");
            Assert.That(missingTests, Is.Empty, "Doc sample markers without matching NUnit doc tests");
        });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs")) &&
                Directory.Exists(Path.Combine(directory.FullName, "tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static HashSet<string> GetDocTestMethodNames(string root)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in EnumerateDocTestFiles(root))
        {
            foreach (Match match in TestMethodPattern.Matches(File.ReadAllText(path)))
            {
                names.Add(match.Groups["name"].Value);
            }
        }

        return names;
    }

    private static IEnumerable<string> EnumerateDocTestFiles(string root)
    {
        var testsRoot = Path.Combine(root, "tests");
        return Directory.EnumerateDirectories(testsRoot, "*", SearchOption.TopDirectoryOnly)
            .Select(projectRoot => Path.Combine(projectRoot, "Docs"))
            .Where(Directory.Exists)
            .SelectMany(docsTestRoot => Directory.EnumerateFiles(docsTestRoot, "*.cs", SearchOption.TopDirectoryOnly))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static bool IsPublicDocPage(string docsRoot, string path)
    {
        var relative = TestHelpers.GetRelativePath(docsRoot, path);
        return !relative.StartsWith($"meta{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
               !relative.StartsWith($"plans{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static string? FindAdjacentTestMarker(string[] lines, int fenceLine)
    {
        for (var i = fenceLine - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var marker = TestMarkerPattern.Match(lines[i]);
            return marker.Success ? marker.Groups["name"].Value : null;
        }

        return null;
    }
}
