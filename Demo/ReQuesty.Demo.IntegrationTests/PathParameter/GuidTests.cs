namespace ReQuesty.Demo.IntegrationTests.PathParameter;

/// <summary>
///   Tests for the Guid endpoints
/// </summary>
public class GuidTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Valid guid should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Guid_Random()
    {
        Guid guidId = Guid.NewGuid();

        Task<SomeObject?> task = ApiClient.PathParameter.GuidId[guidId].GetAsync();

        task.ShouldNotThrow();
        SomeObject? result = await task;
        result.ShouldNotBeNull();
        result.Id.ShouldBe(guidId);
    }
}
