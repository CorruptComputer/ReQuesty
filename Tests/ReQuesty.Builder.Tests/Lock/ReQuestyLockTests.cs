using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Lock;
using Xunit;

namespace ReQuesty.Builder.Tests.Lock;

public class ReQuestyLockTests
{
    [Fact]
    public void UpdatesAConfiguration()
    {
        ReQuestyLock reQuestyLock = new()
        {
            DescriptionLocation = "description",
        };
        GenerationConfiguration generationConfiguration = new();
        reQuestyLock.UpdateGenerationConfigurationFromLock(generationConfiguration);
        Assert.Equal(reQuestyLock.DescriptionLocation, generationConfiguration.OpenAPIFilePath);
    }
}
