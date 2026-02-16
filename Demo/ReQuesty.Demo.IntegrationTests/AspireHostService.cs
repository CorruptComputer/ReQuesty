using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using ReQuesty.Demo.AppHost;

namespace ReQuesty.Demo.IntegrationTests;

/// <summary>
///   Provides a singleton service for managing the Aspire host.
/// </summary>
public class AspireHostService : IAsyncDisposable
{
    private static AspireHostService? _instance;

    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly DistributedApplication _application;

    private AspireHostService(DistributedApplication app)
    {
        _application = app;
    }

    /// <summary>
    ///   Creates or retrieves the singleton instance of <see cref="AspireHostService"/>.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<AspireHostService> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_instance is null)
        {
            try
            {
                bool lockAcquired = false;

                do
                {
                    lockAcquired = await _semaphore.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken);
                }
                while (!lockAcquired);

                _instance = await CreateAsync(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return _instance;
    }

    /// <summary>
    ///   Gets an <see cref="HttpClient"/> configured to communicate with the API service.
    /// </summary>
    /// <returns></returns>
    public HttpClient GetApiHttpClient()
    {
        HttpClient client = _application.CreateHttpClient(Program.ApiProjectName);
        return client;
    }

    private static async Task<AspireHostService> CreateAsync(CancellationToken cancellationToken)
    {
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ReQuesty_Demo_AppHost>(cancellationToken);

        builder.Services.ConfigureHttpClientDefaults(client =>
        {
            client.AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.FailureRatio = 1.0;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(180);
            });
        });

        DistributedApplication app = await builder.BuildAsync(cancellationToken);
        ResourceNotificationService resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();

        await app.StartAsync(cancellationToken);
        await resourceNotificationService.WaitForResourceAsync(Program.ApiProjectName, KnownResourceStates.Running, cancellationToken);
        return new(app);
    }

    #region IAsyncDisposable
    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _application.StopAsync();
        await _application.DisposeAsync();
        _instance = null;

        GC.SuppressFinalize(this);
    }
    #endregion
}
