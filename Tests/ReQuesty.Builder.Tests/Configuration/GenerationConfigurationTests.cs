using ReQuesty.Builder.Configuration;
using Xunit;

namespace ReQuesty.Builder.Tests.Configuration;
public class GenerationConfigurationTests
{
    [Fact]
    public void Clones()
    {
        GenerationConfiguration generationConfiguration = new()
        {
            ClientClassName = "class1",
            IncludePatterns = null!,
        };
        GenerationConfiguration? clone = generationConfiguration.Clone() as GenerationConfiguration;
        Assert.NotNull(clone);
        Assert.Equal(generationConfiguration.ClientClassName, clone.ClientClassName);
        Assert.NotNull(clone.IncludePatterns);
        Assert.Empty(clone.IncludePatterns);
        clone.ClientClassName = "class2";
        Assert.NotEqual(generationConfiguration.ClientClassName, clone.ClientClassName);
    }
}
