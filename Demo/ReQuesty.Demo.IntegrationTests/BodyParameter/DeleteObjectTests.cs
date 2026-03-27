namespace ReQuesty.Demo.IntegrationTests.BodyParameter;

/// <summary>
///   Tests for the Delete verb.
/// </summary>
public class DeleteObjectTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Using Delete should not throw.
    /// </summary>
    [Fact]
    public async Task Delete_Object()
    {
        Guid id = Guid.NewGuid();

        Task task = ApiClient.BodyParameter.Object[id].DeleteAsync();

        task.ShouldNotThrow();
        await task;
    }
}
