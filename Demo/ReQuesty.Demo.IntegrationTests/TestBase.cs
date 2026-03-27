namespace ReQuesty.Demo.IntegrationTests;

/// <summary>
///   Base class for the integration tests.
/// </summary>
public abstract class TestBase(ApiClientFixture fixture) : IClassFixture<ApiClientFixture>
{
    /// <summary>
    ///   The API client to use for the tests.
    /// </summary>
    protected readonly DemoApiClient ApiClient = fixture.ApiClient;
}