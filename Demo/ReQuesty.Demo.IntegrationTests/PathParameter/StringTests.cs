namespace ReQuesty.Demo.IntegrationTests.PathParameter;

/// <summary>
///   Tests for the string path parameter endpoints
/// </summary>
public class StringTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Valid string path parameter should return an object with the string as its Name
    /// </summary>
    [Fact]
    public async Task String_Random()
    {
        string stringId = Guid.NewGuid().ToString();

        Task<SomeObject?> task = ApiClient.PathParameter.StringId[stringId].GetAsync();

        task.ShouldNotThrow();
        SomeObject? result = await task;
        result.ShouldNotBeNull();
        result.Name.ShouldBe(stringId);
    }
}
