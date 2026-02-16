using System;
using System.Text.Json;
using ReQuesty.Runtime.Abstractions;
using Shouldly;

namespace ReQuesty.Demo.IntegrationTests.PrimativeReturn;

/// <summary>
///   Tests for the Guid endpoints
/// </summary>
public class GuidTests : TestBase
{
    /// <summary>
    ///   Null guid should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Guid_Null()
    {
        await SetupApiClientAsync();

        Task<Guid?> task = ApiClient!.PrimativeReturn.Guid.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Null;
        });

        task.ShouldNotThrow();
        Guid? result = await task;
        result.ShouldBe(Guid.Empty);
    }

    /// <summary>
    ///   Valid guid should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Guid_Random()
    {
        await SetupApiClientAsync();

        Task<Guid?> task = ApiClient!.PrimativeReturn.Guid.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Random;
        });

        task.ShouldNotThrow();
        Guid? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid guid should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Guid_Invalid()
    {
        await SetupApiClientAsync();

        Task<Guid?> task = ApiClient!.PrimativeReturn.Guid.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Invalid;
        });

        task.ShouldThrow<JsonException>();
    }

    /// <summary>
    ///   Null nullable guid should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableGuid_Null()
    {
        await SetupApiClientAsync();

        Task<Guid?> task = ApiClient!.PrimativeReturn.NullableGuid.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Null;
        });

        task.ShouldNotThrow();
        Guid? result = await task;
        result.ShouldBe(Guid.Empty);
    }

    /// <summary>
    ///   Valid nullable guid should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableGuid_Random()
    {
        await SetupApiClientAsync();

        Task<Guid?> task = ApiClient!.PrimativeReturn.NullableGuid.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Random;
        });

        task.ShouldNotThrow();
        Guid? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid nullable guid should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableGuid_Invalid()
    {
        await SetupApiClientAsync();

        Task<Guid?> task = ApiClient!.PrimativeReturn.NullableGuid.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Invalid;
        });

        task.ShouldThrow<JsonException>();
    }
}
