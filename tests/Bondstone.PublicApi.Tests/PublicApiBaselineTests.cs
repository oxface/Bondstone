using System.Reflection;
using PublicApiGenerator;
using Xunit;

namespace Bondstone.PublicApi.Tests;

public sealed class PublicApiBaselineTests
{
    private const string UpdateBaselineEnvironmentVariable = "BONDSTONE_UPDATE_PUBLIC_API_BASELINE";
    private static readonly ApiGeneratorOptions GeneratorOptions = new()
    {
        IncludeAssemblyAttributes = false,
    };

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(PackageAssemblies))]
    public void PackagePublicApi_MatchesBaseline(string assemblyName)
    {
        string actual = NormalizePublicApi(
            Assembly.Load(new AssemblyName(assemblyName)).GeneratePublicApi(GeneratorOptions));
        string baselinePath = Path.Combine(AppContext.BaseDirectory, "Baselines", assemblyName + ".txt");

        if (ShouldUpdateBaselines())
        {
            string sourceBaselinePath = FindSourceBaselinePath(assemblyName);
            Directory.CreateDirectory(Path.GetDirectoryName(sourceBaselinePath)!);
            File.WriteAllText(sourceBaselinePath, actual);
            return;
        }

        Assert.True(
            File.Exists(baselinePath),
            $"Missing public API baseline for {assemblyName}. Run with {UpdateBaselineEnvironmentVariable}=1 to create it.");

        string expected = File.ReadAllText(baselinePath).ReplaceLineEndings("\n");

        Assert.Equal(expected, actual);
    }

    public static TheoryData<string> PackageAssemblies()
    {
        return
        [
            "Bondstone",
            "Bondstone.Hosting",
            "Bondstone.Persistence",
            "Bondstone.Persistence.EntityFrameworkCore",
            "Bondstone.Persistence.EntityFrameworkCore.Postgres",
            "Bondstone.Transport.Local",
            "Bondstone.Transport.RabbitMq",
            "Bondstone.Transport.ServiceBus",
        ];
    }

    private static bool ShouldUpdateBaselines()
    {
        string? value = Environment.GetEnvironmentVariable(UpdateBaselineEnvironmentVariable);

        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindSourceBaselinePath(string assemblyName)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bondstone.slnx")))
            {
                return Path.Combine(
                    directory.FullName,
                    "tests",
                    "Bondstone.PublicApi.Tests",
                    "Baselines",
                    assemblyName + ".txt");
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }

    private static string NormalizePublicApi(string publicApi)
    {
        return publicApi.ReplaceLineEndings("\n").TrimEnd() + "\n";
    }
}
