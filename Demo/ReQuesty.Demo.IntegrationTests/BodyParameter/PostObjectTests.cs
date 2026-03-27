namespace ReQuesty.Demo.IntegrationTests.BodyParameter;

/// <summary>
///   Tests for the Post verb.
/// </summary>
public class PostObjectTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Posting a SomeObject should return it back
    /// </summary>
    [Fact]
    public async Task Post_Object()
    {
        SomeObject body = new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Object",
            Age = 30,
            RequestedAt = DateTimeOffset.UtcNow,
            Cost = 99.99
        };

        Task<SomeObject?> task = ApiClient.BodyParameter.Object.PostAsync(body);

        task.ShouldNotThrow();
        SomeObject? result = await task;
        result.ShouldNotBeNull();
        result.Id.ShouldBe(body.Id);
        result.Name.ShouldBe(body.Name);
        result.Age.ShouldBe(body.Age);
    }
}
