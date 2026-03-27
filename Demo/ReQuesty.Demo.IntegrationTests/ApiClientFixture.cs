namespace ReQuesty.Demo.IntegrationTests;

/// <summary>
///   Provides a <see cref="DemoApiClient"/> on a per-test basis
/// </summary>
public class ApiClientFixture : IAsyncLifetime
{
    /// <summary>
    ///   The API client to use for the test, unique to each test.
    /// </summary>
    public DemoApiClient ApiClient { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        ApiService apiService = await ApiService.CreateAsync();
        ApiClient = apiService.Client;
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;
}