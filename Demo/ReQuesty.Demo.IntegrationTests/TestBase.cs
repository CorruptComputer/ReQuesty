using ReQuesty.Demo.IntegrationTests.ApiClient;

namespace ReQuesty.Demo.IntegrationTests;

/// <summary>
///   Base class for the integration tests.
/// </summary>
public abstract class TestBase
{
    private ApiService? ApiService { get; set; }

    internal DemoApiClient? ApiClient => ApiService?.Client;

    internal async Task SetupApiClientAsync(CancellationToken cancellationToken = default)
    {
        ApiService = await ApiService.CreateAsync(cancellationToken);

        if (ApiService is null || ApiClient is null)
        {
            throw new InvalidOperationException("Failed to initialize API client.");
        }
    }
}