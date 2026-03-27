namespace ReQuesty.Demo.IntegrationTests.PrimativeReturn;

/// <summary>
///   Tests for the bool endpoints
/// </summary>
public class BoolTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Null bool should not throw
    /// </summary>
    [Fact]
    public async Task Bool_Null()
    {
        Task<bool?> task = ApiClient.PrimativeReturn.Bool.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        bool? result = await task;
        result.ShouldBe(false);
    }

    /// <summary>
    ///   Valid bool should not throw
    /// </summary>
    [Fact]
    public async Task Bool_Random()
    {
        Task<bool?> task = ApiClient.PrimativeReturn.Bool.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        bool? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid bool should throw
    /// </summary>
    [Fact]
    public async Task Bool_Invalid()
    {
        Task<bool?> task = ApiClient.PrimativeReturn.Bool.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }

    /// <summary>
    ///   Null nullable bool should not throw
    /// </summary>
    [Fact]
    public async Task NullableBool_Null()
    {
        Task<bool?> task = ApiClient.PrimativeReturn.Bool.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        bool? result = await task;
        result.ShouldBe(false);
    }

    /// <summary>
    ///   Valid nullable bool should not throw
    /// </summary>
    [Fact]
    public async Task NullableBool_Random()
    {
        Task<bool?> task = ApiClient.PrimativeReturn.Bool.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        bool? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid nullable bool should throw
    /// </summary>
    [Fact]
    public async Task NullableBool_Invalid()
    {
        Task<bool?> task = ApiClient.PrimativeReturn.Bool.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }
}
