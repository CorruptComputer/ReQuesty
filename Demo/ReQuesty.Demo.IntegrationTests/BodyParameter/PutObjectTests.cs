namespace ReQuesty.Demo.IntegrationTests.BodyParameter;

/// <summary>
///   Tests for the Put verb.
/// </summary>
public class PutObjectTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Putting a SomeObject should return it with the route ID applied
    /// </summary>
    [Fact]
    public async Task Put_Object()
    {
        Guid id = Guid.NewGuid();
        SomeObject body = new()
        {
            Id = Guid.NewGuid(),
            Name = "Updated Object",
            Age = 40,
            RequestedAt = DateTimeOffset.UtcNow,
            Cost = 55.0
        };

        Task<SomeObject?> task = ApiClient.BodyParameter.Object[id].PutAsync(body);

        task.ShouldNotThrow();
        SomeObject? result = await task;
        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
        result.Name.ShouldBe(body.Name);
        result.Age.ShouldBe(body.Age);
    }
}
