using System.Text.Json;

namespace ReQuesty.Demo.IntegrationTests.PrimativeReturn;

/// <summary>
///   Tests for the Guid endpoints
/// </summary>
public class GuidTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Null guid should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Guid_Null()
    {
        Task<Guid?> task = ApiClient.PrimativeReturn.Guid.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
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
        Task<Guid?> task = ApiClient.PrimativeReturn.Guid.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
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
        Task<Guid?> task = ApiClient.PrimativeReturn.Guid.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
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
        Task<Guid?> task = ApiClient.PrimativeReturn.Guid.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
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
        Task<Guid?> task = ApiClient.PrimativeReturn.Guid.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
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
        Task<Guid?> task = ApiClient.PrimativeReturn.Guid.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        task.ShouldThrow<JsonException>();
    }
}
