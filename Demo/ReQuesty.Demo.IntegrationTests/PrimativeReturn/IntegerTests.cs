namespace ReQuesty.Demo.IntegrationTests.PrimativeReturn;

/// <summary>
///   Tests for the int endpoints
/// </summary>
public class IntegerTests : TestBase
{
    /// <summary>
    ///   Null int should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Integer_Null()
    {
        await SetupApiClientAsync();

        Task<int?> task = ApiClient!.PrimativeReturn.Integer.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Null;
        });

        task.ShouldNotThrow();
        int? result = await task;
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid int should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Integer_Random()
    {
        await SetupApiClientAsync();

        Task<int?> task = ApiClient!.PrimativeReturn.Integer.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Random;
        });

        task.ShouldNotThrow();
        int? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid int should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Integer_Invalid()
    {
        await SetupApiClientAsync();

        Task<int?> task = ApiClient!.PrimativeReturn.Integer.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }

    /// <summary>
    ///   Null nullable int should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableInteger_Null()
    {
        await SetupApiClientAsync();

        Task<int?> task = ApiClient!.PrimativeReturn.Integer.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Null;
        });

        task.ShouldNotThrow();
        int? result = await task;
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid nullable int should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableInteger_Random()
    {
        await SetupApiClientAsync();

        Task<int?> task = ApiClient!.PrimativeReturn.Integer.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Random;
        });

        task.ShouldNotThrow();
        int? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid nullable int should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableInteger_Invalid()
    {
        await SetupApiClientAsync();

        Task<int?> task = ApiClient!.PrimativeReturn.Integer.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }
}
