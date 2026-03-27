namespace ReQuesty.Demo.IntegrationTests.PathParameter;

/// <summary>
///   Tests for the int endpoints
/// </summary>
public class IntegerTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Valid int should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Integer_Random()
    {
        int integerId = Random.Shared.Next();

        Task<SomeObject?> task = ApiClient.PathParameter.IntegerId[integerId].GetAsync();

        task.ShouldNotThrow();
        SomeObject? result = await task;
        result.ShouldNotBeNull();
    }
}
