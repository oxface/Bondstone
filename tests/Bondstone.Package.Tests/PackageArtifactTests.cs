using System.IO.Compression;
using System.Xml.Linq;
using Xunit;

namespace Bondstone.Package.Tests;

public sealed class PackageArtifactTests
{
    private const string TargetFramework = "net10.0";

    [Fact]
    [Trait("Category", "Package")]
    public void PackableProjects_ShipXmlDocumentationBesidePackageAssembly()
    {
        var repositoryRoot = FindRepositoryRoot();
        var packageDirectory = Path.Combine(repositoryRoot, "artifacts", "packages");
        var expectedVersion = ReadExpectedPackageVersion(repositoryRoot);
        var packableProjects = FindPackableProjects(repositoryRoot).ToArray();

        Assert.NotEmpty(packableProjects);
        Assert.True(
            Directory.Exists(packageDirectory),
            $"Package artifact directory '{packageDirectory}' does not exist. Run 'pnpm backend:pack' before package artifact tests.");

        foreach (var project in packableProjects)
        {
            var packagePath = Path.Combine(packageDirectory, $"{project.PackageId}.{expectedVersion}.nupkg");

            Assert.True(
                File.Exists(packagePath),
                $"Expected package '{packagePath}' was not produced. Run 'pnpm backend:pack' from a clean build.");

            using var package = ZipFile.OpenRead(packagePath);
            var entries = package.Entries.Select(entry => entry.FullName).ToHashSet(StringComparer.Ordinal);
            var assemblyPath = $"lib/{TargetFramework}/{project.AssemblyName}.dll";
            var documentationPath = $"lib/{TargetFramework}/{project.AssemblyName}.xml";

            Assert.True(entries.Contains(assemblyPath), $"Package '{packagePath}' is missing '{assemblyPath}'.");
            Assert.True(entries.Contains(documentationPath), $"Package '{packagePath}' is missing '{documentationPath}'.");
        }
    }

    private static IEnumerable<PackableProject> FindPackableProjects(string repositoryRoot)
    {
        var sourceDirectory = Path.Combine(repositoryRoot, "src");

        return Directory.EnumerateFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories)
            .Select(ReadPackableProject)
            .Where(project => project is not null)
            .Select(project => project!);
    }

    private static PackableProject? ReadPackableProject(string projectPath)
    {
        var document = XDocument.Load(projectPath);
        var isPackable = document
            .Descendants("IsPackable")
            .Any(element => string.Equals(element.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));

        if (!isPackable)
        {
            return null;
        }

        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var packageId = ReadElementValue(document, "PackageId") ?? projectName;
        var assemblyName = ReadElementValue(document, "AssemblyName") ?? projectName;

        return new PackableProject(packageId, assemblyName);
    }

    private static string ReadExpectedPackageVersion(string repositoryRoot)
    {
        var document = XDocument.Load(Path.Combine(repositoryRoot, "Directory.Build.props"));
        var versionPrefix = ReadElementValue(document, "VersionPrefix");

        Assert.False(string.IsNullOrWhiteSpace(versionPrefix), "Directory.Build.props must define VersionPrefix.");

        return IsGitHubActions()
            ? versionPrefix
            : $"{versionPrefix}-local";
    }

    private static string? ReadElementValue(XDocument document, string name) =>
        document.Descendants(name)
            .Select(element => element.Value.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static bool IsGitHubActions() =>
        string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Bondstone.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not find the Bondstone repository root.");
    }

    private sealed record PackableProject(string PackageId, string AssemblyName);
}
