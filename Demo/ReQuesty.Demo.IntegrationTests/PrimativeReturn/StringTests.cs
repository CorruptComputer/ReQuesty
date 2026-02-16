using System;
using ReQuesty.Runtime.Abstractions;
using Shouldly;

namespace ReQuesty.Demo.IntegrationTests.PrimativeReturn;

/// <summary>
///   Tests for the string endpoints
/// </summary>
public class StringTests : TestBase
{
    /// <summary>
    ///   Null string should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task String_Null()
    {
        await SetupApiClientAsync();

        Task<string?> task = ApiClient!.PrimativeReturn.String.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Null;
        });

        task.ShouldNotThrow();
        string? result = await task;
        result.ShouldBeNull();
    }

    /// <summary>
    ///   Valid string should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task String_Random()
    {
        await SetupApiClientAsync();

        Task<string?> task = ApiClient!.PrimativeReturn.String.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Random;
        });

        task.ShouldNotThrow();
        string? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid string should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task String_Invalid()
    {
        await SetupApiClientAsync();

        Task<string?> task = ApiClient!.PrimativeReturn.String.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Invalid;
        });

        task.ShouldNotThrow();
        string? result = await task;
        result.ShouldBeNull();
    }

    /// <summary>
    ///   Null nullable string should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableString_Null()
    {
        await SetupApiClientAsync();

        Task<string?> task = ApiClient!.PrimativeReturn.NullableString.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Null;
        });

        task.ShouldNotThrow();
        string? result = await task;
        result.ShouldBeNull();
    }

    /// <summary>
    ///   Valid nullable string should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableString_Random()
    {
        await SetupApiClientAsync();

        Task<string?> task = ApiClient!.PrimativeReturn.NullableString.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Random;
        });

        task.ShouldNotThrow();
        string? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid nullable string should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableString_Invalid()
    {
        await SetupApiClientAsync();

        Task<string?> task = ApiClient!.PrimativeReturn.NullableString.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Invalid;
        });

        task.ShouldNotThrow();
        string? result = await task;
        result.ShouldBeNull();
    }
}
