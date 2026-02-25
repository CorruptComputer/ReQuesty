namespace ReQuesty.Demo.IntegrationTests.PrimativeReturn;

/// <summary>
///   Tests for the double endpoints
/// </summary>
public class DoubleTests : TestBase
{
    /// <summary>
    ///   Null double should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Double_Null()
    {
        await SetupApiClientAsync();

        Task<double?> task = ApiClient!.PrimativeReturn.Double.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Null;
        });

        task.ShouldNotThrow();
        double? result = await task;
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid double should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Double_Random()
    {
        await SetupApiClientAsync();

        Task<double?> task = ApiClient!.PrimativeReturn.Double.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Random;
        });

        task.ShouldNotThrow();
        double? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid double should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Double_Invalid()
    {
        await SetupApiClientAsync();

        Task<double?> task = ApiClient!.PrimativeReturn.Double.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }

    /// <summary>
    ///   Null nullable double should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableDouble_Null()
    {
        await SetupApiClientAsync();

        Task<double?> task = ApiClient!.PrimativeReturn.Double.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Null;
        });

        task.ShouldNotThrow();
        double? result = await task;
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid nullable double should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableDouble_Random()
    {
        await SetupApiClientAsync();

        Task<double?> task = ApiClient!.PrimativeReturn.Double.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Random;
        });

        task.ShouldNotThrow();
        double? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid nullable double should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableDouble_Invalid()
    {
        await SetupApiClientAsync();

        Task<double?> task = ApiClient!.PrimativeReturn.Double.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = IntegrationTests.ApiClient.Models.ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }
}
