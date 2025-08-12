using Xunit;

namespace ReQuesty.Builder.Tests;
public class ReQuestyGeneratedTests
{
    [Fact]
    public void StaticallyGeneratedAssemblyVersion()
    {
        string? testDir = Path.GetDirectoryName(typeof(ReQuestyGeneratedTests).Assembly.Location);

        if (string.IsNullOrEmpty(testDir))
        {
            Assert.Fail("Test project directory could not be determined.");
            return;
        }

        string? topLevelFolder =
            Directory.GetParent(testDir) // [...]/ReQuesty/Tests/ReQuesty.Builder.Tests/bin/Debug/net9.0/
            ?.Parent                     // [...]/ReQuesty/Tests/ReQuesty.Builder.Tests/bin/Debug/
            ?.Parent                     // [...]/ReQuesty/Tests/ReQuesty.Builder.Tests/bin/
            ?.Parent                     // [...]/ReQuesty/Tests/ReQuesty.Builder.Tests/
            ?.Parent                     // [...]/ReQuesty/Tests/
            ?.FullName;                  // [...]/ReQuesty/

        string csprojFile = Path.Join(topLevelFolder, "ReQuesty", "ReQuesty.csproj");

        string version = GetLineValue(csprojFile, "Version");

        Assert.Equal(version, ReQuesty.Generated.ReQuestyVersion.Current());
    }

    private static string GetLineValue(string csprojFile, string key)
    {
        string line = Array.Find(File.ReadAllLines(csprojFile), l => l.Contains($"<{key}>")) ?? string.Empty;
        line = line.Trim();
        line = line.Replace($"<{key}>", "");
        return line.Replace($"</{key}>", "");
    }
}
