namespace ReQuesty.Demo.IntegrationTests.PathParameter;

/// <summary>
///   Tests for the long path parameter endpoints
/// </summary>
public class LongTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Valid long path parameter should return an object with matching cost
    /// </summary>
    [Fact]
    public async Task Long_Random()
    {
        long longId = Random.Shared.NextInt64(1, 10000);

        Task<SomeObject?> task = ApiClient.PathParameter.LongId[longId].GetAsync();

        task.ShouldNotThrow();
        SomeObject? result = await task;
        result.ShouldNotBeNull();
        result.Cost.ShouldBe(longId);
    }
}
