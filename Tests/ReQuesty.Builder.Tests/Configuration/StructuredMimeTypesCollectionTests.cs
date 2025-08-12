using ReQuesty.Builder.Configuration;
using Xunit;

namespace ReQuesty.Builder.Tests.Configuration;

public sealed class StructuredMimeTypesCollectionTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new StructuredMimeTypesCollection(null!));
    }
    [Fact]
    public void ParsesWithOrWithoutPriorities()
    {
        StructuredMimeTypesCollection mimeTypes = new(["application/json", "application/xml;q=0.8"]);
        Assert.Equal("application/json", mimeTypes.First(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal("application/xml;q=0.8", mimeTypes.Last(), StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("application/atom+xml", mimeTypes);
        Assert.Equal(0.8f, mimeTypes.GetPriority("application/atom+xml"));
        Assert.Equal(1, mimeTypes.GetPriority("application/json"));
        Assert.Equal(0.8f, mimeTypes.GetPriority("application/xml"));
    }
    [Fact]
    public void DoesNotAddDuplicates()
    {
        Assert.Throws<ArgumentException>(() => new StructuredMimeTypesCollection(["application/json", "application/json;q=0.8"]));
    }
    [Fact]
    public void ClearsEntries()
    {
        StructuredMimeTypesCollection mimeTypes = new(["application/json", "application/xml;q=0.8"]);
        Assert.Equal(2, mimeTypes.Count);
        mimeTypes.Clear();
        Assert.Empty(mimeTypes);
    }
    [Theory]
    [InlineData("application/json, application/xml, application/yaml", "application/json", "application/json")]
    [InlineData("application/json, application/xml, application/yaml", "application/json,text/plain", "application/json")]
    [InlineData("application/json, application/xml, application/yaml;q=0.8", "application/json,text/plain,application/yaml", "application/json,application/yaml;q=0.8")]
    [InlineData("application/json, application/xml;q=0.9, application/yaml;q=0.8", "application/github+json", "application/github+json")]
    [InlineData("application/vnd.topicus.keyhub+json;version=67, application/yaml;q=0.8", "application/vnd.topicus.keyhub+json;version=67", "application/vnd.topicus.keyhub+json;version=67")]
    [InlineData("application/vnd.topicus.keyhub+json, application/yaml;q=0.8", "application/vnd.topicus.keyhub+json;version=67", "application/vnd.topicus.keyhub+json;version=67")]
    [InlineData("application/vnd.topicus.keyhub+json;version=67", "application/vnd.topicus.keyhub+json;version=67,application/vnd.topicus.keyhub+json;version=67,application/vnd.topicus.keyhub+xml;version=67", "application/vnd.topicus.keyhub+json;version=67")]
    public void MatchesAccept(string configuredTypes, string declaredTypes, string expectedTypes)
    {
        StructuredMimeTypesCollection mimeTypes = new(configuredTypes.Split(',').Select(static x => x.Trim()));
        IEnumerable<string> result = mimeTypes.GetAcceptedTypes(declaredTypes.Split(',').Select(static x => x.Trim()));
        IEnumerable<string> deserializedExpectedTypes = expectedTypes.Split(',').Select(static x => x.Trim());
        foreach (string? expectedType in deserializedExpectedTypes)
        {
            Assert.Contains(expectedType, result);
        }

        Assert.Equal(result.Distinct(StringComparer.OrdinalIgnoreCase).Count(), result.Count());
    }
    [Theory]
    [InlineData("application/json, application/xml;q=0.9, application/yaml;q=0.8", "application/json", "application/json")]
    [InlineData("application/json, application/xml;q=0.9, application/yaml;q=0.8", "application/json,text/plain", "application/json")]
    [InlineData("application/json, application/xml;q=0.9, application/yaml;q=0.8", "application/json,text/plain,application/yaml", "application/json")]
    [InlineData("application/json, application/xml;q=0.9, application/yaml;q=0.8", "application/github+json", "application/github+json")]
    [InlineData("application/vnd.topicus.keyhub+json;version=67, application/yaml;q=0.8", "application/vnd.topicus.keyhub+json;version=67", "application/vnd.topicus.keyhub+json;version=67")]
    public void MatchesContentType(string configuredTypes, string declaredTypes, string expectedTypes)
    {
        StructuredMimeTypesCollection mimeTypes = new(configuredTypes.Split(',').Select(static x => x.Trim()));
        IEnumerable<string> result = mimeTypes.GetContentTypes(declaredTypes.Split(',').Select(static x => x.Trim()));
        IEnumerable<string> deserializedExpectedTypes = expectedTypes.Split(',').Select(static x => x.Trim());
        foreach (string? expectedType in deserializedExpectedTypes)
        {
            Assert.Contains(expectedType, result);
        }
    }
    [Fact]
    public void ThrowsOnInvalidMimeType()
    {
        Assert.Throws<ArgumentException>(() => new StructuredMimeTypesCollection(["application"]));
        Assert.Throws<ArgumentException>(() => new StructuredMimeTypesCollection([null!]));
    }
}
