using System;
using System.Diagnostics.CodeAnalysis;
using ReQuesty.Demo.IntegrationTests.ApiClient;
using ReQuesty.Runtime.Abstractions.Authentication;
using ReQuesty.Runtime.Http;

namespace ReQuesty.Demo.IntegrationTests;

/// <summary>
///   Provides access to the Demo API client, this is not a singleton. Each test should create its own instance.
/// </summary>
public class ApiService : IAsyncDisposable
{
    /// <summary>
    ///   The client for the API.
    /// </summary>
    public DemoApiClient Client { get; }

    private ApiService(DemoApiClient client)
    {
        Client = client;
    }

    internal static async Task<ApiService> CreateAsync(CancellationToken cancellationToken = default)
    {
        AspireHostService hostService = await AspireHostService.GetAsync(cancellationToken);
        HttpClientRequestAdapter requestAdapter = new(new AnonymousAuthenticationProvider(), httpClient: hostService.GetApiHttpClient());

        return new ApiService(new(requestAdapter));
    }

    #region IAsyncDisposable
    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
    #endregion
}
