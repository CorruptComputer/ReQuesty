namespace ReQuesty.Demo.IntegrationTests.StatusCode;

/// <summary>
///   Tests for the status code endpoints
/// </summary>
public class StatusCodeTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   200 OK should not throw and return an object
    /// </summary>
    [Fact]
    public async Task StatusCode_Ok()
    {
        Task task = ApiClient.StatusCode.Ok.GetAsync();

        task.ShouldNotThrow();
    }

    /// <summary>
    ///   201 Created should not throw and return an object
    /// </summary>
    [Fact]
    public async Task StatusCode_Created()
    {
        Task task = ApiClient.StatusCode.Created.GetAsync();

        task.ShouldNotThrow();
    }

    /// <summary>
    ///   204 No Content should not throw
    /// </summary>
    [Fact]
    public async Task StatusCode_NoContent()
    {
        Task task = ApiClient.StatusCode.Nocontent.GetAsync();

        task.ShouldNotThrow();
        await task;
    }

    /// <summary>
    ///   400 Bad Request should throw
    /// </summary>
    [Fact]
    public async Task StatusCode_BadRequest()
    {
        Task task = ApiClient.StatusCode.Badrequest.GetAsync();

        task.ShouldThrow<Exception>();
    }

    /// <summary>
    ///   404 Not Found should throw
    /// </summary>
    [Fact]
    public async Task StatusCode_NotFound()
    {
        Task task = ApiClient.StatusCode.Notfound.GetAsync();

        task.ShouldThrow<Exception>();
    }

    /// <summary>
    ///   500 Internal Server Error should eventually throw (ReQuesty may retry 5xx responses
    ///   before surfacing the exception, so no fixed-timeout assertion is used here).
    /// </summary>
    [Fact]
    public async Task StatusCode_InternalError()
    {
        Exception ex = await Record.ExceptionAsync(() => ApiClient.StatusCode.Internalerror.GetAsync());

        ex.ShouldNotBeNull();
    }
}
