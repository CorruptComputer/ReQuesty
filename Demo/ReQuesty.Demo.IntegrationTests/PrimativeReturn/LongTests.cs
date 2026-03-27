namespace ReQuesty.Demo.IntegrationTests.PrimativeReturn;

/// <summary>
///   Tests for the long endpoints
/// </summary>
public class LongTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Null long should not throw
    /// </summary>
    [Fact]
    public async Task Long_Null()
    {
        Task<long?> task = ApiClient.PrimativeReturn.Long.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        long? result = await task;
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid long should not throw
    /// </summary>
    [Fact]
    public async Task Long_Random()
    {
        Task<long?> task = ApiClient.PrimativeReturn.Long.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        long? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid long should throw
    /// </summary>
    [Fact]
    public async Task Long_Invalid()
    {
        Task<long?> task = ApiClient.PrimativeReturn.Long.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }

    /// <summary>
    ///   Null nullable long should not throw
    /// </summary>
    [Fact]
    public async Task NullableLong_Null()
    {
        Task<long?> task = ApiClient.PrimativeReturn.Long.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        long? result = await task;
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid nullable long should not throw
    /// </summary>
    [Fact]
    public async Task NullableLong_Random()
    {
        Task<long?> task = ApiClient.PrimativeReturn.Long.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        long? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid nullable long should throw
    /// </summary>
    [Fact]
    public async Task NullableLong_Invalid()
    {
        Task<long?> task = ApiClient.PrimativeReturn.Long.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }
}
