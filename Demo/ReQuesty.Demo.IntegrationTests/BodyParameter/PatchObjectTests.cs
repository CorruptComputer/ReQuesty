namespace ReQuesty.Demo.IntegrationTests.BodyParameter;

/// <summary>
///   Tests for the Patch verb.
/// </summary>
public class PatchObjectTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Patching a SomeObject should return it with the route ID applied
    /// </summary>
    [Fact]
    public async Task Patch_Object()
    {
        Guid id = Guid.NewGuid();
        SomeObject body = new()
        {
            Name = "Patched Object",
            Age = 22,
            RequestedAt = DateTimeOffset.UtcNow,
            Cost = 12.5
        };

        Task<SomeObject?> task = ApiClient.BodyParameter.Object[id].PatchAsync(body);

        task.ShouldNotThrow();
        SomeObject? result = await task;
        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
        result.Name.ShouldBe(body.Name);
    }
}
