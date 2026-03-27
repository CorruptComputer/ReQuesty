namespace ReQuesty.Demo.IntegrationTests.PrimativeReturn;

/// <summary>
///   Tests for the float endpoints
/// </summary>
public class FloatTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Null float should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Float_Null()
    {
        Task<float?> task = ApiClient.PrimativeReturn.Float.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        float? result = await task;
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid float should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Float_Random()
    {
        Task<float?> task = ApiClient.PrimativeReturn.Float.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        float? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid float should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Float_Invalid()
    {
        Task<float?> task = ApiClient.PrimativeReturn.Float.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }

    /// <summary>
    ///   Null nullable float should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableFloat_Null()
    {
        Task<float?> task = ApiClient.PrimativeReturn.Float.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        float? result = await task;
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid nullable float should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableFloat_Random()
    {
        Task<float?> task = ApiClient.PrimativeReturn.Float.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        float? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid nullable float should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableFloat_Invalid()
    {
        Task<float?> task = ApiClient.PrimativeReturn.Float.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }
}
